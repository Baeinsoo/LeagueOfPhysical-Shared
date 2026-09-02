using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 스턴/무적 남은 시간을 "끝나는 절대 틱"으로 실어 보내는 변환이, 시뮬이 실제로 그 상태를 푸는
    /// 틱을 가리키는지 본다.
    ///
    /// 여기가 한 틱이라도 어긋나면 받는 쪽 새만 더 오래 얼어 있게 되고, 그 한 틱은 전진과 중력을
    /// 통째로 잃는다 — 라이브에서 "같은 틱인데 서버가 한 틱 앞서 있음"으로 관측된 그 현상이다.
    /// </summary>
    public class FlappyStunWireRoundTripTests
    {
        private const float DeltaTime = 0.02f;

        //  실제 마스터데이터 값(stun 0.8 / invuln 0.6). float로 0.02씩 빼면 정확히 0을 못 찍는
        //  쪽이라, 변환이 그 잔여를 한 틱으로 세면 스턴이 도는 내내 매 틱 틀린다.
        private static FlappyConfig Config()
            => new FlappyConfig(11f, 23f, 70f, 30f, 0.45f, 0.9f, 0.35f, stunTime: 0.8f, invulnTime: 0.6f);

        private static Entity Bird()
        {
            var bird = new Entity("bird");
            bird.Add(new FlappyStun());
            return bird;
        }

        [Test]
        public void 스턴_끝틱이_실제로_풀리는_틱을_가리킨다()
        {
            var config = Config();
            var system = new FlappyStunSystem(config);
            var bird = Bird();
            var stun = bird.Get<FlappyStun>();

            system.Enter(bird);
            int release = TicksToClearStun(config, config.StunTime);
            Assert.That(release, Is.GreaterThan(1), "이 검사가 성립하려면 스턴이 여러 틱이어야 한다");

            //  틱 라벨 1..release-1: release번째 Tick 호출에서 풀리므로 그 전까지는 계속 스턴 중이다.
            for (long now = 1; now < release; now++)
            {
                system.Tick(bird, DeltaTime);
                Assert.That(stun.StunRemaining, Is.GreaterThan(0f), $"틱 {now}에서 아직 스턴 중이어야 한다");

                Assert.That(FlappyTickDuration.EndTick(stun.StunRemaining, now, DeltaTime),
                    Is.EqualTo((long)release),
                    $"틱 {now}에서 보낸 끝틱이 실제로 풀리는 틱과 다르다. 남은시간={stun.StunRemaining:F9}");
            }
        }

        [Test]
        public void 받은_남은시간으로_굴려도_같은_틱에_풀린다()
        {
            var config = Config();
            var system = new FlappyStunSystem(config);
            var bird = Bird();
            var stun = bird.Get<FlappyStun>();

            system.Enter(bird);
            int release = TicksToClearStun(config, config.StunTime);

            for (long now = 1; now < release; now++)
            {
                system.Tick(bird, DeltaTime);

                long endTick = FlappyTickDuration.EndTick(stun.StunRemaining, now, DeltaTime);
                float decoded = FlappyTickDuration.RemainingSeconds(endTick, now, DeltaTime);

                Assert.That(TicksToClearStun(config, decoded), Is.EqualTo(release - (int)now),
                    $"틱 {now}에서 받은 남은시간({decoded:F9})으로 굴리면 푸는 틱이 다르다");
            }
        }

        [Test]
        public void 무적_끝틱이_실제로_풀리는_틱을_가리킨다()
        {
            var config = Config();
            var system = new FlappyStunSystem(config);
            var bird = Bird();
            var stun = bird.Get<FlappyStun>();

            //  스턴이 끝나는 틱에 무적이 채워진다 — 거기서부터 센다.
            system.Enter(bird);
            while (stun.StunRemaining > 0f)
            {
                system.Tick(bird, DeltaTime);
            }
            Assert.That(stun.InvulnRemaining, Is.GreaterThan(0f), "스턴이 끝나면 무적이 채워져야 한다");

            int release = TicksToClearInvuln(config, stun.InvulnRemaining);
            Assert.That(release, Is.GreaterThan(1));

            for (long now = 1; now < release; now++)
            {
                system.Tick(bird, DeltaTime);
                Assert.That(stun.InvulnRemaining, Is.GreaterThan(0f), $"틱 {now}에서 아직 무적이어야 한다");

                Assert.That(FlappyTickDuration.EndTick(stun.InvulnRemaining, now, DeltaTime),
                    Is.EqualTo((long)release),
                    $"틱 {now}에서 보낸 무적 끝틱이 실제와 다르다. 남은시간={stun.InvulnRemaining:F9}");
            }
        }

        [Test]
        public void 남은_시간이_없으면_끝틱은_0이다()
        {
            Assert.That(FlappyTickDuration.EndTick(0f, tick: 100, deltaTime: DeltaTime), Is.EqualTo(0L));
        }

        [Test]
        public void 이미_지났거나_같은_끝틱은_남은_시간이_0이다()
        {
            Assert.That(FlappyTickDuration.RemainingSeconds(90, tick: 100, deltaTime: DeltaTime), Is.EqualTo(0f));
            Assert.That(FlappyTickDuration.RemainingSeconds(100, tick: 100, deltaTime: DeltaTime), Is.EqualTo(0f));
            Assert.That(FlappyTickDuration.RemainingSeconds(0, tick: 100, deltaTime: DeltaTime), Is.EqualTo(0f));
        }

        [Test]
        public void 시뮬이_이미_0으로_본_잔여는_남은_틱이_없다()
        {
            //  매 틱 float를 빼면 정확히 0을 못 찍고 아주 조금 남는다. 시뮬은 그 조각을 끝으로
            //  보므로 변환도 그래야 한다 — 한 틱으로 세면 받는 쪽만 더 얼어 있게 된다.
            Assert.That(FlappyTickDuration.EndTick(1e-6f, tick: 100, deltaTime: DeltaTime), Is.EqualTo(0L));
        }

        [Test]
        public void 틱_경계를_아주_조금_넘은_잔여는_한_틱을_더_세지_않는다()
        {
            //  float로 빼 나가면 남은 시간이 틱 경계보다 아주 조금 크게 나올 수 있다(0.8이 아니라
            //  0.8000001). 그걸 그대로 올림하면 41틱이 되어 받는 쪽만 한 틱 더 얼어 있게 된다 —
            //  라이브에서 겪은 그 버그다. 세기 전에 Epsilon을 빼는 것이 그것을 막는다.
            Assert.That(FlappyTickDuration.EndTick(0.8f + 1e-7f, tick: 100, deltaTime: DeltaTime),
                Is.EqualTo(140L));
        }

        [Test]
        public void 틱_사이의_시간은_올려서_센다()
        {
            //  0.05초는 2.5틱 — 3틱을 세야 그 시간이 다 지난다. 내림하면 덜 얼어 있게 된다.
            Assert.That(FlappyTickDuration.EndTick(0.05f, tick: 100, deltaTime: DeltaTime), Is.EqualTo(103L));
        }

        [Test]
        public void 남은_틱만큼_초로_바뀐다()
        {
            Assert.That(FlappyTickDuration.RemainingSeconds(20, tick: 10, deltaTime: DeltaTime),
                Is.EqualTo(0.2f).Within(1e-4f));
        }

        //  주어진 남은 시간에서 출발해 몇 번째 Tick 호출에 스턴이 0이 되는지.
        private static int TicksToClearStun(FlappyConfig config, float remaining)
        {
            var system = new FlappyStunSystem(config);
            var bird = Bird();
            bird.Get<FlappyStun>().StunRemaining = remaining;
            for (int i = 1; i <= 1000; i++)
            {
                system.Tick(bird, DeltaTime);
                if (bird.Get<FlappyStun>().StunRemaining <= 0f)
                {
                    return i;
                }
            }
            return -1;
        }

        private static int TicksToClearInvuln(FlappyConfig config, float remaining)
        {
            var system = new FlappyStunSystem(config);
            var bird = Bird();
            bird.Get<FlappyStun>().InvulnRemaining = remaining;
            for (int i = 1; i <= 1000; i++)
            {
                system.Tick(bird, DeltaTime);
                if (bird.Get<FlappyStun>().InvulnRemaining <= 0f)
                {
                    return i;
                }
            }
            return -1;
        }
    }
}
