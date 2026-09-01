using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;

namespace LOP
{
    /// <summary>
    /// Skydive의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 한 틱: ① 입력을 자세로 반영(축은 정해진 속도로만 움직인다) → ② 자세가 목표 속도를 정한다
    /// → ③ 맵에 막히면 벽까지만 옮긴다(미끄러짐·접지 판정) → ④ 방금 나온 접지로 스태미나 소모·회복.
    /// 레이저 판정은 Detection에 들어오지만(슬라이스 4) 지금은 비어 있다.
    /// </summary>
    public class SkydiveWorld : GameFramework.World.WorldBase
    {
        private readonly SkydiveMoveSystem _moveSystem;
        private readonly StaminaSystem _staminaSystem;
        private readonly SkydiveConfig _config;
        private readonly ICollisionQuery _collisionQuery;
        private readonly int _layerMask;

        // 매 틱 도는 코드라 목록을 새로 만들지 않고 비워서 다시 쓴다.
        private readonly List<GameFramework.World.Entity> _divers = new List<GameFramework.World.Entity>();

        // 자세·스태미나의 틱별 사진. 위치·속도는 WorldBase가 담는다.
        private readonly GameFramework.Netcode.SequenceBuffer<Dictionary<string, SkydiveSavedState>> _gameFrames
            = new GameFramework.Netcode.SequenceBuffer<Dictionary<string, SkydiveSavedState>>(SaveCapacity);

        public SkydiveWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            SkydiveMoveSystem moveSystem,
            StaminaSystem staminaSystem,
            SkydiveConfig config,
            ICollisionQuery collisionQuery,
            int layerMask)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
            _staminaSystem = staminaSystem;
            _config = config;
            _collisionQuery = collisionQuery;
            _layerMask = layerMask;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            CollectDivers();

            if (HasStarted(tick) == false)
            {
                // 출발 전. 속도를 명시적으로 0으로 둔다 — 스냅샷과 물리 팔로워가 이 값을 읽는다.
                for (int i = 0; i < _divers.Count; i++)
                {
                    var velocity = _divers[i].Get<GameFramework.World.Velocity>();
                    if (velocity != null)
                    {
                        velocity.Linear = System.Numerics.Vector3.Zero;
                    }
                }
                return;
            }

            for (int i = 0; i < _divers.Count; i++)
            {
                ApplyPostureInput(_divers[i], deltaTime);
            }

            for (int i = 0; i < _divers.Count; i++)
            {
                _moveSystem.Tick(_divers[i], deltaTime, _config);
            }

            // 속도가 전원 다 정해진 뒤에 옮긴다 — 슬라이스 6의 몸싸움이 이 사이에 들어온다(스펙 §5).
            for (int i = 0; i < _divers.Count; i++)
            {
                MoveBlockedByMap(_divers[i], deltaTime);
            }

