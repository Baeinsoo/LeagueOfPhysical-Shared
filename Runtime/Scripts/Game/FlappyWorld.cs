using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;

namespace LOP
{
    /// <summary>
    /// Flappy Race의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 한 틱: ① 속도(중력·플랩·고정 전진) → ② 새끼리 몸싸움 → ③ 맵에 막히며 이동.
    /// ②를 전원의 ① 뒤에 두는 이유는, 한 마리씩 처리하면 먼저 나온 새가 아직 갱신되지 않은
    /// 상대 속도를 보게 돼 순서가 결과를 가르기 때문이다.
    /// </summary>
    public class FlappyWorld : GameFramework.World.WorldBase
    {
        private readonly FlappyMoveSystem _moveSystem;
        private readonly FlappyBodyCollisionSystem _bodyCollisionSystem;
        private readonly ICollisionQuery _collisionQuery;
        private readonly GameFramework.World.IMotionBridge _motionBridge;
        private readonly FlappyConfig _config;
        private readonly int _layerMask;

        // 매 틱 도는 코드라 목록을 새로 만들지 않고 비워서 다시 쓴다.
        private readonly List<GameFramework.World.Entity> _birds = new List<GameFramework.World.Entity>();

        public FlappyWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            FlappyMoveSystem moveSystem,
            FlappyBodyCollisionSystem bodyCollisionSystem,
            ICollisionQuery collisionQuery,
            GameFramework.World.IMotionBridge motionBridge,
            FlappyConfig config,
            int layerMask)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
            _bodyCollisionSystem = bodyCollisionSystem;
            _collisionQuery = collisionQuery;
            _motionBridge = motionBridge;
            _config = config;
            _layerMask = layerMask;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            CollectBirds();

            for (int i = 0; i < _birds.Count; i++)
            {
                _moveSystem.Tick(_birds[i], deltaTime);
            }

            // 전원의 속도가 정해진 뒤 한 번(페이즈 배리어). 새끼리 겹침은 여기서 다 풀리므로
            // 아래 물리 브릿지의 Separate는 부르지 않는다.
            _bodyCollisionSystem.Resolve(_birds);

            // 스크립트로 옮긴 자리를 물리에 먼저 알려야 sweep이 한 틱 전 자리에서 이뤄지지 않는다.
            _motionBridge.SyncTransforms();
            for (int i = 0; i < _birds.Count; i++)
            {
                MoveThroughMap(_birds[i], deltaTime);
            }
        }

        // 시뮬 대상만 모아 id 순으로 세운다. 레지스트리 순회 순서는 정해져 있지 않은데,
        // 몸싸움을 푸는 순서가 클·서에서 같아야 두 쪽이 같은 결과에 이른다.
        private void CollectBirds()
        {
            _birds.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    _birds.Add(entity);
                }
            }
            _birds.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        }

        private void MoveThroughMap(GameFramework.World.Entity entity, float deltaTime)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            if (transform == null || velocity == null)
            {
                return;
            }

            _motionBridge.Depenetrate(entity);

            var result = KinematicMover.Move(new KinematicMoveInput(
                transform.Position.ToUnity(), velocity.Linear.ToUnity(),
                _config.BodyRadius, _config.BodyHeight, deltaTime, _layerMask), _collisionQuery);

            transform.Position = result.position.ToNumerics();
            velocity.Linear = result.velocity.ToNumerics();

            _motionBridge.PushMotion(entity);
        }
    }
}
