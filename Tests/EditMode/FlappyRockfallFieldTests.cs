using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class FlappyRockfallFieldTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown() { if (go != null) { Object.DestroyImmediate(go); } }

        private FlappyRockfall Make(float phase)
        {
            go = new GameObject("rockfall");
            var r = go.AddComponent<FlappyRockfall>();
            var rock = new GameObject("Rock");
            rock.transform.SetParent(go.transform, false);
            rock.transform.localPosition = new Vector3(0.3f, 5.6f, -0.2f);
            r.Rock = rock.transform;
            r.Period = 2f; r.Speed = 9f; r.Drop = 15.76f; r.Size = 1.2f; r.HiddenRise = 5.6f; r.Phase = phase;
            return r;
        }

        [Test]
        public void 떨어지는_중이면_바위_윗면이_천장에서_내려온_만큼_아래에()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            //  9 m 내려옴 → 윗면 −9, 가운데 0.6 − 9. (10-24 전엔 아랫면이 −9였다 — 첫 틱에 바위 한 개가
            //  통째로 천장 밑에 나타나던 식. 지금은 윗면 기준이라 천장에서 미끄러져 나온다.)
            field.PoseForTick(50, 0.02f);
            Assert.AreEqual(-8.4f, r.Rock.localPosition.y, 1e-3f);
        }

        [Test]
        public void 떨어지기_시작한_틱엔_바위가_천장_속에서_미끄러져_나온다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            //  0틱: 막 떨어지기 시작 — 아랫면이 천장선(0)에 있고 몸통은 다 천장 속이다.
            field.PoseForTick(0, 0.02f);
            Assert.AreEqual(0f, r.Rock.localPosition.y - r.Size * 0.5f, 1e-4f);
            //  1틱: 한 틱 낙하(0.18 m)만큼만 천장 밑으로 나와 있다.
            field.PoseForTick(1, 0.02f);
            Assert.AreEqual(-0.18f, r.Rock.localPosition.y - r.Size * 0.5f, 1e-4f);
        }

        [Test]
        public void 다_떨어지면_바위가_바닥_밑으로_다_빠져_있다()
        {
            //  통로 6 m(천장 0, 바닥 −6), Drop = 통로 + 바위 = 7.2, 한 틱 0.2 m(10 m/s).
            //  마지막으로 떨어지는 틱(35틱, 7.0 m)엔 윗면이 바닥선 위 한 틱 거리 안이고, 다음 틱(7.2 m = Drop)
            //  부터는 숨는다 — 윗면이 바닥선에 닿는 바로 그때 끝나므로 통로에 바위가 남은 채 사라지지 않는다.
            var field = new FlappyRockfallField();
            var r = Make(0f);
            r.Speed = 10f; r.Drop = 7.2f; r.Period = 2f;
            field.Add(r);
            const float floor = -6f;

            field.PoseForTick(35, 0.02f);
            float top = r.Rock.localPosition.y + r.Size * 0.5f;
            Assert.Greater(top, floor);
            Assert.LessOrEqual(top - floor, 0.2f + 1e-4f);

            field.PoseForTick(36, 0.02f);
            Assert.AreEqual(r.HiddenRise, r.Rock.localPosition.y, 1e-5f);
        }

        [Test]
        public void 숨는_동안은_천장_속에()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            field.PoseForTick(95, 0.02f);   // 1.9초 → 다 떨어짐
            Assert.AreEqual(5.6f, r.Rock.localPosition.y, 1e-5f);
        }

        [Test]
        public void x와_z는_건드리지_않는다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r);
            field.PoseForTick(50, 0.02f);
            Assert.AreEqual(0.3f, r.Rock.localPosition.x, 1e-5f);
            Assert.AreEqual(-0.2f, r.Rock.localPosition.z, 1e-5f);
        }

        [Test]
        public void 같은_틱을_다시_놓으면_같은_자리다()
        {
            var field = new FlappyRockfallField();
            var r = Make(90f);
            field.Add(r);
            field.PoseForTick(33, 0.02f);
            Vector3 first = r.Rock.localPosition;
            field.PoseForTick(90, 0.02f);
            field.PoseForTick(33, 0.02f);
            Assert.AreEqual(first, r.Rock.localPosition);
        }

        [Test]
        public void 같은_낙석을_두_번_넣어도_하나다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            field.Add(r); field.Add(r);
            Assert.AreEqual(1, field.Count);
        }

        [Test]
        public void 바위가_없으면_건너뛴다()
        {
            var field = new FlappyRockfallField();
            var r = Make(0f);
            r.Rock = null;
            field.Add(r);
            Assert.DoesNotThrow(() => field.PoseForTick(50, 0.02f));
        }
    }
}
