using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 넉백 생존전의 시뮬 코어. 칸 바닥 위를 걷고 탄을 쏘며, 칸이 없는 곳에서는 떨어진다.
    /// 위치·회전·속도는 베이스가 되감기 상태로 담고, 여기서는 몸별 접지·총·피해 쌓임과 날아가는 탄을 더 담는다.
    /// </summary>
    public class KnockbackWorld : GameFramework.World.WorldBase
    {
        private readonly KnockbackMoveSystem moveSystem;
        private readonly GameFramework.World.IMotionBridge motionBridge;
        private readonly KnockbackFireSystem fireSystem;
        private readonly BodyCollisionSystem bodyCollision;
        private readonly KnockbackConfig config;
        private readonly KnockbackPillars pillars;
        private readonly KnockbackPillarColliders pillarColliders;

        private sealed class SavedState
        {
            public readonly Dictionary<string, KnockbackSavedBody> Bodies = new Dictionary<string, KnockbackSavedBody>();
            public List<KnockbackShot> Shots;
        }

        private readonly GameFramework.Netcode.SequenceBuffer<SavedState> saved
            = new GameFramework.Netcode.SequenceBuffer<SavedState>(SaveCapacity);

        private readonly List<GameFramework.World.Entity> movers = new List<GameFramework.World.Entity>();
        private readonly List<GameFramework.World.Entity> standing = new List<GameFramework.World.Entity>();

        public KnockbackFloor Floor { get; }

        /// <summary>날아가는 탄들. 탄은 엔티티가 아니라 이 장부에만 있다.</summary>
        public KnockbackShotBook Shots { get; } = new KnockbackShotBook();

        public KnockbackWorld(GameFramework.World.EntityRegistry entityRegistry,
                              GameFramework.World.WorldEventBuffer eventBuffer,
                              KnockbackMoveSystem moveSystem,
                              GameFramework.World.IMotionBridge motionBridge,
                              KnockbackFloor floor,
                              KnockbackFireSystem fireSystem,
                              BodyCollisionSystem bodyCollision,
                              KnockbackConfig config,
                              KnockbackPillars pillars = null,
                              KnockbackPillarColliders pillarColliders = null)
            : base(entityRegistry, eventBuffer)
        {
            this.moveSystem = moveSystem;
            this.motionBridge = motionBridge;
            this.fireSystem = fireSystem;
            this.bodyCollision = bodyCollision;
            this.config = config;
            this.pillars = pillars;
            this.pillarColliders = pillarColliders;
            Floor = floor;
        }

        /// <summary>서버가 알려 준 남의 탄을 장부에 넣는다. 이미 있으면 false.</summary>
        public bool IngestRemoteShot(in KnockbackShot shot) => Shots.Add(shot);

        //  서버가 확정해 지운 탄(맞음). 되감기가 저장본에서 되살리지 못하게 키를 기억해 둔다.
        private readonly HashSet<(string shooterId, long fireTick)> removedShots = new HashSet<(string, long)>();

        public bool RemoveShot(string shooterId, long fireTick)
        {
            removedShots.Add((shooterId, fireTick));
            return Shots.Remove(shooterId, fireTick);
        }

        private readonly List<(string targetId, string sourceId)> chainHitsThisTick = new List<(string, string)>();

        /// <summary>이번 틱에 날아가는 몸에 부딪혀 날림이 걸린 (맞은 몸, 부딪혀 온 몸). 막타 크레딧을 서버가 읽는다. 되감기 저장 대상이 아니다.</summary>
        public IReadOnlyList<(string targetId, string sourceId)> ChainHitsThisTick => chainHitsThisTick;

        protected override void Mutation(long tick, float deltaTime)
        {
            chainHitsThisTick.Clear();
            //  기둥 몸을 이 틱 모양으로 세운다 — 이동의 충돌 검사(SyncTransforms 뒤)보다 앞이어야 한다.
            pillarColliders?.Apply(tick);

            if (HasStarted(tick) == false)
            {
                //  출발 전 대기. 속도를 0으로 적어 두는 건 스냅샷과 물리 팔로워가 이 값을 읽기 때문이다.
                foreach (var entity in EntityRegistry.All)
                {
                    var velocity = entity.Get<GameFramework.World.Velocity>();
                    if (velocity != null && entity.Has<GameFramework.World.Simulated>())
                    {
                        velocity.Linear = System.Numerics.Vector3.Zero;
                    }
                }
                return;
            }

            //  발사는 이동 앞이다 — 반동이 이번 틱 속도에 실려야 한다.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                fireSystem.Reload(entity, tick);
                //  서버가 맞았다고 지운 탄은 되감기 재생이 같은 입력으로 다시 쏘아도 장부에 넣지 않는다(탄 수는 쏜 대로 준다).
                if (fireSystem.TryFire(entity, tick, out var shot)
                    && removedShots.Contains((shot.ShooterId, shot.FireTick)) == false
                    && Shots.Add(shot))
                {
                    EventBuffer.Append(new KnockbackShotFiredEvent(shot.ShooterId, shot.FireTick, shot.Origin, shot.Direction, shot.IsBomb));
                }
            }

            motionBridge.SyncTransforms();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                motionBridge.Depenetrate(entity);
                motionBridge.Separate(entity);
                moveSystem.Tick(entity, tick, deltaTime);
                motionBridge.PushMotion(entity);
            }

            AddHazardDamage(tick);

            CollectBodies();
            MarkChainHits(tick);
            bodyCollision.Resolve(movers, standing);
            ApplyChainHits(tick);

            //  앞 틱 선분으로 지운다 — 그 틱의 서버 판정은 이미 끝났으니 순서가 결과를 바꾸지 않는다.
            if (pillars != null)
            {
                Shots.RemoveBlocked(tick - 1, pillars.CollectStanding, pillars.Radius, config, deltaTime);
            }

            Shots.RemoveSpent(tick, config.ShotSpeed, config.ShotRange, deltaTime);
            removedShots.RemoveWhere(k => new KnockbackShot(k.shooterId, k.fireTick, UnityEngine.Vector2.zero, UnityEngine.Vector2.right)
                .IsSpent(tick, config.ShotSpeed, config.ShotRange, deltaTime));
        }

        //  위험 지대에 서 있는 몸의 위험도를 올린다. 소수 누적 없이 틱 번호로 정해 되감아도 같다.
        private void AddHazardDamage(long tick)
        {
            int interval = config.HazardDamageIntervalTicks;
            if (interval <= 0 || tick % interval != 0)
            {
                return;
            }

            foreach (var entity in EntityRegistry.All)
            {
                var transform = entity.Get<GameFramework.World.Transform>();
                var body = entity.Get<KnockbackBody>();
                var ground = entity.Get<GameFramework.World.GroundState>();
                if (entity.Has<GameFramework.World.Simulated>() == false || transform == null || body == null || ground == null)
                {
                    continue;
                }

                //  떨어지는 중(접지 아님)이거나 이미 떨어져 탈락한 몸은 올리지 않는다.
                if (ground.IsGrounded && Floor.InHazard(transform.Position.X, transform.Position.Z, tick))
                {
                    body.DamagePercent += 1;
                }
            }
        }

        //  날아가는 몸에 맞닿은 몸은 충돌로 속도를 받는 순간 같이 날려진다 — 줄줄이 떨어지는 장면.
        //  누가 날아가는 중이었는지는 속도를 푸는 "앞"의 상태로 정한다(푼 뒤엔 서로 얽힌다).
        private readonly List<GameFramework.World.Entity> chainTargets = new List<GameFramework.World.Entity>();
        private readonly List<System.Numerics.Vector3> chainBefore = new List<System.Numerics.Vector3>();
        private readonly List<string> chainSources = new List<string>();
        private readonly List<bool> chainTargetWasFlying = new List<bool>();

        private static UnityEngine.Vector3 ToUnity(System.Numerics.Vector3 v) => new UnityEngine.Vector3(v.X, v.Y, v.Z);

        private static bool IsFlying(GameFramework.World.Entity entity, long tick)
            => tick < (entity.Get<KnockbackBody>()?.KnockbackEndTick ?? long.MinValue);

        private void MarkChainHits(long tick)
        {
            chainTargets.Clear();
            chainBefore.Clear();
            chainSources.Clear();
            chainTargetWasFlying.Clear();
            foreach (var mover in movers)
            {
                if (mover.Has<KnockbackBody>() == false)
                {
                    continue;
                }

                var moverPos = ToUnity(mover.Get<GameFramework.World.Transform>().Position);
                foreach (var other in standing)
                {
                    if (ReferenceEquals(other, mover) || IsFlying(other, tick) == false)
                    {
                        continue;
                    }

                    var otherPos = ToUnity(other.Get<GameFramework.World.Transform>().Position);
                    if (BodyOverlap.TryCompute(moverPos, otherPos, BodySizes.CharacterRadius, BodySizes.CharacterHeight,
                                               out _, out _))
                    {
                        chainTargets.Add(mover);
                        chainBefore.Add(mover.Get<GameFramework.World.Velocity>().Linear);
                        chainSources.Add(other.Id);
                        chainTargetWasFlying.Add(IsFlying(mover, tick));
                        break;
                    }
                }
            }
        }

        private void ApplyChainHits(long tick)
        {
            for (int i = 0; i < chainTargets.Count; i++)
            {
                var entity = chainTargets[i];
                if (entity.Get<GameFramework.World.Velocity>().Linear == chainBefore[i])
                {
                    continue;
                }

                var body = entity.Get<KnockbackBody>();
                body.KnockbackEndTick = System.Math.Max(body.KnockbackEndTick, KnockbackRules.StunEndTick(body, tick, config));
                //  크레딧은 "걷다가 처음 날려진 몸"만 물려받는다. 이미 날아가던 몸의 크레딧(직접 맞음 등)은 지키려고 목록에서 뺀다.
                if (chainTargetWasFlying[i] == false)
                {
                    chainHitsThisTick.Add((entity.Id, chainSources[i]));
                }
            }
        }

        //  id 순으로 세운다 — 푸는 순서가 클·서에서 같아야 같은 결과가 나온다.
        private void CollectBodies()
        {
            movers.Clear();
            standing.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                var transform = entity.Get<GameFramework.World.Transform>();
                if (entity.Get<EntityKind>()?.Kind != EntityType.Character)
                {
                    continue;
                }

                if (transform == null || entity.Has<GameFramework.World.Velocity>() == false
                    || entity.Has<GameFramework.World.CapsuleShape>() == false)
                {
                    continue;
                }

                //  떨어지는 몸은 더 부딪히지 않는다 — 부딪히는 쪽(movers)도 마찬가지여야 클라가 서버와 같다.
                if (transform.Position.Y < -0.05f)
                {
                    continue;
                }

                standing.Add(entity);
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    movers.Add(entity);
                }
            }
            movers.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            standing.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        protected override void SaveGameState(long tick)
        {
            var state = new SavedState { Shots = Shots.Capture() };
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;
                }

                var ground = entity.Get<GameFramework.World.GroundState>();
                var gun = entity.Get<KnockbackGun>();
                var body = entity.Get<KnockbackBody>();
                if (ground == null && gun == null && body == null)
                {
                    continue;
                }

                state.Bodies[entity.Id] = new KnockbackSavedBody
                {
                    IsGrounded = ground != null && ground.IsGrounded,
                    KnockbackEndTick = body != null ? body.KnockbackEndTick : long.MinValue,
                    DamagePercent = body != null ? body.DamagePercent : 0,
                    Ammo = gun != null ? gun.Ammo : 0,
                    NextReloadTick = gun != null ? gun.NextReloadTick : long.MaxValue,
                    CooldownEndTick = gun != null ? gun.CooldownEndTick : 0,
                    HasBomb = gun != null && gun.HasBomb,
                };
            }
            saved.Record(tick, state);
        }

        protected override bool LoadGameState(long tick)
        {
            if (saved.TryGet(tick, out var state) == false)
            {
                return false;
            }

            foreach (var pair in state.Bodies)
            {
                var entity = EntityRegistry.Get(pair.Key);
                if (entity == null)
                {
                    continue;
                }

                var s = pair.Value;
                var ground = entity.Get<GameFramework.World.GroundState>();
                if (ground != null)
                {
                    ground.IsGrounded = s.IsGrounded;
                }
                var gun = entity.Get<KnockbackGun>();
                if (gun != null)
                {
                    gun.Ammo = s.Ammo;
                    gun.NextReloadTick = s.NextReloadTick;
                    gun.CooldownEndTick = s.CooldownEndTick;
                    gun.HasBomb = s.HasBomb;
                }
                var body = entity.Get<KnockbackBody>();
                if (body != null)
                {
                    body.KnockbackEndTick = s.KnockbackEndTick;
                    body.DamagePercent = s.DamagePercent;
                }
            }
            //  내 몸이 쏜 탄(예측)만 저장본으로 되돌린다. 남의 탄은 서버가 한 번만 알려 준 사실이라
            //  저장본에 없어도 지우지 않고, 서버가 지운 탄은 저장본에 있어도 되살리지 않는다.
            var rebuilt = new List<KnockbackShot>();
            foreach (var shot in Shots.Shots)
            {
                if (IsSimulatedShooter(shot.ShooterId) == false)
                {
                    rebuilt.Add(shot);
                }
            }
            Shots.Restore(new List<KnockbackShot>());
            foreach (var shot in rebuilt)
            {
                Shots.Add(shot);
            }
            foreach (var shot in state.Shots)
            {
                if (IsSimulatedShooter(shot.ShooterId) && removedShots.Contains((shot.ShooterId, shot.FireTick)) == false)
                {
                    Shots.Add(shot);
                }
            }
            return true;
        }

        private bool IsSimulatedShooter(string shooterId)
            => EntityRegistry.Get(shooterId)?.Has<GameFramework.World.Simulated>() == true;

        /// <summary>그 틱에 저장된 한 몸의 상태. 서버 보정 처리기가 "그때 내 값이 뭐였나"를 물을 때 쓴다.</summary>
        public bool TryGetSaved(long tick, string entityId, out KnockbackSavedBody savedBody)
        {
            savedBody = default;
            return saved.TryGet(tick, out var state) && state.Bodies.TryGetValue(entityId, out savedBody);
        }
    }

    /// <summary>한 틱에 저장된 몸 하나의 넉백 모드 상태.</summary>
    public struct KnockbackSavedBody
    {
        public bool IsGrounded;
        public long KnockbackEndTick;
        public int DamagePercent;
        public int Ammo;
        public long NextReloadTick;
        public long CooldownEndTick;
        public bool HasBomb;
    }
}
