using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Skydive의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 한 틱: ① 입력을 자세로 반영(축은 정해진 속도로만 움직인다) → ② 스태미나 소모·회복
    /// → ③ 자세가 정한 목표 속도로 이동.
    /// 레이저 판정은 Detection에 들어오지만(슬라이스 4) 지금은 비어 있다.
    /// </summary>
    public class SkydiveWorld : GameFramework.World.WorldBase
    {
        private readonly SkydiveMoveSystem _moveSystem;
        private readonly StaminaSystem _staminaSystem;
        private readonly SkydiveConfig _config;

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
            SkydiveConfig config)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
            _staminaSystem = staminaSystem;
            _config = config;
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
                // 임시 바닥에 닿아 있으면 "발 딛고 있다"로 본다 — 슬라이스 3이 이 판정을
                // 진짜 지면 접촉으로 바꾼다.
                var transform = _divers[i].Get<GameFramework.World.Transform>();
                bool grounded = transform != null && transform.Position.Y <= _config.GroundY + 0.01f;
                _staminaSystem.Tick(_divers[i], deltaTime, _config, grounded);
            }

            for (int i = 0; i < _divers.Count; i++)
            {
                _moveSystem.Tick(_divers[i], deltaTime, _config);
            }
        }

        // 입력이 자세를 바로 덮어쓰지 않는다 — 정해진 속도로만 움직인다. 그래야 자세가
        // 튀지 않고, 남을 예측하는 쪽의 오차도 완만해진다.
        private void ApplyPostureInput(GameFramework.World.Entity entity, float deltaTime)
        {
            var posture = entity.Get<Posture>();
            var command = entity.Get<InputBuffer>()?.Current;
            if (posture == null || command == null)
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
    }
}
