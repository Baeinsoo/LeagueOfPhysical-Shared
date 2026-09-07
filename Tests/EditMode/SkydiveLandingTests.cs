using NUnit.Framework;

public class SkydiveLandingTests
{
    private const float JumpPower = 11f;
    private const float Lethal = 15f;

    //  생성자 인자가 길어 읽기 어려우므로, 이 테스트가 실제로 쓰는 값만 의미 있게 채운다.
    //  나머지는 실제 표의 값을 그대로 옮겨 두어 "이상한 조합에서만 맞는 테스트"가 되지 않게 한다.
    private static LOP.SkydiveConfig Config(float lethal = Lethal, float jump = JumpPower)
    {
        return new LOP.SkydiveConfig(
            spreadFallSpeed: 60f, diveFallSpeed: 90f, glideFallSpeed: 6f,
            spreadMoveSpeed: 12f, diveMoveSpeed: 9f, glideMoveSpeed: 14f,
            spreadTurnAccel: 22f, diveTurnAccel: 6f, glideTurnAccel: 18f,
            fallApproach: 29f, postureRate: 4f,
            bodyRadius: 0.4f, bodyHeight: 1.8f, groundY: 0f,
            staminaMax: 300f, glideDrain: 20f, groundRecover: 40f, emergencyGlideTime: 1f,
            groundMoveSpeed: 4f, groundAccel: 100f, jumpPower: jump, poseClearance: 5f,
            fallBrake: 150f,
            glideWindLag: 0.2f, spreadWindLag: 2.06f, diveWindLag: 3.1f,
            landingLethalSpeed: lethal);
    }

    [Test]
    public void 활공_하강_속도로_닿으면_안_죽는다()
    {
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(6f, Config()));
    }

    /// <summary>
    /// 하드 제약이다 — 점프해서 착지하는 것이 죽으면 게임이 성립하지 않는다.
    /// 문턱과 JumpPower 중 어느 쪽을 움직여도 이 테스트가 그 사실을 알려 준다.
    /// </summary>
    [Test]
    public void 점프_착지는_절대_안_죽는다()
    {
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(JumpPower, Config()),
                       "점프 착지 속도가 문턱을 넘었다 — 문턱과 JumpPower를 같이 봐야 한다");
    }

    [Test]
    public void 대자와_다이브_낙하_속도로_닿으면_죽는다()
    {
        Assert.IsTrue(LOP.SkydiveLanding.IsLethal(60f, Config()));
        Assert.IsTrue(LOP.SkydiveLanding.IsLethal(90f, Config()));
    }

    /// <summary>문턱이 실제로 그 자리에 있는지. 0.1 차이로 갈려야 한다.</summary>
    [Test]
    public void 문턱_바로_아래위가_갈린다()
    {
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(Lethal - 0.1f, Config()));
        Assert.IsTrue(LOP.SkydiveLanding.IsLethal(Lethal + 0.1f, Config()));
    }

    /// <summary>위로 가는 속도(점프 직후)는 착지가 아니다.</summary>
    [Test]
    public void 위로_가는_속도는_치명이_아니다()
    {
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(-60f, Config()));
    }

    [Test]
    public void 컴포넌트가_없으면_치명이_아니다()
    {
        var entity = new GameFramework.World.Entity("diver");
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(entity, Config()));
    }

    [Test]
    public void 컴포넌트의_값으로_판정한다()
    {
        var entity = new GameFramework.World.Entity("diver");
        entity.Add(new LOP.LandingImpact { DownwardSpeed = 60f });
        Assert.IsTrue(LOP.SkydiveLanding.IsLethal(entity, Config()));

        entity.Get<LOP.LandingImpact>().DownwardSpeed = 6f;
        Assert.IsFalse(LOP.SkydiveLanding.IsLethal(entity, Config()));
    }
}
