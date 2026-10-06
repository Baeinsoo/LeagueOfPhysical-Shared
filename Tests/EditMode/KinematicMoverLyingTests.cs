using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 누운 캡슐(Task 1 CapsuleShape.Lying, Axis=X)로 움직일 때 KinematicMover가 세로가 아니라
    /// 가로 치수로 벽·천장에 닿는지 본다. 맵은 <see cref="KinematicMoverSlopeTests"/>와 같은 방식
    /// (HalfSpaceQuery)으로 만든다.
    /// </summary>
    public class KinematicMoverLyingTests
    {
        //  FlapWang 새가 누우면 쓸 치수: 반지름 0.45, 세로 두께 0.9(=반지름×2), 가로 전체 길이 1.28.
        const float Radius = 0.45f;
        const float Height = 0.9f;
        const float LyingLength = 1.28f;
        const float DeltaTime = 0.02f;
        const float Speed = 10f;

        [Test]
        public void 누운_몸은_옆으로_064에서_멈춘다()
        {
            //  벽 x=2 — 빈 공간은 x<=2 쪽(법선이 -x).
            var map = new HalfSpaceQuery();
            map.Faces.Add(new HalfSpaceQuery.Face
            {
                Point = new Vector3(2f, 0f, 0f),
                Normal = new Vector3(-1f, 0f, 0f),
            });

            Vector3 pos = Vector3.zero;
            Vector3 vel = new Vector3(Speed, 0f, 0f);

            for (int tick = 0; tick < 60; tick++)
            {
                var result = KinematicMover.Move(
                    new KinematicMoveInput(pos, vel, Radius, Height, DeltaTime, ~0, stepOffset: 0f,
                        groundProbe: 0f, lyingLength: LyingLength), map);
                pos = result.position;
                vel = result.velocity;
            }

            Assert.That(pos.x, Is.EqualTo(2f - 0.64f).Within(0.02f),
                "누운 몸은 발밑 기준 +0.64m 떨어진 데서 벽에 닿아야 한다(가로로 긴 쪽이 먼저 닿는다)");
        }

        [Test]
        public void 누운_몸은_천장에_09에서_멈춘다()
        {
            //  천장 y=3 — 빈 공간은 y<=3 쪽(법선이 -y).
            var map = new HalfSpaceQuery();
            map.Faces.Add(new HalfSpaceQuery.Face
            {
                Point = new Vector3(0f, 3f, 0f),
                Normal = new Vector3(0f, -1f, 0f),
            });

            Vector3 pos = Vector3.zero;
            Vector3 vel = new Vector3(0f, Speed, 0f);

            for (int tick = 0; tick < 60; tick++)
            {
                var result = KinematicMover.Move(
                    new KinematicMoveInput(pos, vel, Radius, Height, DeltaTime, ~0, stepOffset: 0f,
                        groundProbe: 0f, lyingLength: LyingLength), map);
                pos = result.position;
                vel = result.velocity;
            }

            //  누운 캡슐의 세로 두께는 Height(=반지름×2)다 — 세운 캡슐처럼 전체 Height가 아니라
            //  딱 지름만큼만 발밑 위로 올라간다.
            Assert.That(pos.y, Is.EqualTo(3f - Height).Within(0.02f),
                "누운 몸은 발밑 기준 +0.9m(지름) 떨어진 데서 천장에 닿아야 한다");
        }

        [Test]
        public void 길이_0이면_예전_세운_캡슐과_같다()
        {
            //  (1) 단순 낙하: lyingLength를 생략한 것과 명시적으로 0을 준 것이 완전히 같아야 한다.
            var groundMapA = new HalfSpaceQuery();
            groundMapA.AddGround(0f);
            var withoutArg = KinematicMover.Move(
                new KinematicMoveInput(new Vector3(0f, 0.5f, 0f), new Vector3(0f, -5f, 0f),
                    Radius, Height, DeltaTime, ~0, stepOffset: 0f), groundMapA);

            var groundMapB = new HalfSpaceQuery();
            groundMapB.AddGround(0f);
            var withZero = KinematicMover.Move(
                new KinematicMoveInput(new Vector3(0f, 0.5f, 0f), new Vector3(0f, -5f, 0f),
                    Radius, Height, DeltaTime, ~0, stepOffset: 0f, lyingLength: 0f), groundMapB);

            Assert.That(withZero.position, Is.EqualTo(withoutArg.position),
                "lyingLength 생략과 명시적 0은 같은 위치여야 한다(낙하)");
            Assert.That(withZero.velocity, Is.EqualTo(withoutArg.velocity));
            Assert.That(withZero.grounded, Is.EqualTo(withoutArg.grounded));

            //  (2) 옆 벽에 부딪히는 경로 — 수평 sweep(Cast)이 CapsuleEnds.Of를 쓰도록 바뀌었으니
            //      그 경로도 똑같이 확인한다. 여러 틱에 걸쳐 실제로 벽에 닿을 때까지 굴린다.
            var wallMapA = new HalfSpaceQuery();
            wallMapA.Faces.Add(new HalfSpaceQuery.Face { Point = new Vector3(2f, 0f, 0f), Normal = new Vector3(-1f, 0f, 0f) });
            Vector3 posA = Vector3.zero;
            Vector3 velA = new Vector3(Speed, 0f, 0f);
            for (int tick = 0; tick < 60; tick++)
            {
                var r = KinematicMover.Move(
                    new KinematicMoveInput(posA, velA, Radius, Height, DeltaTime, ~0, stepOffset: 0f), wallMapA);
                posA = r.position;
                velA = r.velocity;
            }

            var wallMapB = new HalfSpaceQuery();
            wallMapB.Faces.Add(new HalfSpaceQuery.Face { Point = new Vector3(2f, 0f, 0f), Normal = new Vector3(-1f, 0f, 0f) });
            Vector3 posB = Vector3.zero;
            Vector3 velB = new Vector3(Speed, 0f, 0f);
            for (int tick = 0; tick < 60; tick++)
            {
                var r = KinematicMover.Move(
                    new KinematicMoveInput(posB, velB, Radius, Height, DeltaTime, ~0, stepOffset: 0f, lyingLength: 0f), wallMapB);
                posB = r.position;
                velB = r.velocity;
            }

            Assert.That(posB, Is.EqualTo(posA), "lyingLength 생략과 명시적 0은 같은 위치여야 한다(벽에 부딪힘)");
            Assert.That(velB, Is.EqualTo(velA));
        }
    }
}
