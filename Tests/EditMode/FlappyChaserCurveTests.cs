using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 추격자 벽의 위치 규칙. 이 곡선 하나가 "실수를 몇 번까지 봐주는가"를 정하므로,
    /// 상한에 닿는 시각과 그 뒤 등속이 이 게임의 난이도 그 자체다.
    /// </summary>
    public class FlappyChaserCurveTests
    {
        private const float Tolerance = 1e-3f;

        //  결승선이 사실상 없다는 뜻 — 상한을 보지 않는 테스트들이 쓴다.
        private const float StopFar = float.MaxValue;

        //  추격자와 전진속도만 실제 값이고 나머지는 이 테스트에 무의미한 자리채움이다.
        private static FlappyConfig Config()
            => new FlappyConfig(forwardSpeed: 11f, flapImpulse: 23f, gravity: 70f, maxFallSpeed: 30f,
                                bodyRadius: 0.45f, bodyHeight: 0.9f, restitution: 0.35f,
                                stunTime: 0.8f, invulnTime: 0.6f,
                                dashMult: 2f, dashDuration: 0.2f, dashChargeBase: 0.13f, dashChargeDive: 1.2f,
                                chaserStartX: -60f, chaserInitialSpeed: 7f,
                                chaserAcceleration: 0.075f, chaserMaxSpeed: 10f);

        [Test]
        public void 출발_전에는_시작점에_서_있다()
        {
            Assert.That(FlappyChaserCurve.XAt(Config(), -1f, StopFar), Is.EqualTo(-60f).Within(Tolerance));
            Assert.That(FlappyChaserCurve.XAt(Config(), 0f, StopFar), Is.EqualTo(-60f).Within(Tolerance));
        }

        [Test]
        public void 상한에_닿는_시각은_속도차를_가속도로_나눈_값이다()
        {
            //  이 시각이 곧 압박 전환점이다 — 여기부터 실수 여유가 더 늘지 않는다.
            Assert.That(FlappyChaserCurve.RampSeconds(Config()), Is.EqualTo(40f).Within(Tolerance));
        }

        [Test]
        public void 가속하는_동안은_반가속도_제곱만큼_더_간다()
        {
            //  -60 + 7×40 + ½×0.075×40² = 280
            Assert.That(FlappyChaserCurve.XAt(Config(), 40f, StopFar), Is.EqualTo(280f).Within(Tolerance));
        }

        [Test]
        public void 상한_뒤로는_등속이다()
        {
            //  280 + 10×20 = 480
            Assert.That(FlappyChaserCurve.XAt(Config(), 60f, StopFar), Is.EqualTo(480f).Within(Tolerance));
        }

        [Test]
        public void 앞서_몇_번을_물었든_같은_시각이면_같은_답이다()
        {
            //  누적하지 않는다는 것이 이 곡선의 전부다. 누적하면 프레임 수에 따라 답이 갈리고
            //  되돌리기로 과거 틱을 물을 수도 없다.
            var config = Config();
            float once = FlappyChaserCurve.XAt(config, 75.2f, StopFar);

            for (float t = 0f; t < 75.2f; t += 0.02f)
            {
                FlappyChaserCurve.XAt(config, t, StopFar);
            }

            Assert.That(FlappyChaserCurve.XAt(config, 75.2f, StopFar), Is.EqualTo(once).Within(0f));
        }

        [Test]
        public void 결승선을_지나서는_안_간다()
        {
            //  벽이 결승선을 지나가면, 그 뒤에 멈춰 선 완주자를 통과하는 그림이 나온다.
            //  완주자는 추격자 판정에서 빠져 안 죽는데 화면에서는 먹히는 것처럼 보인다.
            Assert.That(FlappyChaserCurve.XAt(Config(), 120f, stopAtX: 632f),
                Is.EqualTo(632f).Within(Tolerance));
        }

        [Test]
        public void 결승선_전에는_상한이_영향을_주지_않는다()
        {
            Assert.That(FlappyChaserCurve.XAt(Config(), 40f, stopAtX: 632f),
                Is.EqualTo(280f).Within(Tolerance));
        }

        [Test]
        public void 한_번도_안_박은_새는_영영_안_잡힌다()
        {
            //  상한이 전진속도보다 낮다는 것의 의미가 이것이다. 이 성질이 깨지면
            //  "완주가 기본"이라는 원칙 자체가 무너진다.
            var config = Config();

            for (float t = 0f; t <= 120f; t += 0.02f)
            {
                float birdX = -3f + config.ForwardSpeed * t;
                Assert.That(birdX - config.BodyRadius,
                    Is.GreaterThan(FlappyChaserCurve.XAt(config, t, StopFar)), $"t={t}");
            }
        }
    }
}
