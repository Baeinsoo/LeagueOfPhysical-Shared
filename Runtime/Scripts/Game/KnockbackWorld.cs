using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 넉백 생존전의 시뮬 코어. 칸 바닥 위를 걷고, 칸이 없는 곳에서는 떨어진다.
    /// 위치·회전·속도는 베이스가 되감기 상태로 담고, 여기서는 몸별 접지 여부만 더 담는다.
    /// </summary>
    public class KnockbackWorld : GameFramework.World.WorldBase
    {
        private readonly KnockbackMoveSystem moveSystem;
        private readonly GameFramework.World.IMotionBridge motionBridge;

        private readonly GameFramework.Netcode.SequenceBuffer<Dictionary<string, bool>> saved
            = new GameFramework.Netcode.SequenceBuffer<Dictionary<string, bool>>(SaveCapacity);

        public KnockbackFloor Floor { get; }

        public KnockbackWorld(GameFramework.World.EntityRegistry entityRegistry,
                              GameFramework.World.WorldEventBuffer eventBuffer,
                              KnockbackMoveSystem moveSystem,
                              GameFramework.World.IMotionBridge motionBridge,
                              KnockbackFloor floor)
            : base(entityRegistry, eventBuffer)
        {
            this.moveSystem = moveSystem;
            this.motionBridge = motionBridge;
            Floor = floor;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
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
        }

        protected override void SaveGameState(long tick)
        {
            var grounded = new Dictionary<string, bool>();
            foreach (var entity in EntityRegistry.All)
            {
                var ground = entity.Get<GameFramework.World.GroundState>();
                if (ground != null && entity.Has<GameFramework.World.Simulated>())
                {
                    grounded[entity.Id] = ground.IsGrounded;
                }
            }
            saved.Record(tick, grounded);
        }

        protected override bool LoadGameState(long tick)
        {
            if (saved.TryGet(tick, out var grounded) == false)
            {
                return false;
            }

            foreach (var pair in grounded)
            {
                var ground = EntityRegistry.Get(pair.Key)?.Get<GameFramework.World.GroundState>();
                if (ground != null)
                {
                    ground.IsGrounded = pair.Value;
                }
            }
            return true;
        }
    }
}
