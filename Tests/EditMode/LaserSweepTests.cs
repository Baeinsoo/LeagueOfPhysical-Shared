using System.Numerics;
using LOP;
using NUnit.Framework;

public class LaserSweepTests
{
    private const float CapsuleRadius = 0.4f;
    private const float CapsuleHeight = 1.8f;

    // 원점을 지나 +X로 뻗은 고정 빔. 캐릭터는 X=0 근처를 세로로 지나간다.
    private static Laser FixedBeamAlongX()
        => new Laser(new Vector3(-20f, 1000f, 0f), length: 40f, radius: 0.3f,
                     startAngle: 0f, angularSpeed: 0f, sweepHalfRange: 0f,
                     period: 0, onTicks: 0, phase: 0);

    private static void Capsule(float y, out Vector3 bottom, out Vector3 top)
    {
        bottom = new Vector3(0f, y, 0f);
        top = new Vector3(0f, y + CapsuleHeight, 0f);
    }

    // 이 슬라이스의 존재 이유. 다이브 한 틱은 4.5m라 "틱 시작/끝만 보기"로는 얇은 빔을 그냥 지난다.
    [Test]
    public void 빠른_낙하가_얇은_빔을_통과하지_못한다()
    {
        Laser laser = FixedBeamAlongX();
        Capsule(1003f, out Vector3 bottomFrom, out Vector3 topFrom);
        Capsule(998.5f, out Vector3 bottomTo, out Vector3 topTo);

        bool hit = LaserSweep.Hit(laser, tick: 0, bottomFrom, topFrom, bottomTo, topTo,
                                  CapsuleRadius, out float toi);

        Assert.IsTrue(hit, "한 틱에 4.5m를 내려가며 y=1000의 빔을 지났는데 안 잡혔다");
        Assert.That(toi, Is.InRange(0f, 1f));
    }

    // 원 스펙의 서브스텝(캐릭터만 쪼개기)이 못 잡던 경우. 캐릭터는 멈춰 있고 빔이 훑고 지나간다.
    // 이 경우가 HitTolerance의 존재 이유이기도 하다 — 안전 전진 폭이 남은 거리에 비례해
    // 접촉 시각에 점점 가까워지기만 하므로, 허용 오차가 없으면 상한까지 돌다 놓친다.
    [Test]
    public void 정지한_캐릭터를_회전_빔이_훑으면_잡힌다()
    {
        // 캐릭터는 (10, 1000, 0)에 서 있고, 빔은 각도 -0.2에서 +0.2로 지나며 그 자리를 쓴다.
        var laser = new Laser(new Vector3(0f, 1000.5f, 0f), length: 20f, radius: 0.3f,
                              startAngle: -0.2f, angularSpeed: 0.4f, sweepHalfRange: 0f,
                              period: 0, onTicks: 0, phase: 0);
        var bottom = new Vector3(10f, 1000f, 0f);
        var top = new Vector3(10f, 1000f + CapsuleHeight, 0f);

        bool hit = LaserSweep.Hit(laser, tick: 0, bottom, top, bottom, top,
                                  CapsuleRadius, out _);

        Assert.IsTrue(hit, "빔이 한 틱 안에 캐릭터를 쓸고 지나갔는데 안 잡혔다");
    }

    [Test]
    public void 멀리_떨어져_있으면_안_맞는다()
    {
        Laser laser = FixedBeamAlongX();
        Capsule(1200f, out Vector3 bottomFrom, out Vector3 topFrom);
        Capsule(1195.5f, out Vector3 bottomTo, out Vector3 topTo);

        bool hit = LaserSweep.Hit(laser, tick: 0, bottomFrom, topFrom, bottomTo, topTo,
                                  CapsuleRadius, out _);

        Assert.IsFalse(hit);
    }

    [Test]
    public void 꺼진_레이저는_맞지_않는다()
    {
        var laser = new Laser(new Vector3(-20f, 1000f, 0f), 40f, 0.3f, 0f, 0f, 0f,
                              period: 10, onTicks: 4, phase: 0);
        Capsule(1003f, out Vector3 bottomFrom, out Vector3 topFrom);
        Capsule(998.5f, out Vector3 bottomTo, out Vector3 topTo);

        Assert.IsTrue(LaserSweep.Hit(laser, 0, bottomFrom, topFrom, bottomTo, topTo, CapsuleRadius, out _));
        Assert.IsFalse(LaserSweep.Hit(laser, 5, bottomFrom, topFrom, bottomTo, topTo, CapsuleRadius, out _));
    }

    // 캡슐 축은 세로, 빔은 가로라 3D에서 대개 어긋나 있다(2D에는 없는 경우). 2D 거리 공식을
    // 잘못 옮기면 여기서 걸린다.
    [Test]
    public void 어긋난_두_선분의_거리를_바르게_잰다()
    {
        var p1 = new Vector3(0f, 0f, 0f);
        var q1 = new Vector3(0f, 10f, 0f);      // 세로
        var p2 = new Vector3(-5f, 5f, 3f);
        var q2 = new Vector3(5f, 5f, 3f);       // 가로, z로 3만큼 어긋남

        float d = LaserSweep.SegmentDistance(p1, q1, p2, q2);

        Assert.AreEqual(3f, d, 1e-4f);
    }

    [Test]
    public void 끝점_밖에서는_끝점까지의_거리를_준다()
    {
        var p1 = new Vector3(0f, 0f, 0f);
        var q1 = new Vector3(0f, 1f, 0f);
        var p2 = new Vector3(0f, 5f, 0f);
        var q2 = new Vector3(0f, 9f, 0f);

        float d = LaserSweep.SegmentDistance(p1, q1, p2, q2);

        Assert.AreEqual(4f, d, 1e-4f);
    }

    [Test]
    public void 겹친_두_선분의_거리는_0이다()
    {
        var p1 = new Vector3(-1f, 0f, 0f);
        var q1 = new Vector3(1f, 0f, 0f);
        var p2 = new Vector3(0f, -1f, 0f);
        var q2 = new Vector3(0f, 1f, 0f);

        Assert.AreEqual(0f, LaserSweep.SegmentDistance(p1, q1, p2, q2), 1e-4f);
    }

    // 둘 다 멈춰 있고 닿지도 않으면 안전 전진 폭이 0이라 무한 반복이 될 수 있다.
    [Test]
    public void 아무것도_안_움직이면_바로_끝난다()
    {
        Laser laser = FixedBeamAlongX();
        Capsule(1050f, out Vector3 bottom, out Vector3 top);

        bool hit = LaserSweep.Hit(laser, 0, bottom, top, bottom, top, CapsuleRadius, out _);

        Assert.IsFalse(hit);
    }
}
