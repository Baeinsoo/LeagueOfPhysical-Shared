using GameFramework.World;

namespace LOP.Tests
{
    /// <summary>
    /// 활쏘기 월드를 조립하는 공용 테스트 픽스처.
    ///
    /// <para><see cref="Still"/>은 <b>걷지 않는</b> 사수를 위한 것이다 — 이동 부품을 넘기긴 하지만
    /// 코스의 걷는 속도가 0이라 실제로는 한 줄도 안 돈다. 이동과 무관한 시험(화살·되감기·화살통)이
    /// 조립 코드를 베끼지 않게 여기 한 곳에 둔다.</para>
    /// </summary>
    internal static class ArcheryWorldFixture
    {
        public static ArcheryWorld Still(EntityRegistry registry, ArcheryAimSystem aimSystem,
                                         ArcheryCourse course, float tickInterval,
                                         WorldEventBuffer eventBuffer = null)
        {
            return new ArcheryWorld(registry, eventBuffer ?? new WorldEventBuffer(),
                                    aimSystem, course, tickInterval,
                                    new MovementSystem(new StatsSystem(), new MotionContributionSystem()),
                                    new KinematicMoveSystem(new FlappyWorldFixture.NeverHit(), 0),
                                    new FlappyWorldFixture.NoopMotionBridge());
        }
    }
}
