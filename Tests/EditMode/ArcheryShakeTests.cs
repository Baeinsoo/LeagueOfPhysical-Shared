using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryShakeTests
    {
        private static ArcheryConfig Config(float free = 1f, float ramp = 2f, float max = 3f)
        {
            return new ArcheryConfig(
                wavePeriodTicks: 88, minTargets: 2, maxTargets: 3,
                spawnRadius: 2f, spawnMinY: 2f, spawnMaxY: 6f, minSeparation: 1.2f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: free, shakeRampSeconds: ramp, shakeMaxDegrees: max,
                riseHeightMin: 1.2f, riseHeightMax: 2.4f, staggerTicks: 12, restTicks: 20,
                kinds: new[] { new ArcheryTargetKind(0.6f, 1, 50, false, ArcheryTargetShape.Sphere, null) });
        }

        //  짧게 당겼다 놓는 평소 사격이 흔들리면 안 된다 — 그러면 벌이 아니라 잡음이다.
        [Test]
        public void 유예_시간_안에는_전혀_안_흔들린다()
        {
            var config = Config(free: 1f);
            for (int i = 0; i < 20; i++)
            {
                float held = i * 0.05f;
                Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(held, 12345, config), $"held={held}");
            }

            //  경계 그 자체. 유예 시간과 정확히 같은 값이면 아직 안 흔들려야 한다.
            Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(config.ShakeFreeSeconds, 12345, config));
        }

        [Test]
        public void 오래_들고_있을수록_더_흔들린다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);

            float early = Amplitude(config, heldSeconds: 1.5f);
            float late = Amplitude(config, heldSeconds: 2.9f);

            Assert.Greater(late, early);
        }

        [Test]
        public void 아무리_오래_들고_있어도_정해진_폭을_안_넘는다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);

            for (float t = 1f; t <= 30f; t += 0.05f)
            {
                var offset = ArcheryShake.Offset(t, 777, config);
                Assert.LessOrEqual(Mathf.Abs(offset.x), 3f + 1e-3f, $"held={t}");
                Assert.LessOrEqual(Mathf.Abs(offset.y), 3f + 1e-3f, $"held={t}");
            }
        }

        //  같은 입력이면 언제 물어도 같은 답 — 클라와 서버가 같은 값을 봐야 한다.
        [Test]
        public void 같은_입력이면_언제_물어도_같다()
        {
            var config = Config();
            for (float t = 0f; t <= 5f; t += 0.17f)
            {
                Assert.AreEqual(ArcheryShake.Offset(t, 42, config), ArcheryShake.Offset(t, 42, config));
            }
        }

        [Test]
        public void 사람마다_다르게_흔들린다()
        {
            var config = Config();
            int a = ArcheryShake.PhaseSeedOf("entity-a");
            int b = ArcheryShake.PhaseSeedOf("entity-b");

            Assert.AreNotEqual(a, b);
            //  위상은 해시의 아래 16비트만 쓰므로 서로 다른 id가 같은 위상이 될 확률이 6만분의 1쯤
            //  있다. 이 두 id는 27484와 28789로 갈린다(GameFramework.Rng.Hashing.Fnv1a64로 직접
            //  계산해 확인). 실패하면 id를 바꾸지 말고 위상 비트 수를 늘릴 것 — 겹침이 실제로 난다는 신호다.
            Assert.AreNotEqual(ArcheryShake.Offset(2.5f, a, config), ArcheryShake.Offset(2.5f, b, config));
        }

        //  값이 뚝뚝 끊기면 손떨림이 아니라 화면 고장으로 보인다.
        [Test]
        public void 값이_매끄럽게_이어진다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);
            var previous = ArcheryShake.Offset(1f, 99, config);

            for (float t = 1.01f; t <= 6f; t += 0.01f)
            {
                var current = ArcheryShake.Offset(t, 99, config);
                //  끊김을 잡는 것이 목적이다. 값이 튀면(위상이 갑자기 바뀌면) 폭의 두 배인 6도쯤
                //  움직이므로, 매끄러울 때의 최대 변화(10ms에 약 0.3도)보다 넉넉한 0.5도를 문턱으로
                //  둔다 — 더 조이면 사인파의 정상 기울기에 걸려 거짓 실패가 난다.
                Assert.Less((current - previous).magnitude, 0.5f, $"held={t}");
                previous = current;
            }
        }

        private static float Amplitude(ArcheryConfig config, float heldSeconds)
        {
            //  한 시점의 값은 사인파의 위상 때문에 작을 수 있다 — 잠깐 동안의 최댓값으로 폭을 잰다.
            float peak = 0f;
            for (float t = heldSeconds; t < heldSeconds + 1f; t += 0.01f)
            {
                peak = Mathf.Max(peak, ArcheryShake.Offset(t, 12345, config).magnitude);
            }
            return peak;
        }
    }
}
