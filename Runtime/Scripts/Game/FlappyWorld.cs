using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Flappy Race의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 한 틱: ① 유령정지 시간 감소 → ② 속도(중력·플랩·고정 전진, 멈춰 있으면 스킵) → ③ 새끼리
    /// 몸싸움 → ④ 맵은 막지 않고 통과 + 부딪히면 유령정지 진입.
    /// ③을 전원의 ② 뒤에 두는 이유는, 한 마리씩 처리하면 먼저 나온 새가 아직 갱신되지 않은
    /// 상대 속도를 보게 돼 순서가 결과를 가르기 때문이다.
    /// </summary>
    public class FlappyWorld : GameFramework.World.WorldBase
    {
        private readonly FlappyMoveSystem _moveSystem;
        private readonly FlappyBodyCollisionSystem _bodyCollisionSystem;
        private readonly FlappyGhostSystem _ghostSystem;
        private readonly ICollisionQuery _collisionQuery;
        private readonly GameFramework.World.IMotionBridge _motionBridge;
        private readonly int _layerMask;

        // 매 틱 도는 코드라 목록을 새로 만들지 않고 비워서 다시 쓴다.
        private readonly List<GameFramework.World.Entity> _birds = new List<GameFramework.World.Entity>();

        public FlappyWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            FlappyMoveSystem moveSystem,
            FlappyBodyCollisionSystem bodyCollisionSystem,
            FlappyGhostSystem ghostSystem,
            ICollisionQuery collisionQuery,
            GameFramework.World.IMotionBridge motionBridge,
            int layerMask)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
            _bodyCollisionSystem = bodyCollisionSystem;
            _ghostSystem = ghostSystem;
            _collisionQuery = collisionQuery;
            _motionBridge = motionBridge;
            _layerMask = layerMask;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            CollectBirds();

            // 시간 감소가 먼저다. 이번 틱에 풀릴 새는 이번 틱부터 움직인다.
            for (int i = 0; i < _birds.Count; i++)
            {
                _ghostSystem.Tick(_birds[i], deltaTime);
            }

            for (int i = 0; i < _birds.Count; i++)
            {
                if (_ghostSystem.IsStopped(_birds[i]))
                {
                    // 멈춰 있는 새는 전진도 하지 않는다 — 시간 손실이 이 게임의 페널티다.
                    _birds[i].Get<GameFramework.World.Velocity>().Linear = System.Numerics.Vector3.Zero;
                    continue;
                }
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

        // 맵은 더는 막지 않는다. 부딪혔는지만 보고 유령으로 넘긴다 —
        // 전진 속도가 고정이라 "막기"로는 벽에 박힌 새가 수평으로 영영 빠져나오지 못한다.
        private void MoveThroughMap(GameFramework.World.Entity entity, float deltaTime)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var body = entity.Get<GameFramework.World.CapsuleShape>();
            if (transform == null || velocity == null || body == null)
            {
                return;
            }

            Vector3 start = transform.Position.ToUnity();
            Vector3 delta = velocity.Linear.ToUnity() * deltaTime;

            if (delta.sqrMagnitude > 0f)
            {
                // 캡슐 끝점 규약은 KinematicMover.Cast와 같다 — position은 발밑 기준.
                Vector3 p1 = start + Vector3.up * body.Radius;
                Vector3 p2 = start + Vector3.up * (body.Height - body.Radius);
                var hit = _collisionQuery.CapsuleCast(
                    p1, p2, body.Radius, delta.normalized, delta.magnitude, _layerMask);
                if (hit.HasHit)
                {
                    _ghostSystem.Enter(entity);
                }
            }

            transform.Position = (start + delta).ToNumerics();
            _motionBridge.PushMotion(entity);
        }
    }
}
