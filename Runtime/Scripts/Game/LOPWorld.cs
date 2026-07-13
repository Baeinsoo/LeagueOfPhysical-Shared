namespace LOP
{
    public class LOPWorld : GameFramework.World.WorldBase
    {
        private readonly MovementSystem _movementSystem;
        private readonly AbilitySystem _abilitySystem;
        private readonly StatusEffectSystem _statusEffectSystem;
        private readonly AbilityEffectExecutor _abilityEffectExecutor;
        private readonly KinematicMoveSystem _kinematicMoveSystem;
        private readonly GameFramework.IMotionBridge _motionBridge;

        public LOPWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            MovementSystem movementSystem,
            AbilitySystem abilitySystem,
            StatusEffectSystem statusEffectSystem,
            AbilityEffectExecutor abilityEffectExecutor,
            KinematicMoveSystem kinematicMoveSystem,
            GameFramework.IMotionBridge motionBridge)
            : base(entityRegistry, eventBuffer)
        {
            _movementSystem = movementSystem;
            _abilitySystem = abilitySystem;
            _statusEffectSystem = statusEffectSystem;
            _abilityEffectExecutor = abilityEffectExecutor;
            _kinematicMoveSystem = kinematicMoveSystem;
            _motionBridge = motionBridge;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
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
                    _motionBridge.Depenetrate(entity.Id);
                    _kinematicMoveSystem.Tick(entity, deltaTime);
                    _motionBridge.PushMotion(entity.Id);
                }
            }
        }
    }
}
