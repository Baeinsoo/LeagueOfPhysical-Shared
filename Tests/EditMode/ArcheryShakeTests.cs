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

        //  배포 값(shake_free_seconds=0)에서는 잡은 순간부터 자란다 — "실제 손처럼" 완전히
        //  가만있는 구간이 없어야 한다는 것이 이번 슬라이스의 핵심 요구다.
        [Test]
        public void free가_0이면_잡자마자부터_흔들리기_시작한다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f);

            bool sawNonZero = false;
            for (int i = 1; i <= 20; i++)
            {
                float held = i * 0.01f;   // 10ms~200ms — 막 잡은 직후 구간.
                if (ArcheryShake.Offset(held, drawRatio: 1f, phaseSeed: 12345, config).sqrMagnitude > 0f)
                {
                    sawNonZero = true;
                    break;
                }
            }
            Assert.IsTrue(sawNonZero,
                "free=0이면 별도 유예 구간 없이 잡은 직후부터 흔들림이 자라기 시작해야 한다");
        }

        //  0초에는 아직 하나도 못 잡고 있었다는 뜻이라(당기기 시작한 바로 그 순간) 흔들림도
        //  정확히 0이어야 한다 — 이건 "유예 구간"이 아니라 grow(0)=0에서 자연히 나오는 값이다.
        [Test]
        public void 잡은_바로_그_순간0초에는_아직_0이다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f);
            Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(0f, drawRatio: 1f, phaseSeed: 12345, config));
        }

        //  free는 "피로가 쌓이기 시작하는 시점"이라는 뜻 자체는 여전히 유효하다 — 배포 값이
        //  0으로 바뀌었을 뿐, 0이 아닌 값을 넣으면 그 구간 동안은 여전히 안 흔들려야 한다.
        [Test]
        public void free를_0이_아닌_값으로_주면_그_구간_안에서는_안_흔들린다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);
            for (int i = 0; i < 20; i++)
            {
                float held = i * 0.05f;
                Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(held, drawRatio: 1f, phaseSeed: 12345, config), $"held={held}");
            }

            //  경계 그 자체. 유예 시간과 정확히 같은 값이면 아직 안 흔들려야 한다.
            Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(config.ShakeFreeSeconds, drawRatio: 1f, phaseSeed: 12345, config));
        }

        [Test]
        public void 오래_들고_있을수록_더_흔들린다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);

            float early = Amplitude(config, heldSeconds: 1.5f, drawRatio: 1f);
            float late = Amplitude(config, heldSeconds: 2.9f, drawRatio: 1f);

            Assert.Greater(late, early);
        }

        //  덜 당긴 활은 덜 떤다 — 절반만 당긴 활이 절반만 당긴 시위의 부하를 진다.
        [Test]
        public void 많이_당길수록_더_흔들린다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f);

            float half = Amplitude(config, heldSeconds: 1f, drawRatio: 0.5f);
            float full = Amplitude(config, heldSeconds: 1f, drawRatio: 1f);

            Assert.Greater(full, half);
            //  진폭 공식이 drawRatio에 선형이라(같은 시각·같은 위상이면 파형 값 자체는 같고
            //  진폭만 배율로 곱해진다) 절반은 정확히 절반이어야 한다.
            Assert.AreEqual(0.5f * full, half, 1e-4f);
        }

        [Test]
        public void 아무리_오래_들고_있어도_정해진_폭을_안_넘는다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);

            for (float t = 1f; t <= 30f; t += 0.05f)
            {
                var offset = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 777, config);
                Assert.LessOrEqual(Mathf.Abs(offset.x), 3f + 1e-3f, $"held={t}");
                Assert.LessOrEqual(Mathf.Abs(offset.y), 3f + 1e-3f, $"held={t}");
            }
        }

        //  drawRatio를 1보다 세게 먹여도(방어적으로) 안 넘어야 한다 — Clamp01이 지키는 것.
        [Test]
        public void drawRatio가_1을_넘어도_폭을_안_넘는다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f);

            for (float t = 0.5f; t <= 10f; t += 0.5f)
            {
                var offset = ArcheryShake.Offset(t, drawRatio: 5f, phaseSeed: 321, config);
                Assert.LessOrEqual(Mathf.Abs(offset.x), 3f + 1e-3f, $"held={t}");
                Assert.LessOrEqual(Mathf.Abs(offset.y), 3f + 1e-3f, $"held={t}");
            }
        }

        //  이 시험이 재는 건 "순수함수인가"다 — 내부 가변 상태나 시간 의존 없이 같은 입력에
        //  매번 같은 값만 나오면, 클라 프로세스와 서버 프로세스가 각자 이 함수를 불러도 같은
        //  답이 나온다는 보장이 선다(두 프로세스를 실제로 띄워 비교하진 않는다 — 순수함수라는
        //  성질 자체가 그 보장의 근거다. ArcheryShake.PhaseSeedOf의 주석 참고).
        [Test]
        public void 순수함수라_같은_입력엔_항상_같은_값이_나온다()
        {
            var config = Config();
            for (float t = 0f; t <= 5f; t += 0.17f)
            {
                Assert.AreEqual(
                    ArcheryShake.Offset(t, drawRatio: 0.7f, phaseSeed: 42, config),
                    ArcheryShake.Offset(t, drawRatio: 0.7f, phaseSeed: 42, config));
            }
        }

        [Test]
        public void 사람마다_다르게_흔들린다()
        {
            var config = Config();
            int a = ArcheryShake.PhaseSeedOf("entity-a");
            int b = ArcheryShake.PhaseSeedOf("entity-b");

            //  값 자체를 못박는다 — "다르기만 하면 통과"로는 Fnv1a64를 string.GetHashCode()로
            //  바꿔치기해도(클·서 합의가 깨지는 바로 그 사고, ArcheryShake.PhaseSeedOf의 주석
            //  참고) 여전히 초록일 수 있다. 이 리터럴은 GameFramework.Rng.Hashing.Fnv1a64로
            //  "entity-a"/"entity-b"를 직접 돌려 확인한 값이다 — 해시 함수가 바뀌면 이 시험부터
            //  빨개져야 한다.
            Assert.AreEqual(1243376476, a);
            Assert.AreEqual(1243377781, b);

            //  위상은 해시의 아래 16비트만 쓰므로 서로 다른 id가 같은 위상이 될 확률이 6만분의 1쯤
            //  있다. 이 두 id는 27484와 28789로 갈린다. 실패하면 id를 바꾸지 말고 위상 비트 수를
            //  늘릴 것 — 겹침이 실제로 난다는 신호다.
            Assert.AreNotEqual(
                ArcheryShake.Offset(2.5f, drawRatio: 1f, phaseSeed: a, config),
                ArcheryShake.Offset(2.5f, drawRatio: 1f, phaseSeed: b, config));
        }

        //  값이 뚝뚝 끊기면 손떨림이 아니라 화면 고장으로 보인다.
        [Test]
        public void 값이_매끄럽게_이어진다()
        {
            var config = Config(free: 1f, ramp: 2f, max: 3f);
            var previous = ArcheryShake.Offset(1f, drawRatio: 1f, phaseSeed: 99, config);

            for (float t = 1.01f; t <= 6f; t += 0.01f)
            {
                var current = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 99, config);
                //  끊김을 잡는 것이 목적이다. 값이 튀면(위상이 갑자기 바뀌면) 폭의 두 배인 6도쯤
                //  움직이므로, 매끄러울 때의 최대 변화(10ms에 약 0.3도)보다 넉넉한 0.5도를 문턱으로
                //  둔다 — 더 조이면 사인파의 정상 기울기에 걸려 거짓 실패가 난다.
                Assert.Less((current - previous).magnitude, 0.5f, $"held={t}");
                previous = current;
            }
        }

        //  free=0으로 바뀐 지점(잡은 순간)을 가로질러도 값이 안 튀어야 한다 — 예전엔 이 지점이
        //  "완전한 정지 → 흔들림 시작"의 경계였는데, 지금은 grow(0)=0이라 그 자체가 매끈한
        //  0이어야 한다(불연속이 없어야 한다는 요구사항의 직접적인 시험).
        [Test]
        public void free가_0일_때_시작_지점을_가로질러도_안_튄다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f);
            var previous = ArcheryShake.Offset(0f, drawRatio: 1f, phaseSeed: 55, config);

            for (float t = 0f; t <= 1f; t += 0.01f)
            {
                var current = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 55, config);
                Assert.Less((current - previous).magnitude, 0.5f, $"held={t}");
                previous = current;
            }
        }

        private static float Amplitude(ArcheryConfig config, float heldSeconds, float drawRatio)
        {
            //  한 시점의 값은 사인파의 위상 때문에 작을 수 있다 — 잠깐 동안의 최댓값으로 폭을 잰다.
            float peak = 0f;
            for (float t = heldSeconds; t < heldSeconds + 1f; t += 0.01f)
            {
                peak = Mathf.Max(peak, ArcheryShake.Offset(t, drawRatio, 12345, config).magnitude);
            }
            return peak;
        }
    }
}
