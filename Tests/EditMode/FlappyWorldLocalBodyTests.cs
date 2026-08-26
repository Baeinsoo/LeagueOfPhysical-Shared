using GameFramework;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // Task10: 클라에서 원격 새는 굴리지 않지만(외삽으로만 그린다) 부딪힘 상대는 돼야 한다.
    // 내 새만 밀려나고 원격은 그대로 있어야 한다 — 원격의 운명은 서버가 정한다.
    public class FlappyWorldLocalBodyTests
    {
        // 아무데도 안 부딪히는 빈 하늘 — 몸싸움만 보고 싶을 때 맵 충돌은 빼 둔다.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask) => CollisionHit.None;
        }

        [Test]
        public void 시뮬_대상이_아닌_새도_밀어내기_상대가_된다()
        {
            // 내 새(Simulated) 하나 + 원격(Simulated 없음) 하나를 겹쳐 둔다.
            var world = FlappyWorldFixture.CreateWithRemoteBird(new EmptySkyQuery(), out var mine, out var remote);
            world.GameplayStartTick = 0;   // 이 파일은 몸싸움 상대 여부를 다룬다, 출발 게이트가 아니다
            var remoteTransform = remote.Get<GameFramework.World.Transform>();
            var mineTransform = mine.Get<GameFramework.World.Transform>();
            mineTransform.Position = remoteTransform.Position;   // 완전히 겹침
            var remoteStart = remoteTransform.Position;

            world.Tick(1, 0.02f);

            // 내 새는 밀려났고, 원격은 그대로다(반작용 없음 — 서버가 정한다).
            Assert.That(mineTransform.Position, Is.Not.EqualTo(remoteTransform.Position));
            Assert.That(remoteTransform.Position, Is.EqualTo(remoteStart));

            // "그냥 떨어져서 우연히 달라진 것"이 아님을 값으로 못박는다.
            //   완전 겹침 0.9 - 허용겹침 0.01 = 0.89, 그 절반 0.445만큼 밀린다.
            //   + 중력 한 틱(70 * 0.02 * 0.02 = 0.028)
            // 절반인 이유: 서버는 두 마리 다 굴려 내 새를 절반만 민다. 클라가 전부 떠안으면 예측이
            // 서버보다 앞서 나가 새가 붙어 있는 내내 보정이 난다(FlappyBodyCollisionSystem 참고).
            // 밀어내기가 아예 없었다면 중력만으로 -0.028 근처에 머문다 — 자릿수가 달라 구분된다.
            Assert.That(mineTransform.Position.Y, Is.EqualTo(-0.473f).Within(1e-3f));
        }
    }
}
