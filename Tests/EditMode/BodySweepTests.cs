using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 한 틱을 훑어 "처음 닿은 순간"의 접촉 방향을 찾는 <see cref="BodySweep"/>.
    /// 재는 것은 <b>방향</b>뿐이다 — 밀어내기와 속도 교환은 여전히 틱이 끝난 모습에서 한다.
    /// </summary>
    public class BodySweepTests
    {
        //  Skydive 몸 규격. 심(축) 선분 길이 = 1.8 − 0.8 = 1.0 m.
        const float Radius = 0.4f;
        const float Height = 1.8f;

        [Test]
        public void 멀찍이_스쳐_지나가면_안_닿는다()
        {
            bool hit = BodySweep.TryContactNormal(
                Vector3.zero, Vector3.zero,
                new Vector3(5f, 3f, 0f), new Vector3(5f, 1.2f, 0f),
                Radius, Height, out _, out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void 서로_안_움직이면_훑을_것이_없다()
        {
            //  상대 이동이 0이면 틱 끝 모습이 곧 시작 모습이다 — 부르는 쪽이 그 모습으로 푼다.
            bool hit = BodySweep.TryContactNormal(
                Vector3.zero, Vector3.zero,
                new Vector3(0.3f, 0.6f, 0f), new Vector3(0.3f, 0.6f, 0f),
                Radius, Height, out _, out _);

            Assert.IsFalse(hit);
        }

        [Test]
        public void 내리꽂은_밟기는_틱_끝_모습과_달리_세로_방향을_준다()
        {
            //  아래 몸은 가만히 있고, 위 몸이 한 틱에 1.8 m(초속 90)를 내려와 깊이 파고든다.
            //  가로로 0.3 어긋나 있어 정확히 포개진 예외 분기로 새지 않는다.
            Vector3 lowerFrom = Vector3.zero, lowerTo = Vector3.zero;
            Vector3 upperFrom = new Vector3(0.3f, 2.4f, 0f), upperTo = new Vector3(0.3f, 0.6f, 0f);

            //  틱이 끝난 모습만 보면 심 선분이 겹쳐(간격 0.6 < 1.0) 방향이 <b>순수 가로</b>다 —
            //  머리를 밟았는데 옆으로 밀려나던 원인이 이것이다.
            Assert.IsTrue(BodyOverlap.TryCompute(lowerTo, upperTo, Radius, Height,
                                                 out Vector3 endPushDir, out _));
            Assert.AreEqual(0f, endPushDir.y, 1e-6f);

            bool hit = BodySweep.TryContactNormal(lowerFrom, lowerTo, upperFrom, upperTo,
                                                  Radius, Height,
                                                  out Vector3 pushDir, out float timeOfImpact);

            Assert.IsTrue(hit);
            Assert.Less(pushDir.y, -0.9f, "아래 몸을 아래로 미는 방향이어야 위에서 밟힌 것이 된다");
            Assert.Greater(timeOfImpact, 0f);
            Assert.Less(timeOfImpact, 1f);
        }

        [Test]
        public void 옆에서_다가오면_가로_방향을_준다()
        {
            bool hit = BodySweep.TryContactNormal(
                new Vector3(-0.6f, 0f, 0f), new Vector3(-0.36f, 0f, 0f),
                new Vector3(0.6f, 0f, 0f), new Vector3(0.36f, 0f, 0f),
                Radius, Height, out Vector3 pushDir, out _);

            Assert.IsTrue(hit);
            Assert.AreEqual(0f, pushDir.y, 1e-4f, "옆 접촉이 세로로 오인되면 헛접지가 생긴다");
            Assert.AreEqual(-1f, pushDir.x, 1e-3f);
        }

        [Test]
        public void 시작부터_겹쳐_있으면_그_자리의_방향을_준다()
        {
            //  얹혀 있는 몸은 틱 시작부터 겹쳐 있다. 그때는 시작 모습이 곧 처음 닿은 순간이다.
            bool hit = BodySweep.TryContactNormal(
                Vector3.zero, Vector3.zero,
                new Vector3(0f, 1.7f, 0f), new Vector3(0f, 1.65f, 0f),
                Radius, Height, out Vector3 pushDir, out float timeOfImpact);

            Assert.IsTrue(hit);
            Assert.AreEqual(0f, timeOfImpact, 1e-6f);
            Assert.AreEqual(-1f, pushDir.y, 1e-3f);
        }

        [Test]
        public void 걸음_상한이_속도를_따라간다()
        {
            //  상한을 상수로 박아 두면 낙하 속도를 올렸을 때 조용히 못 찾기 시작한다. 한 틱에
            //  1.68 m(초속 84 상대)를 넘는, 지금 튜닝값보다 빠른 접근에서도 찾아내는지 본다.
            bool hit = BodySweep.TryContactNormal(
                Vector3.zero, Vector3.zero,
                new Vector3(0.3f, 12f, 0f), new Vector3(0.3f, 0.6f, 0f),
                Radius, Height, out Vector3 pushDir, out _);

            Assert.IsTrue(hit);
            Assert.Less(pushDir.y, -0.9f);
        }
    }
}
