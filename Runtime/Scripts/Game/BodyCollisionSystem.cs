using System.Collections.Generic;
using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 캐릭터끼리 부딪히면 서로 밀어내고 속도를 주고받는다(맵 장애물과 달리 정지 페널티 없이
    /// 자리싸움만). 몸↔맵 밀어내기는 <c>IMotionBridge.Depenetrate</c>가 맡고, 이쪽은 몸↔몸이다.
    /// 겹침은 <see cref="BodyOverlap"/>이 산수로 구하고, 속도 교환은 <see cref="VerticalBounce"/>가 맡는다.
    ///
    /// <para>게임 비종속이다 — 몸 규격과 반발계수를 숫자로 받는다. 값을 어디서 얻는지는 각 게임의
    /// LifetimeScope가 정한다.</para>
    ///
    /// <para><b>다른 게임에 가져다 쓸 때 갈아끼울 곳:</b> 속도 교환이
    /// <see cref="VerticalBounce"/>라 <b>세로 성분만</b> 오간다. 전진 속도가 상수라 손댈 수 없던
    /// 게임(Flappy Race)의 전제다. <b>가로로도 밀리는 게임은 이 단계를 접촉 법선 방향의 일반
    /// 충격량으로 바꿔야 한다.</b> 짝 순회·id 순서·절반씩 밀기·"양쪽 속도를 읽고 나서 쓴다"
    /// 규칙은 결정론을 위한 것이라 그대로 두는 편이 좋다.</para>
    /// </summary>
    public class BodyCollisionSystem
    {
        /// <summary>허용 겹침. 딱 붙는 지점까지 밀어내면 다음 틱에 또 파고들어 떤다.</summary>
        private const float Slop = 0.01f;

        private readonly float bodyRadius;
        private readonly float bodyHeight;
        private readonly float restitution;

        public BodyCollisionSystem(float bodyRadius, float bodyHeight, float restitution)
        {
            this.bodyRadius = bodyRadius;
            this.bodyHeight = bodyHeight;
            this.restitution = restitution;
        }

        /// <summary>
        /// 넘겨받은 몸들을 둘씩 모두 맞대어 겹친 짝을 푼다.
        /// 부르는 쪽은 <b>모두의 속도가 정해진 뒤</b> 한 번만 부르고, 목록을 엔티티 id 순으로 세워
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

        /// <summary>
        /// <paramref name="movers"/>는 위치·속도가 바뀌고 <paramref name="bodies"/>는 읽기 전용이다.
        /// 클라에서 원격 새는 서버가 굴리는 걸 외삽으로 그릴 뿐이라, 로컬에서 밀어내 봤자 다음
        /// 스냅샷이 오면 도로 튕겨 보인다 — 그래서 <b>자리를 옮기는 건</b> mover 쪽뿐이다.
        /// 다만 <b>미는 양은 양쪽 경우가 같다(절반)</b> — 서버도 내 새는 절반만 미니, 클라가 더 밀면
        /// 예측이 서버와 어긋나 보정이 계속 난다. 자세한 이유는 <see cref="ResolveOneSided"/> 참고.
        /// <paramref name="bodies"/>가 <paramref name="movers"/>와 같은 집합이면(서버가 그렇다 — 모든
        /// 새가 Simulated) 아래 2단계가 비어 1단계만 남고, 그 1단계는
        /// <see cref="Resolve(IReadOnlyList{GameFramework.World.Entity})"/>와 완전히 같은 일을 한다
        /// — 그래서 서버 동작은 지금과 같다.
        /// </summary>
        public void Resolve(IReadOnlyList<GameFramework.World.Entity> movers, IReadOnlyList<GameFramework.World.Entity> bodies)
        {
            // 1) movers끼리는 기존과 똑같이 양쪽 다 밀려난다.
            for (int i = 0; i < movers.Count; i++)
            {
                for (int j = i + 1; j < movers.Count; j++)
                {
                    ResolvePair(movers[i], movers[j]);
                }
            }

            // 2) movers가 아닌 상대(원격)와는 mover만 자리를 옮긴다(미는 양은 1단계와 같은 절반).
            //    "이미 1단계에서 처리했나"는 movers 목록 자체로 판단한다(엔티티가 어떤 컴포넌트를
            //    들고 있는지에 기대지 않는다 — 그래야 이 메서드가 넘겨받은 목록만으로 정확하다).
            for (int i = 0; i < movers.Count; i++)
            {
                for (int j = 0; j < bodies.Count; j++)
                {
                    var body = bodies[j];
                    if (ContainsReference(movers, body))
                    {
                        continue;
                    }
                    ResolveOneSided(movers[i], body);
                }
            }
        }

        private static bool ContainsReference(IReadOnlyList<GameFramework.World.Entity> list, GameFramework.World.Entity entity)
        {
            for (int i = 0; i < list.Count; i++)
            {
                if (ReferenceEquals(list[i], entity))
                {
                    return true;
                }
            }
            return false;
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
            if (!BodyOverlap.TryCompute(positionA, positionB, bodyRadius, bodyHeight,
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
            linearA.y = VerticalBounce.ResolveVy(beforeA, beforeB, pushDir.y, restitution);
            linearB.y = VerticalBounce.ResolveVy(beforeB, beforeA, -pushDir.y, restitution);
            velocityA.Linear = linearA.ToNumerics();
            velocityB.Linear = linearB.ToNumerics();
        }

        // mover 한쪽만 자리를 옮긴다. 다만 미는 양은 <see cref="ResolvePair"/>와 똑같이 절반이다 —
        // 서버는 두 마리 다 굴리므로 내 새를 절반만 민다. 클라가 겹침 전체를 떠안으면 내 예측이
        // 서버보다 절반만큼 앞서 나가고, 새가 붙어 있는 내내 그 차이가 보정으로 돌아온다(= 렉).
        // "지금 내 화면에서 완전히 안 겹치게"보다 "내 예측이 서버 답과 같게"가 우선이다.
        // 남의 새 몫 절반은 어차피 다음 스냅샷에 실려 온다.
        private void ResolveOneSided(GameFramework.World.Entity mover, GameFramework.World.Entity body)
        {
            var transformMover = mover.Get<GameFramework.World.Transform>();
            var transformBody = body.Get<GameFramework.World.Transform>();
            var velocityMover = mover.Get<GameFramework.World.Velocity>();
            var velocityBody = body.Get<GameFramework.World.Velocity>();
            if (transformMover == null || transformBody == null || velocityMover == null || velocityBody == null)
            {
                return;
            }

            Vector3 positionMover = transformMover.Position.ToUnity();
            Vector3 positionBody = transformBody.Position.ToUnity();
            if (!BodyOverlap.TryCompute(positionMover, positionBody, bodyRadius, bodyHeight,
                                              out Vector3 pushDir, out float depth))
            {
                return;
            }

            float half = Mathf.Max(depth - Slop, 0f) * 0.5f;
            transformMover.Position = (positionMover + pushDir * half).ToNumerics();

            // body 쪽 속도는 읽기만 한다 — 안 밀리는 쪽이니 세로 속도도 이 함수 밖에서는 그대로다.
            Vector3 linearMover = velocityMover.Linear.ToUnity();
            float vyBody = velocityBody.Linear.ToUnity().y;
            linearMover.y = VerticalBounce.ResolveVy(linearMover.y, vyBody, pushDir.y, restitution);
            velocityMover.Linear = linearMover.ToNumerics();
        }
    }
}
