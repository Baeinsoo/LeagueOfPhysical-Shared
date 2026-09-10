using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ContactImpulseTests
    {
        const float Tolerance = 1e-4f;
        const float E = 0.35f;

        [Test]
        public void 정면으로_다가오면_법선_방향_속도를_주고받는다()
        {
            //  A가 위에서 -10으로 내려와 정지한 B를 때린다. 법선은 A를 위로 민다.
            Vector3 vA = new Vector3(0f, -10f, 0f);
            Vector3 vB = Vector3.zero;
            Vector3 n = Vector3.up;

            Vector3 afterA = ContactImpulse.Resolve(vA, vB, n, E);
            Vector3 afterB = ContactImpulse.Resolve(vB, vA, -n, E);

            //  다가오는 속도 -10에 (1+e)/2 = 0.675를 곱해 6.75만큼 주고받는다.
            Assert.AreEqual(-3.25f, afterA.y, Tolerance);
            Assert.AreEqual(-6.75f, afterB.y, Tolerance);
        }

        [Test]
        public void 가로_법선이면_가로_속도가_바뀐다()
        {
            //  세로만 다루던 옛 커널이 못 하던 일 — 이 테스트가 이번 변경의 이유다.
            Vector3 vA = new Vector3(8f, 0f, 0f);
            Vector3 vB = Vector3.zero;
            Vector3 n = Vector3.left;   // A를 -x로 밀어낸다

            Vector3 afterA = ContactImpulse.Resolve(vA, vB, n, E);

            Assert.AreEqual(8f - 0.675f * 8f, afterA.x, Tolerance);
            Assert.AreEqual(0f, afterA.y, Tolerance);
        }

        [Test]
        public void 이미_멀어지는_중이면_건드리지_않는다()
        {
            Vector3 vA = new Vector3(0f, 5f, 0f);
            Vector3 after = ContactImpulse.Resolve(vA, Vector3.zero, Vector3.up, E);
            Assert.AreEqual(vA, after);
        }

        [Test]
        public void 아주_천천히_닿으면_안_튕긴다()
        {
            //  RestingSpeed 아래는 e를 0으로 본다 — 얹혀 있을 때 떠는 것을 막는다.
            float slow = ContactImpulse.RestingSpeed * 0.5f;
            Vector3 after = ContactImpulse.Resolve(new Vector3(0f, -slow, 0f), Vector3.zero, Vector3.up, E);

            //  e=0이므로 다가오던 속도의 절반만 지워진다.
            Assert.AreEqual(-slow * 0.5f, after.y, Tolerance);
        }

        [Test]
        public void 스치면_거의_안_바뀐다()
        {
            Vector3 v = new Vector3(0f, -10f, 0f);
            Vector3 straight = ContactImpulse.Resolve(v, Vector3.zero, Vector3.up, E);
            Vector3 glancing = ContactImpulse.Resolve(
                v, Vector3.zero, new Vector3(0.866f, 0.5f, 0f), E);   // 법선이 60도 기운다

            Assert.Less(Mathf.Abs(glancing.y - v.y), Mathf.Abs(straight.y - v.y));
        }

        [Test]
        public void 반발계수_1이면_등질량_정면에서_속도가_통째로_교환된다()
        {
            Vector3 vA = new Vector3(0f, -10f, 0f);
            Vector3 afterA = ContactImpulse.Resolve(vA, Vector3.zero, Vector3.up, 1f);
            Vector3 afterB = ContactImpulse.Resolve(Vector3.zero, vA, Vector3.down, 1f);

            Assert.AreEqual(0f, afterA.y, Tolerance);
            Assert.AreEqual(-10f, afterB.y, Tolerance);
        }
    }
}
