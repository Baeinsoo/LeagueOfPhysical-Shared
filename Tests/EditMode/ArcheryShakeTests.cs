using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class ArcheryShakeTests
    {
        private static ArcheryConfig Config(float free = 1f, float ramp = 2f, float max = 3f, float baseRatio = 0f)
        {
            return new ArcheryConfig(
                wavePeriodTicks: 88, minTargets: 2, maxTargets: 3,
                spawnRadius: 2f, spawnMinY: 2f, spawnMaxY: 6f, minSeparation: 1.2f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: free, shakeRampSeconds: ramp, shakeMaxDegrees: max,
                riseHeightMin: 1.2f, riseHeightMax: 2.4f, staggerTicks: 12, restTicks: 20,
                kinds: new[] { new ArcheryTargetKind(0.6f, 1, 50, false, ArcheryTargetShape.Sphere, null) },
                shakeBaseRatio: baseRatio);
        }

        //  이전 버전(fix 1회차)의 구멍: "일부 샘플이 0이 아니다"만 재서 1e-6도짜리 값도 통과시켰다
        //  — 배포 수치(free=0, ramp=2.5, max=0.25)로 계산하면 0.3초 당긴 화살은 12m에서 0.6cm
        //  밖에 안 흔들려, 사실상 "잡자마자부터 흔들린다"는 요구를 만족 못 했다(빠른 사격 =
        //  흔한 케이스가 여전히 사실상 정지). 원인은 기준선(baseline)이 없어 피로가 0에서부터
        //  자랐기 때문 — 이 시험은 그 구멍을 "몇 %는 실려야 하나"로 못박는다.
        [Test]
        public void 잡고_바로_기준선만큼의_최소_진폭으로_흔들린다()
        {
            //  배포와 같은 비율(base=0.4)로 짠 설정. free=0(유예 없음)이라 fatigue는 이 구간
            //  (0~0.3초, ramp=2.5초)에서 사실상 0에 가깝다 — 그런데도 최소 진폭이 나와야 한다.
            var config = Config(free: 0f, ramp: 2.5f, max: 3f, baseRatio: 0.4f);

            //  fatigue가 사실상 0인 구간에서도 최소 30%(=baseRatio 근방)는 실려야 한다.
            for (float t = 0f; t < 0.3f; t += 0.05f)
            {
                float amplitude = ArcheryShake.AmplitudeDegrees(t, drawRatio: 1f, config);
                Assert.GreaterOrEqual(amplitude, 0.3f * config.ShakeMaxDegrees,
                    $"잡은 지 {t}초(사실상 즉시)인데 진폭이 바닥에도 못 미친다 — amplitude={amplitude}");
            }
        }

        //  진폭이 크다고 적어 놓고 파형이 그 폭을 안 쓰면 소용없다 — 한 주기 안에 두 축 모두
        //  진폭 전부를 써야 한다. (위 시험은 크기만, 이 시험은 그 크기가 실제로 나오는지를 본다.)
        [Test]
        public void 파형이_한_주기_안에_진폭_전부를_쓴다()
        {
            var config = Config(free: 0f, ramp: 0f, max: 3f, baseRatio: 1f);   // 피로 무관하게 항상 최대
            float amplitude = ArcheryShake.AmplitudeDegrees(1f, 1f, config);

            float peakX = 0f, peakY = 0f;
            for (float t = 0f; t < 10f; t += 0.002f)
            {
                var offset = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 12345, config);
                peakX = Mathf.Max(peakX, Mathf.Abs(offset.x));
                peakY = Mathf.Max(peakY, Mathf.Abs(offset.y));
            }

            Assert.AreEqual(amplitude, peakX, 1e-2f, "좌우가 진폭을 다 안 쓴다");
            Assert.AreEqual(amplitude, peakY, 1e-2f, "위아래가 진폭을 다 안 쓴다");
        }

        // ── 업계 표준 플로트의 모양 ──────────────────────────────────────────────
        //
        //  ⭐ 아래 두 시험이 **이 파일의 존재 이유**다. 이전 버전(0.7+1.7+8.7Hz 합)은 크기는
        //  멀쩡했는데 실물에서 "보정이 사실상 불가능"했다. 크기를 재는 시험만 있었고 **속도를
        //  재는 시험이 없었기** 때문이다.

        [Test]
        public void 조준점이_8자를_그린다()
        {
            var config = Config(free: 0f, ramp: 0f, max: 3f, baseRatio: 1f);

            float periodX = PeriodOf(t => ArcheryShake.Offset(t, 1f, 12345, config).x);
            float periodY = PeriodOf(t => ArcheryShake.Offset(t, 1f, 12345, config).y);

            //  가로가 세로의 정확히 두 배 속도(=주기는 절반)라야 누운 8자가 된다. 1:1이면 원,
            //  안 맞아떨어지는 비면 불규칙해서 못 배운다.
            Assert.AreEqual(2f, periodY / periodX, 0.05f,
                $"가로:세로 주기비가 2:1이 아니다 — x={periodX}s, y={periodY}s");
        }

        [Test]
        public void 사람이_눈으로_보고_따라갈_수_있을_만큼_느리다()
        {
            var config = Config(free: 0f, ramp: 0f, max: 3f, baseRatio: 1f);

            float periodX = PeriodOf(t => ArcheryShake.Offset(t, 1f, 12345, config).x);
            float periodY = PeriodOf(t => ArcheryShake.Offset(t, 1f, 12345, config).y);

            //  사람이 보고 반응하는 데 약 0.2초가 걸린다. 주기가 그것의 몇 배는 돼야 "보고
            //  되돌리기"가 성립한다 — 닫힌 루프 추적의 한계가 대략 0.5Hz(주기 2초)다.
            //  이전 버전의 1.7Hz(주기 0.59초)는 반응했을 때 이미 반대로 가 있었다.
            Assert.GreaterOrEqual(Mathf.Min(periodX, periodY), 1.5f,
                $"빠른 쪽 축의 주기가 1.5초보다 짧다 — 사람이 못 따라간다. x={periodX}s, y={periodY}s");
        }

        //  기준선이 있어도 당기지 않으면(drawRatio=0) 여전히 완전히 0이어야 한다 — 기준선은
        //  "쥐고 있을 때의 바닥"이지, 안 당겨도 흔들리는 잡음이 아니다.
        [Test]
        public void 기준선이_있어도_안_당기면_0이다()
        {
            var config = Config(free: 0f, ramp: 2.5f, max: 3f, baseRatio: 0.4f);
            for (float t = 0f; t <= 3f; t += 0.3f)
            {
                Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(t, drawRatio: 0f, phaseSeed: 12345, config), $"held={t}");
            }
        }

        //  기준선=0이면(이 슬라이스 이전과 같은 모양) 잡은 바로 그 순간(0초)은 여전히 정확히
        //  0이다 — 이건 "유예 구간"이 아니라 fatigue(0)=0에서 자연히 나오는 값이다. 기준선을
        //  0이 아닌 값으로 켜는 순간 이 성질은 깨진다(그것이 의도다 — 아래 대비되는 시험 참고).
        [Test]
        public void 기준선이_0이면_잡은_바로_그_순간0초에는_아직_0이다()
        {
            var config = Config(free: 0f, ramp: 2f, max: 3f, baseRatio: 0f);
            Assert.AreEqual(Vector2.zero, ArcheryShake.Offset(0f, drawRatio: 1f, phaseSeed: 12345, config));
        }

        //  기준선이 피로를 "대체"하는 게 아니라 그 위에 "더하는" 것인지 확인한다 — 대체라면
        //  오래 잡아도 baseRatio에서 멈추고 max까지 못 갈 수 있다. fatigue가 다 찬(=1)
        //  시점에서는 factor = base + (1-base)*1 = 1이 되어 baseRatio 값과 무관하게 기준선이
        //  없을 때(baseRatio=0)와 정확히 같은 결과가 나와야 한다 — 다르다면 기준선이 그 위에
        //  더해지는 게 아니라 최종값을 눌러 깎고 있다는 뜻이다.
        [Test]
        public void 기준선은_피로를_대체하지_않고_더한다_오래_잡으면_기준선_유무가_결과에_영향을_안_준다()
        {
            var configWithBase = Config(free: 0f, ramp: 2f, max: 3f, baseRatio: 0.4f);
            var configNoBase = Config(free: 0f, ramp: 2f, max: 3f, baseRatio: 0f);

            //  ramp(2초)를 한참 넘겨 fatigue가 1로 다 찬 구간.
            for (float t = 5f; t <= 6f; t += 0.1f)
            {
                var withBase = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 12345, configWithBase);
                var noBase = ArcheryShake.Offset(t, drawRatio: 1f, phaseSeed: 12345, configNoBase);
                Assert.Less((withBase - noBase).magnitude, 1e-4f,
                    $"held={t}에서 fatigue가 다 찼는데도 기준선 유무로 값이 달라진다 — " +
                    "기준선이 피로 위에 '더하기'가 아니라 결과를 '대체'하고 있다는 뜻이다");
            }
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

        //  전에는 1초를 훑어 최댓값을 폭으로 삼았다. 플로트가 느려지면서(주기 4초) 그 방식이
        //  무너졌다 — 1초 창에 봉우리가 안 들어와 "피로가 늘었는데 잰 값은 줄어드는" 일이 난다.
        //  지금은 크기를 파형과 분리해 두었으니 직접 묻는다(위상과 무관).
        private static float Amplitude(ArcheryConfig config, float heldSeconds, float drawRatio)
        {
            return ArcheryShake.AmplitudeDegrees(heldSeconds, drawRatio, config);
        }

        //  한 축이 0을 **같은 방향으로** 지나는 두 시각의 간격 = 그 축의 주기(초).
        //  주파수 상수를 시험이 알 필요가 없게 파형에서 직접 잰다.
        private static float PeriodOf(System.Func<float, float> axis)
        {
            const float step = 0.002f;
            float firstUp = -1f;
            float previous = axis(0f);

            for (float t = step; t < 30f; t += step)
            {
                float current = axis(t);
                if (previous <= 0f && current > 0f)
                {
                    if (firstUp < 0f)
                    {
                        firstUp = t;
                    }
                    else
                    {
                        return t - firstUp;
                    }
                }
                previous = current;
            }

            Assert.Fail("30초 안에 주기를 못 쟀다 — 파형이 0을 두 번 안 지난다");
            return 0f;
        }
    }
}
