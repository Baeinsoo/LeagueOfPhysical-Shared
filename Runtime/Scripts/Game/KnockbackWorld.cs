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
        private readonly KnockbackOwnHitPrediction ownHit;

        //  서버 KnockbackHitSystem과 같은 기준 — 이보다 낮으면 바닥 밖으로 빠져 떨어지는 중이라 표적이 아니다.
        private const float StandingMinY = -0.05f;

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
                              KnockbackPillarColliders pillarColliders = null,
                              KnockbackOwnHitPrediction ownHitPrediction = null)
            : base(entityRegistry, eventBuffer)
        {
            ownHit = ownHitPrediction;
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
        public bool IngestRemoteShot(in KnockbackShot shot) => IngestRemoteShot(shot, out _);

        /// <summary>
        /// 서버가 알려 준 남의 탄을 장부에 넣는다. 이미 알던 탄이면 false.
        /// 내 몸 맞음을 예측하는 월드(클라)에서는, 이 탄이 이미 지나간 틱에 내 몸에 닿았어야 했으면
        /// <paramref name="replayFrom"/>에 "이 틱 상태로 되감아 다시 계산하라"를 돌려준다 — 되감기 재생 중
        /// 맞음 판정이 그 틱에 맞음을 넣는다. 그 틱이 저장 범위 밖이면 이 탄은 예측하지 않고 서버 결과를 기다린다.
        /// </summary>
        public bool IngestRemoteShot(in KnockbackShot shot, out long? replayFrom)
        {
            replayFrom = null;
            var key = (shot.ShooterId, shot.FireTick);
            //  예측으로 이미 맞아 장부에서 빠진 탄이 다시 오면(재전송) 또 넣으면 두 번 맞는다.
            if (removedShots.Contains(key) || predictedHits.ContainsKey(key))
            {
                return false;
            }
            if (Shots.Add(shot) == false)
            {
                return false;
            }

            if (ownHit != null && TryFindPastOwnHit(shot, out long hitTick, out string bodyId))
            {
                long anchor = hitTick - 1;
                if (saved.TryGet(anchor, out var anchorState) && anchorState.Bodies.ContainsKey(bodyId)
                    && TryGetSavedMotion(anchor, bodyId, out _))
                {
                    replayFrom = anchor;
                }
                else
                {
                    unpredictedShots.Add(key);
                }
            }
            return true;
        }

        //  서버가 확정해 지운 탄(맞음). 되감기가 저장본에서 되살리지 못하게 키를 기억해 둔다.
        private readonly HashSet<(string shooterId, long fireTick)> removedShots = new HashSet<(string, long)>();

        //  내 몸이 맞았다고 예측해 장부에서 뺀 남의 탄과 그 틱. 되감기 상태다 — 그 틱보다 앞으로 되감으면
        //  탄을 장부로 돌려놓아, 재생이 같은 자리에서 다시 맞힌다(라이브 == 재생).
        private readonly Dictionary<(string shooterId, long fireTick), (KnockbackShot shot, long hitTick)> predictedHits
            = new Dictionary<(string, long), (KnockbackShot, long)>();

        //  맞았어야 할 틱이 저장 범위 밖이라 예측하지 않는 남의 탄. 서버 결과를 기다린다.
        private readonly HashSet<(string shooterId, long fireTick)> unpredictedShots = new HashSet<(string, long)>();

        public bool RemoveShot(string shooterId, long fireTick)
        {
            removedShots.Add((shooterId, fireTick));
            predictedHits.Remove((shooterId, fireTick));
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

        private readonly List<UnityEngine.Vector2> ownHitPillars = new List<UnityEngine.Vector2>();
        private readonly List<(string id, UnityEngine.Vector2 position)> ownHitTarget = new List<(string, UnityEngine.Vector2)>(1);
        private readonly List<(string id, UnityEngine.Vector2 position, int team)> ownHitBlastBody = new List<(string, UnityEngine.Vector2, int)>(1);
        private readonly KnockbackShot[] ownHitShot = new KnockbackShot[1];
        private readonly List<KnockbackShot> ownHitScratch = new List<KnockbackShot>();

        //  내 몸이 남의 탄에 맞는 것을 미리 적용한다(클라만 — 서버는 KnockbackHitSystem이 판정한다).
        //  서버가 이동이 다 끝난 뒤(End) 맞음을 넣으므로 같은 자리인 Detection에서, 서버와 같은 식으로 한다.
        //  맞음·위험도·탈락의 최종 결정은 서버 스냅샷이고, 어긋나면 보정이 고친다.
        protected override void Detection(long tick, float deltaTime)
        {
            if (ownHit == null)
            {
                return;
            }

            ForgetOldOwnHits(tick);
            if (HasStarted(tick) == false || Shots.Shots.Count == 0)
            {
                return;
            }

            bool pillarsCollected = false;
            foreach (var entity in EntityRegistry.All)
            {
                var transform = entity.Get<GameFramework.World.Transform>();
                var body = entity.Get<KnockbackBody>();
                var velocity = entity.Get<GameFramework.World.Velocity>();
                if (entity.Has<GameFramework.World.Simulated>() == false || transform == null || body == null || velocity == null
                    || transform.Position.Y < StandingMinY)
                {
                    continue;
                }

                if (pillarsCollected == false)
                {
                    CollectPillars(tick);
                    pillarsCollected = true;
                }

                var position = new UnityEngine.Vector2(transform.Position.X, transform.Position.Z);
                ownHitScratch.Clear();
                ownHitScratch.AddRange(Shots.Shots);
                foreach (var shot in ownHitScratch)
                {
                    if (HitsOwnBody(shot, entity.Id, position, tick, deltaTime) == false)
                    {
                        continue;
                    }

                    KnockbackRules.ApplyHit(body, velocity, OwnHitDirection(shot, entity.Id, position, tick, deltaTime), tick, config, deltaTime);
                    Shots.Remove(shot.ShooterId, shot.FireTick);
                    predictedHits[(shot.ShooterId, shot.FireTick)] = (shot, tick);
                }
            }
        }

        private void CollectPillars(long tick)
        {
            if (pillars != null)
            {
                pillars.CollectStanding(tick, ownHitPillars);
            }
            else
            {
                ownHitPillars.Clear();
            }
        }

        //  서버 KnockbackHitSystem.FindHits와 같은 판정을 이 몸 하나에 대해: 같은 팀 탄은 지나가고, 기둥이 먼저면 안 맞는다.
        //  남의 몸은 표적에 넣지 않는다 — 남의 몸이 탄을 먼저 막는 일은 서버만 안다(스냅샷 보정이 고친다).
        private bool HitsOwnBody(in KnockbackShot shot, string bodyId, UnityEngine.Vector2 position, long tick, float deltaTime)
        {
            if (shot.ShooterId == bodyId || tick < shot.FireTick
                || unpredictedShots.Contains((shot.ShooterId, shot.FireTick))
                || KnockbackTeams.SameTeam(ownHit.TeamOf(shot.ShooterId), ownHit.TeamOf(bodyId)))
            {
                return false;
            }

            ownHitTarget.Clear();
            ownHitTarget.Add((bodyId, position));
            ownHitShot[0] = shot;
            return KnockbackHits.Find(ownHitShot, ownHitTarget, ownHitPillars, pillars?.Radius ?? 0f, tick, config, deltaTime).Count > 0;
        }

        //  보통 탄은 탄 방향, 폭탄은 서버처럼 몸에 닿은 자리에서 터져 그 자리 → 몸 방향으로 민다.
        //  폭탄이 기둥·사거리 끝·남의 몸에서 터져 반경으로 미는 것은 예측하지 않는다(서버만 그 순서를 안다).
        private UnityEngine.Vector2 OwnHitDirection(in KnockbackShot shot, string bodyId, UnityEngine.Vector2 position, long tick, float deltaTime)
        {
            if (shot.IsBomb == false)
            {
                return shot.Direction;
            }

            var center = KnockbackHits.HitPoint(shot, position, tick, config, deltaTime);
            ownHitBlastBody.Clear();
            ownHitBlastBody.Add((bodyId, position, ownHit.TeamOf(bodyId)));
            var pushes = KnockbackBlast.Resolve(center, shot.Direction, config.BlastRadius, ownHitBlastBody, shot.ShooterId, ownHit.TeamOf(shot.ShooterId));
            return pushes.Count > 0 ? pushes[0].direction : shot.Direction;
        }

        //  되감기가 닿을 수 없을 만큼 오래된 기록은 버린다.
        private void ForgetOldOwnHits(long tick)
        {
            long oldest = tick - SaveCapacity;
            if (predictedHits.Count > 0)
            {
                ownHitKeys.Clear();
                foreach (var pair in predictedHits)
                {
                    if (pair.Value.hitTick < oldest)
                    {
                        ownHitKeys.Add(pair.Key);
                    }
                }
                foreach (var key in ownHitKeys)
                {
                    predictedHits.Remove(key);
                }
            }
            unpredictedShots.RemoveWhere(k => k.fireTick < oldest);
        }

        private readonly List<(string, long)> ownHitKeys = new List<(string, long)>();

        //  이미 저장된(지나간) 틱 중 이 탄이 예측하는 내 몸에 처음 닿는 틱을 찾는다. 그 틱에 몸이 있던 자리는 저장본에서 읽는다 —
        //  저장은 이동이 끝난 뒤라 Detection이 본 자리와 같다. 다 쓴 탄·기둥에 막힌 탄은 그 틱에서 멈춘다(월드가 지우는 차례와 같다).
        private bool TryFindPastOwnHit(in KnockbackShot shot, out long hitTick, out string bodyId)
        {
            hitTick = 0;
            bodyId = null;
            if (LatestSavedTick is not long latest)
            {
                return false;
            }

            float dt = KnockbackConfig.TickInterval;
            long start = System.Math.Max(shot.FireTick, GameplayStartTick);
            for (long t = start; t <= latest; t++)
            {
                if (shot.IsSpent(t, config.ShotSpeed, config.ShotRange, dt))
                {
                    return false;
                }

                CollectPillars(t);
                foreach (var entity in EntityRegistry.All)
                {
                    if (entity.Has<GameFramework.World.Simulated>() == false || entity.Has<KnockbackBody>() == false
                        || TryGetSavedMotion(t, entity.Id, out var motion) == false || motion.Position.Y < StandingMinY)
                    {
                        continue;
                    }
                    if (HitsOwnBody(shot, entity.Id, new UnityEngine.Vector2(motion.Position.X, motion.Position.Z), t, dt))
                    {
                        hitTick = t;
                        bodyId = entity.Id;
                        return true;
                    }
                }

                if (BlockedByPillar(shot, t, dt))
                {
                    return false;
                }
            }
            return false;
        }

        private bool BlockedByPillar(in KnockbackShot shot, long tick, float dt)
        {
            KnockbackHits.Segment(shot, tick, config, dt, out var from, out var to);
            float reach = (pillars?.Radius ?? 0f) + config.ShotRadius;
            foreach (var pillar in ownHitPillars)
            {
                if (KnockbackHitTest.SegmentHitsCircle(from, to, pillar, reach, out _))
                {
                    return true;
                }
            }
            return false;
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
            //  되감은 틱 뒤에 내 몸을 맞혔다고 예측한 남의 탄은 아직 날아가는 중이었다 — 장부로 돌려놓아 재생이 다시 판정하게 한다.
            if (predictedHits.Count > 0)
            {
                ownHitKeys.Clear();
                foreach (var pair in predictedHits)
                {
                    if (pair.Value.hitTick > tick)
                    {
                        ownHitKeys.Add(pair.Key);
                        Shots.Add(pair.Value.shot);
                    }
                }
                foreach (var key in ownHitKeys)
                {
                    predictedHits.Remove(key);
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

    /// <summary>
    /// 클라 월드가 "내 몸(예측하는 몸)이 남의 탄에 맞는 것"을 미리 적용하게 하는 설정.
    /// 서버 월드에는 주지 않는다 — 서버에선 모든 몸이 시뮬 대상이라 서버 판정과 두 번 맞게 된다.
    /// </summary>
    public sealed class KnockbackOwnHitPrediction
    {
        private readonly System.Func<string, int> teamOf;

        /// <param name="teamOf">몸 id → 팀(0 = 팀 없음). 같은 팀 탄은 지나가게 할 때 쓴다.</param>
        public KnockbackOwnHitPrediction(System.Func<string, int> teamOf)
        {
            this.teamOf = teamOf;
        }

        public int TeamOf(string entityId) => teamOf?.Invoke(entityId) ?? 0;
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