            // 이동 뒤에 온다 — "발 딛고 있나"를 이동 커널이 방금 계산했기 때문이다.
            // 앞에 두면 한 틱 전 접지로 회복 여부를 정하게 된다.
            for (int i = 0; i < _divers.Count; i++)
            {
                bool grounded = _divers[i].Get<GameFramework.World.GroundState>()?.IsGrounded ?? false;
                _staminaSystem.Tick(_divers[i], deltaTime, _config, grounded);
            }
        }

        // 맵은 막는다 — KinematicMover가 벽까지만 옮기고 미끄러뜨린다(collide-and-slide).
        // 캡슐 규격은 CapsuleShape가 아니라 config에서 읽는다: 이 게임은 몸 크기도 튜닝값이라
        // 진실원본이 마스터데이터 한 곳이고, 크리에이터가 붙이는 CapsuleShape도 같은 값의 사본이다.
        private void MoveBlockedByMap(GameFramework.World.Entity entity, float deltaTime)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            if (transform == null || velocity == null)
            {
                return;
            }

            //  떨어지는 몸은 턱을 오를 일이 없다. 0을 주면 막혔을 때의 추가 sweep 3발도 안 쏜다.
            var result = KinematicMover.Move(new KinematicMoveInput(
                transform.Position.ToUnity(), velocity.Linear.ToUnity(),
                _config.BodyRadius, _config.BodyHeight, deltaTime,
                _layerMask, stepOffset: 0f), _collisionQuery);

            transform.Position = result.position.ToNumerics();
            // 막힌 축의 속도도 같이 지운다 — 안 지우면 다음 틱 수렴이 "막힌 적 없다는 듯"
            // 옛 속도 위에 계속 쌓인다(KinematicMoveSystem과 같은 관례).
            velocity.Linear = result.velocity.ToNumerics();

            var groundState = entity.Get<GameFramework.World.GroundState>();
            if (groundState != null)
            {
                groundState.IsGrounded = result.grounded;
            }
        }

        // 입력이 자세를 바로 덮어쓰지 않는다 — 정해진 속도로만 움직인다. 그래야 자세가
        // 튀지 않고, 남을 예측하는 쪽의 오차도 완만해진다.
        private void ApplyPostureInput(GameFramework.World.Entity entity, float deltaTime)
        {
            var posture = entity.Get<Posture>();
            var command = entity.Get<InputBuffer>()?.Current;
            if (posture == null)
            {
                return;
            }

            // 자세를 잡을 수 없는 상태면 슬라이더를 아무리 밀어도 안 먹고 대자로 되돌아간다.
            // 착지하면 패러세일이 저절로 접히는 것도 이 줄이 한다.
            if (CanPose(entity) == false)
            {
                posture.Axis = 0f;
                posture.Gliding = false;
                return;
            }

            if (command == null)
            {
                return;
            }

            float target = command.Posture < 0f ? 0f : (command.Posture > 1f ? 1f : command.Posture);
            float step = _config.PostureRate * deltaTime;
            float diff = target - posture.Axis;
            if (diff > step) { posture.Axis += step; }
            else if (diff < -step) { posture.Axis -= step; }
            else { posture.Axis = target; }

            if (command.Glide)
            {
                if (posture.Gliding == false)
                {
                    _staminaSystem.TryStartGlide(entity, _config);
                }
            }
            else
            {
                // 비상 펼침 창(EmergencyRemaining>0)이 도는 동안은 손을 떼도 접지 않는다 — 이 창은
                // 착지 직전 구제용으로 "보장된" 시간이다(스펙 §2.2). 여기서 접어 버리면 그 보장이
                // 손 떼는 순간 날아가 버린다. 창이 끝나는 것은 StaminaSystem.Tick이 스스로 접는다.
                var stamina = entity.Get<Stamina>();
                if (stamina == null || stamina.EmergencyRemaining <= 0f)
                {
                    posture.Gliding = false;
                }
            }
        }

        /// <summary>
        /// 지금 자세(대자·다이브·패러세일)를 잡을 수 있나. 젤다처럼 <b>공중에 충분히 떠 있을
        /// 때만</b> 잡을 수 있다 — 서서 패러세일을 펴거나, 지면 코앞에서 다이브에 들어갈 수는 없다.
        ///
        /// <para>발밑 여유는 매 틱 레이를 쏴서 다시 잰다. 상태를 안 들고 위치에서 다시 구하므로
        /// 되감아 재생해도 같은 답이 나온다.</para>
        /// </summary>
        private bool CanPose(GameFramework.World.Entity entity)
        {
            if (entity.Get<GameFramework.World.GroundState>()?.IsGrounded ?? false)
            {
                return false;
            }

            // 뛴 중에는 이미 조작이 잠겨 있다 — 자세만 따로 열어 줄 이유가 없다.
            if (entity.Get<JumpState>()?.IsJumping ?? false)
            {
                return false;
            }

            var transform = entity.Get<GameFramework.World.Transform>();
            if (transform == null)
            {
                return false;
            }

            // 발밑에서 아래로 쏴, 여유 안에 뭔가 있으면 자세를 못 잡는다.
            var hit = _collisionQuery.Raycast(
                transform.Position.ToUnity(), UnityEngine.Vector3.down, _config.PoseClearance, _layerMask);
            return hit.HasHit == false;
        }

        // id 순으로 세운다. 레지스트리 순회 순서는 정해져 있지 않은데, 처리 순서가 클·서에서
        // 같아야 두 쪽이 같은 결과에 이른다.
        private void CollectDivers()
        {
            _divers.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Get<EntityKind>()?.Kind != EntityType.Character)
                {
                    continue;
                }
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;   // 클라에서 남은 보간으로 그린다
                }
                _divers.Add(entity);
            }
            _divers.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        }

        protected override void SaveGameState(long tick)
        {
            var frame = new Dictionary<string, SkydiveSavedState>();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    frame[entity.Id] = SkydiveSavedState.Capture(entity);
                }
            }
            _gameFrames.Record(tick, frame);
        }

        protected override bool LoadGameState(long tick)
        {
            if (!_gameFrames.TryGet(tick, out var frame))
            {
                return false;
            }
            foreach (var pair in frame)
            {
                var entity = EntityRegistry.Get(pair.Key);
                if (entity != null)
                {
                    pair.Value.RestoreTo(entity);
                }
            }
            return true;
        }

        /// <summary>
        /// 그 틱에 내가 예측했던 자세·스태미나. 서버 스냅과 <b>같은 시점끼리</b> 비교하려면 필요하다 —
        /// 지금 살아 있는 값과 비교하면 클라가 앞서 달리는 구간 내내 시점이 어긋나 보인다.
        /// </summary>
        public bool TryGetSavedPosture(long tick, string entityId, out SkydiveSavedState state)
        {
            if (_gameFrames.TryGet(tick, out var frame) && frame.TryGetValue(entityId, out state))
            {
                return true;
            }
            state = default;
            return false;
        }
    }
}
