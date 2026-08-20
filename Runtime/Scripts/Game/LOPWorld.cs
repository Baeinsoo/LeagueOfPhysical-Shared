namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        private readonly MovementSystem _movementSystem;
        private readonly AbilitySystem _abilitySystem;
        private readonly StatusEffectSystem _statusEffectSystem;
        private readonly AbilityEffectExecutor _abilityEffectExecutor;
        private readonly KinematicMoveSystem _kinematicMoveSystem;
        private readonly GameFramework.World.IMotionBridge _motionBridge;
        private readonly AbilityActivator _abilityActivator;

        // 스킬·상태이상·스탯·마나의 틱별 사진. 위치·속도는 WorldBase가 담는다.
        private readonly GameFramework.Netcode.SequenceBuffer<System.Collections.Generic.Dictionary<string, LOPSavedState>> _gameFrames
            = new GameFramework.Netcode.SequenceBuffer<System.Collections.Generic.Dictionary<string, LOPSavedState>>(SaveCapacity);

        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            MovementSystem movementSystem,
            AbilitySystem abilitySystem,
            StatusEffectSystem statusEffectSystem,
            AbilityEffectExecutor abilityEffectExecutor,
            KinematicMoveSystem kinematicMoveSystem,
            GameFramework.World.IMotionBridge motionBridge,
            AbilityActivator abilityActivator)
            : base(entityRegistry, eventBuffer)
        {
            _movementSystem = movementSystem;
            _abilitySystem = abilitySystem;
            _statusEffectSystem = statusEffectSystem;
            _abilityEffectExecutor = abilityEffectExecutor;
            _kinematicMoveSystem = kinematicMoveSystem;
            _motionBridge = motionBridge;
            _abilityActivator = abilityActivator;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            // 입력에 실린 어빌리티 발동. 이동보다 먼저 해야 한다 — 대시 발동 틱의 입력 게이트가
            // 이 순서에 걸려 있고, 라이브(입력 캡처 → world.Tick)와 재생의 순서가 같아야 결과가 갈리지 않는다.
            foreach (var entity in EntityRegistry.All)
            {
                if (!entity.Has<GameFramework.World.Simulated>())
                {
                    continue;
                }
                var command = entity.Get<InputBuffer>()?.Current;
                if (command != null && command.AbilityId != 0)
                {
                    _abilityActivator.TryActivate(entity.Id, command.AbilityId, tick);
                }
            }

            // 이동은 어빌리티 페이즈 전진보다 먼저 — 대시 발동 틱의 입력 게이트 타이밍이 이 순서에 걸려 있다.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _movementSystem.Tick(entity, tick, deltaTime);
                }
            }

            // 어빌리티 페이즈 전진(Active 진입 시 효과 적용) + 상태이상 만료.
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _abilitySystem.Tick(entity, tick);
                    _statusEffectSystem.Tick(entity, tick);
                }
            }

            // 페이즈 전진 후 active 창 effect 구동(대시 push·데미지·상태효과). 이전엔 host DriveAbilityEffects.
            // cross-entity 판정(서버 데미지/넉백)이 "전원 이동 후"를 보도록 별도 루프(페이즈 배리어).
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _abilityEffectExecutor.DriveActiveEntity(entity, tick);
                }
            }

            // 키네마틱 이동(중력+collide-and-slide). host 물리 브릿지로 겹침해소·rb 반영을 사이드 무관 호출.
            // 이전엔 host MoveCharacters/MoveLocalPlayer. 서버=전 캐릭(전 Simulated) / 클라=내 캐릭만.
            _motionBridge.SyncTransforms();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _motionBridge.Depenetrate(entity);
                    _motionBridge.Separate(entity);
                    _kinematicMoveSystem.Tick(entity, deltaTime);
                    _motionBridge.PushMotion(entity);
                }
            }
        }

        protected override void SaveGameState(long tick)
        {
            var frame = new System.Collections.Generic.Dictionary<string, LOPSavedState>();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    frame[entity.Id] = LOPSavedState.Capture(entity);
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
        /// 그 틱에 내가 예측했던 상태이상 목록. 서버 스냅과 <b>같은 시점끼리</b> 비교해야 해서
        /// 클라 보정이 읽는다 — 지금 살아있는 목록과 비교하면 클라가 서버보다 앞서 달리는 만큼
        /// 늘 달라 보여 불필요한 되돌리기가 매 스냅마다 일어난다.
        /// </summary>
        public bool TryGetSavedStatusEffects(long tick, string entityId, out System.Collections.Generic.List<ActiveEffect> effects)
        {
            effects = null;
            if (_gameFrames.TryGet(tick, out var frame) && frame.TryGetValue(entityId, out var saved))
            {
                effects = saved.StatusEffects;
                return true;
            }
            return false;
        }
    }
}
