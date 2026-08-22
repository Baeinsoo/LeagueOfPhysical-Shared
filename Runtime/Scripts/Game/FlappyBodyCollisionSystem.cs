using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 새끼리 부딪히면 서로 밀어내고 세로 속도를 주고받는다(맵 장애물과 달리 정지 페널티 없이 자리싸움만).
    /// 겹침은 <see cref="FlappyBodyOverlap"/>이 산수로 구하고, 속도 교환은 <see cref="FlappyBounce"/>가 맡는다.
    /// </summary>
    public class FlappyBodyCollisionSystem
    {
        /// <summary>허용 겹침. 딱 붙는 지점까지 밀어내면 다음 틱에 또 파고들어 떤다.</summary>
        private const float Slop = 0.01f;

        private readonly FlappyConfig config;

        public FlappyBodyCollisionSystem(FlappyConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// 넘겨받은 새들을 두 마리씩 모두 맞대어 겹친 짝을 푼다.
        /// 부르는 쪽은 <b>모든 새의 속도가 정해진 뒤</b> 한 번만 부르고, 목록을 엔티티 id 순으로 세워
        /// 넘긴다 — 푸는 순서가 클·서에서 같아야 두 쪽이 같은 결과에 이른다.
        /// </summary>
        public void Resolve(IReadOnlyList<GameFramework.World.Entity> birds)
        {
            for (int i = 0; i < birds.Count; i++)
            {
                for (int j = i + 1; j < birds.Count; j++)
                {
                    ResolvePair(birds[i], birds[j]);
                }
            }
        }

        private void ResolvePair(GameFramework.World.Entity a, GameFramework.World.Entity b)
        {
            var transformA = a.Get<GameFramework.World.Transform>();
            var transformB = b.Get<GameFramework.World.Transform>();
            var velocityA = a.Get<GameFramework.World.Velocity>();
            var velocityB = b.Get<GameFramework.World.Velocity>();
            if (transformA == null || transformB == null || velocityA == null || velocityB == null)
            {
                return;
            }

            Vector3 positionA = transformA.Position.ToUnity();
            Vector3 positionB = transformB.Position.ToUnity();
            if (!FlappyBodyOverlap.TryCompute(positionA, positionB, config.BodyRadius, config.BodyHeight,
                                              out Vector3 pushDir, out float depth))
            {
                return;
            }

            // 절반씩 — 양쪽을 합쳐야 완전히 떨어진다.
            float half = Mathf.Max(depth - Slop, 0f) * 0.5f;
            transformA.Position = (positionA + pushDir * half).ToNumerics();
            transformB.Position = (positionB - pushDir * half).ToNumerics();

            // 둘 다 부딪히기 *전* 속도를 보고 계산한다 — 한쪽을 먼저 고쳐 놓고 다른 쪽이 그 값을 보면
            // 짝을 어느 순서로 넘겼는지가 결과를 바꿔 클·서가 갈린다.
            Vector3 linearA = velocityA.Linear.ToUnity();
            Vector3 linearB = velocityB.Linear.ToUnity();
            float beforeA = linearA.y;
            float beforeB = linearB.y;
            linearA.y = FlappyBounce.ResolveVy(beforeA, beforeB, pushDir.y, config.Restitution);
            linearB.y = FlappyBounce.ResolveVy(beforeB, beforeA, -pushDir.y, config.Restitution);
            velocityA.Linear = linearA.ToNumerics();
            velocityB.Linear = linearB.ToNumerics();
        }
    }
}
