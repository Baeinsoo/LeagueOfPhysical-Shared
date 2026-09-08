using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class BodyCollisionSystemTests
    {
        const float Tolerance = 1e-4f;

        //  이 시스템은 게임을 모른다 — 몸 규격과 반발계수만 받는다.
        //  값은 Flappy Race가 쓰던 것 그대로라 아래 기대값도 그대로 유효하다.
        static BodyCollisionSystem System()
            => new BodyCollisionSystem(bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f);

        static Entity Bird(string id, Vector3 position, Vector3 velocity)
        {
            var entity = new Entity(id);
            entity.Add(new GameFramework.World.Transform { Position = position.ToNumerics() });
            entity.Add(new Velocity { Linear = velocity.ToNumerics() });
            return entity;
        }

        static Vector3 PositionOf(Entity e) => e.Get<GameFramework.World.Transform>().Position.ToUnity();
        static Vector3 VelocityOf(Entity e) => e.Get<Velocity>().Linear.ToUnity();

        [Test]
        public void 겹친_두_새가_절반씩_갈라진다()
        {
            var lower = Bird("bird-1", Vector3.zero, Vector3.zero);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), Vector3.zero);

            System().Resolve(new List<Entity> { lower, upper });

            // 겹침 0.4에서 허용 겹침 0.01을 뺀 0.39를 반씩 나눠 갖는다
            Assert.AreEqual(-0.195f, PositionOf(lower).y, Tolerance);
            Assert.AreEqual(0.695f, PositionOf(upper).y, Tolerance);
        }

        [Test]
        public void 부딪힌_세로_속도를_주고받는다()
        {
            var lower = Bird("bird-1", Vector3.zero, Vector3.zero);
            var upper = Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f));

            System().Resolve(new List<Entity> { lower, upper });

            // VerticalBounce와 같은 값 — 위는 덜 떨어지고 아래는 더 밀린다
            Assert.AreEqual(-3.25f, VelocityOf(upper).y, Tolerance);
            Assert.AreEqual(-6.75f, VelocityOf(lower).y, Tolerance);
        }

        [Test]
        public void 안_겹친_새는_건드리지_않는다()
        {
            var a = Bird("bird-1", Vector3.zero, new Vector3(0f, -10f, 0f));
            var b = Bird("bird-2", new Vector3(0f, 5f, 0f), Vector3.zero);

            System().Resolve(new List<Entity> { a, b });

            Assert.AreEqual(Vector3.zero, PositionOf(a));
            Assert.AreEqual(-10f, VelocityOf(a).y, Tolerance);
            Assert.AreEqual(0f, VelocityOf(b).y, Tolerance);
        }

        [Test]
        public void movers와_bodies가_같으면_기존_한쪽_인자_결과와_같다()
        {
            // 서버는 모든 새가 Simulated라 movers==bodies다. 그 경우 새 오버로드가
            // 기존 Resolve(단일 목록)와 정확히 같은 결과를 내야 서버 동작이 지금과 안 갈린다.
            var singleList = new List<Entity>
            {
                Bird("bird-1", Vector3.zero, Vector3.zero),
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
            };
            var sameList = new List<Entity>
            {
                Bird("bird-1", Vector3.zero, Vector3.zero),
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
            };

            System().Resolve(singleList);
            System().Resolve(sameList, sameList);

            // 두 겹친 새는 실제로 갈라지고 속도도 주고받아야 한다 — 이 확인이 없으면 두 오버로드가
            // "둘 다 아무 일도 안 했다"로 우연히 같아져도 아래 비교를 통과해 버린다.
            Assert.AreNotEqual(Vector3.zero, PositionOf(singleList[0]));
            Assert.AreNotEqual(new Vector3(0f, -10f, 0f), VelocityOf(singleList[1]));

            Assert.AreEqual(PositionOf(singleList[0]), PositionOf(sameList[0]));
            Assert.AreEqual(PositionOf(singleList[1]), PositionOf(sameList[1]));
            Assert.AreEqual(VelocityOf(singleList[0]), VelocityOf(sameList[0]));
            Assert.AreEqual(VelocityOf(singleList[1]), VelocityOf(sameList[1]));
        }

        [Test]
        public void 클라가_민_거리가_서버가_민_거리와_같다()
        {
            //  이 슬라이스에서 제일 중요한 불변식이다. 서버는 두 마리 다 굴려 내 새를 "절반" 밀고,
            //  클라는 내 새만 굴린다 — 이때 클라가 겹침 전체를 떠안으면 예측이 서버보다 절반만큼
            //  앞서 나가고, 새가 붙어 있는 내내 그 차이가 보정으로 돌아온다(실측된 렉의 정체).
            var serverMine = Bird("bird-1", Vector3.zero, Vector3.zero);
            var serverOther = Bird("bird-2", new Vector3(0f, 0.5f, 0f), Vector3.zero);
            var serverAll = new List<Entity> { serverMine, serverOther };
            System().Resolve(serverAll, serverAll);

            var clientMine = Bird("bird-1", Vector3.zero, Vector3.zero);
            var clientOther = Bird("bird-2", new Vector3(0f, 0.5f, 0f), Vector3.zero);
            System().Resolve(
                new List<Entity> { clientMine },
                new List<Entity> { clientMine, clientOther });

            Assert.AreEqual(PositionOf(serverMine).y, PositionOf(clientMine).y, Tolerance);
            Assert.AreEqual(VelocityOf(serverMine).y, VelocityOf(clientMine).y, Tolerance);
        }

        [Test]
        public void bodies에만_있는_상대는_밀리지_않고_mover만_밀린다()
        {
            var mover = Bird("bird-1", Vector3.zero, Vector3.zero);
            var remoteBody = Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f));
            var movers = new List<Entity> { mover };
            var bodies = new List<Entity> { mover, remoteBody };

            System().Resolve(movers, bodies);

            // mover는 밀려났고(원래 위치 0,0,0에서 벗어남), 원격 상대는 자리도 속도도 그대로다.
            Assert.AreNotEqual(Vector3.zero, PositionOf(mover));
            Assert.AreEqual(new Vector3(0f, 0.5f, 0f), PositionOf(remoteBody));
            Assert.AreEqual(new Vector3(0f, -10f, 0f), VelocityOf(remoteBody));
        }

        [Test]
        public void 두_새가_같은_충돌을_각자_보고_계산한다()
        {
            // 한쪽을 먼저 고쳐 놓고 다른 쪽이 그 새 값을 보면 순서가 결과를 바꾼다.
            // 목록 순서를 뒤집어도 결과가 같아야 클·서가 갈리지 않는다.
            var forward = new List<Entity>
            {
                Bird("bird-1", Vector3.zero, Vector3.zero),
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
            };
            var reversed = new List<Entity>
            {
                Bird("bird-2", new Vector3(0f, 0.5f, 0f), new Vector3(0f, -10f, 0f)),
                Bird("bird-1", Vector3.zero, Vector3.zero),
            };

            System().Resolve(forward);
            System().Resolve(reversed);

            Assert.AreEqual(VelocityOf(forward[0]).y, VelocityOf(reversed[1]).y, Tolerance);
            Assert.AreEqual(VelocityOf(forward[1]).y, VelocityOf(reversed[0]).y, Tolerance);
            Assert.AreEqual(PositionOf(forward[0]).y, PositionOf(reversed[1]).y, Tolerance);
            Assert.AreEqual(PositionOf(forward[1]).y, PositionOf(reversed[0]).y, Tolerance);
        }

        //  Skydive용 — 전 축으로 밀리는 몸.
        static BodyCollisionSystem FreeAxisSystem()
            => new BodyCollisionSystem(bodyRadius: 0.4f, bodyHeight: 1.8f,
                                       restitution: 0.35f, axisMask: Vector3.one);

        static Entity Diver(string id, Vector3 position, Vector3 velocity) => Bird(id, position, velocity);

        [Test]
        public void 전_축이면_가로로도_밀린다()
        {
            //  같은 높이에서 옆으로 파고든 둘. 세로만 다루던 옛 동작에서는 x가 안 변했다.
            var left = Diver("diver-1", Vector3.zero, new Vector3(6f, 0f, 0f));
            var right = Diver("diver-2", new Vector3(0.5f, 0f, 0f), Vector3.zero);

            FreeAxisSystem().Resolve(new List<Entity> { left, right });

            Assert.Less(VelocityOf(left).x, 6f);
            Assert.Greater(VelocityOf(right).x, 0f);
        }

        [Test]
        public void 세로_마스크는_가로_속도를_남긴다()
        {
            var left = Diver("diver-1", Vector3.zero, new Vector3(6f, 0f, 0f));
            var right = Diver("diver-2", new Vector3(0.5f, 0f, 0f), Vector3.zero);

            new BodyCollisionSystem(0.4f, 1.8f, 0.35f, Vector3.up)
                .Resolve(new List<Entity> { left, right });

            Assert.AreEqual(6f, VelocityOf(left).x, Tolerance);
            Assert.AreEqual(0f, VelocityOf(right).x, Tolerance);
        }

        [Test]
        public void 위에_있는_쪽만_아래로_닿았다고_보고한다()
        {
            //  세로 간격 1.6은 일부러 고른 값이다. 캡슐 심 선분이 길이 1.0이라 간격이 1.0 이하면
            //  두 선분이 겹쳐 거리가 0이 되고, BodyOverlap이 기하 대신 "규칙으로" Vector3.down을
            //  돌려주는 예외 분기로 빠진다 — 그러면 이 테스트가 판별을 시험하지 않고 통과한다.
            //  진짜 세로 법선은 간격이 (1.0, 1.8)일 때 나온다(1.8 = 몸 높이 = 머리 위에 선 자세).
            var lower = Diver("diver-1", Vector3.zero, Vector3.zero);
            var upper = Diver("diver-2", new Vector3(0f, 1.6f, 0f), new Vector3(0f, -10f, 0f));

            var groundedIds = FreeAxisSystem().Resolve(new List<Entity> { lower, upper });

            Assert.IsTrue(groundedIds.Contains("diver-2"));
            Assert.IsFalse(groundedIds.Contains("diver-1"));
        }

        [Test]
        public void 옆으로만_부딪히면_아무도_아래로_닿지_않았다()
        {
            var left = Diver("diver-1", Vector3.zero, new Vector3(6f, 0f, 0f));
            var right = Diver("diver-2", new Vector3(0.5f, 0f, 0f), Vector3.zero);

            var groundedIds = FreeAxisSystem().Resolve(new List<Entity> { left, right });

            Assert.AreEqual(0, groundedIds.Count);
        }

        [Test]
        public void 짝을_넘기는_순서가_결과를_바꾸지_않는다()
        {
            var a1 = Diver("diver-1", Vector3.zero, new Vector3(0f, -4f, 0f));
            var b1 = Diver("diver-2", new Vector3(0.3f, 0.9f, 0f), new Vector3(0f, -12f, 0f));
            FreeAxisSystem().Resolve(new List<Entity> { a1, b1 });

            var a2 = Diver("diver-1", Vector3.zero, new Vector3(0f, -4f, 0f));
            var b2 = Diver("diver-2", new Vector3(0.3f, 0.9f, 0f), new Vector3(0f, -12f, 0f));
            FreeAxisSystem().Resolve(new List<Entity> { b2, a2 });

            Assert.AreEqual(VelocityOf(a1), VelocityOf(a2));
            Assert.AreEqual(VelocityOf(b1), VelocityOf(b2));
            Assert.AreEqual(PositionOf(a1), PositionOf(a2));
            Assert.AreEqual(PositionOf(b1), PositionOf(b2));
        }

        [Test]
        public void 가로속도가_세로답을_오염시키지_않는다()
        {
            var lower = Diver("d1", Vector3.zero, new Vector3(20f, 0f, 0f));
            var upper = Diver("d2", new Vector3(0f, 1.0f, 0f), new Vector3(0f, -10f, 0f));

            new BodyCollisionSystem(0.4f, 1.8f, 0.35f, Vector3.up)
                .Resolve(new List<Entity> { lower, upper });

            //  가로 속도 20은 세로 답에 영향을 주면 안 된다.
            Assert.AreEqual(-3.25f, VelocityOf(upper).y, Tolerance);
        }
    }
}
