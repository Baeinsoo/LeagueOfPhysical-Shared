using GameFramework;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // 새끼리는 부딪히지 않는다 — 겹쳐도 서로 통과한다.
    //
    // 예전엔 겹치면 절반씩 밀어냈고, 그러려면 클라가 남의 새까지 굴려야 했다(안 굴리면 부딪힐
    // 상대의 자리를 모른다). 그런데 남의 입력은 오지 않아 "안 눌렀다"로 굴러, 상대가 날갯짓할
    // 때마다 크게 어긋났다. 몸싸움을 버리고 남의 새를 지연 스냅샷 보간으로 그리는 쪽을 택했다.
    // 몸싸움 코드 자체(BodyCollisionSystem 등)는 다른 게임이 써서 남아 있다 — 이 게임이
    // 부르지 않을 뿐이다.
    public class FlappyWorldBirdPassThroughTests
    {
        // 아무데도 안 부딪히는 빈 하늘 — 새끼리만 보고 싶을 때 맵 충돌은 빼 둔다.
        private class EmptySkyQuery : ICollisionQuery
        {
            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask) => CollisionHit.None;

            public CollisionHit Raycast(UnityEngine.Vector3 origin, UnityEngine.Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(UnityEngine.Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        [Test]
        public void 완전히_겹쳐도_서로_밀어내지_않는다()
        {
            var world = FlappyWorldFixture.CreateWithRemoteBird(new EmptySkyQuery(), out var mine, out var remote);
            world.GameplayStartTick = 0;   // 이 파일은 몸싸움 여부를 다룬다, 출발 게이트가 아니다
            var mineTransform = mine.Get<GameFramework.World.Transform>();
            var remoteTransform = remote.Get<GameFramework.World.Transform>();
            mineTransform.Position = remoteTransform.Position;   // 완전히 겹침

            world.Tick(1, 0.02f);

            //  중력 한 틱만큼만 내려간다: 속도 -70*0.02 = -1.4, 이동 -1.4*0.02 = -0.028.
            //  밀어내기가 살아 있다면 여기에 겹침 절반(0.445)이 더해져 -0.473이 된다 — 자릿수가
            //  달라 헷갈릴 수 없다. 이 값이 곧 "안 민다"의 증거다.
            Assert.That(mineTransform.Position.Y, Is.EqualTo(-0.028f).Within(1e-3f));
        }

        [Test]
        public void 시뮬_대상이_아닌_새는_아예_건드리지_않는다()
        {
            var world = FlappyWorldFixture.CreateWithRemoteBird(new EmptySkyQuery(), out var mine, out var remote);
            world.GameplayStartTick = 0;
            var mineTransform = mine.Get<GameFramework.World.Transform>();
            var remoteTransform = remote.Get<GameFramework.World.Transform>();
            mineTransform.Position = remoteTransform.Position;
            var remoteStart = remoteTransform.Position;

            world.Tick(1, 0.02f);

            //  남의 새는 클라가 굴리지 않는다 — 자리는 보간기가 서버 스냅으로 정한다.
            //  중력조차 먹지 않아야 한다(먹으면 보간이 그린 자리와 이중으로 어긋난다).
            Assert.That(remoteTransform.Position, Is.EqualTo(remoteStart));
        }
    }
}
