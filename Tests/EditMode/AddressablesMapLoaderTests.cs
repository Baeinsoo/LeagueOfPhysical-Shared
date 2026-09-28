using System;
using LOP;
using NUnit.Framework;

public class AddressablesMapLoaderTests
{
    // 맵이 안 뜨면 바닥이 없는 채 판이 돌아 캐릭터가 계속 떨어지고, 서버가 되돌리길 반복한다(2026-09-28).
    // 실패는 조용히 넘기지 말고 판 시작을 막아야 한다.
    [Test]
    public void 맵을_못_불러오면_어느_맵인지_적어_던진다()
    {
        var e = Assert.Throws<InvalidOperationException>(
            () => AddressablesMapLoader.ThrowIfFailed("Assets/Art/Scenes/DodgeMap.unity", false, "No Location found"));
        StringAssert.Contains("Assets/Art/Scenes/DodgeMap.unity", e.Message);
        StringAssert.Contains("No Location found", e.Message);
    }

    [Test]
    public void 불러왔으면_아무_일도_없다()
    {
        Assert.DoesNotThrow(() => AddressablesMapLoader.ThrowIfFailed("Assets/Art/Scenes/DodgeMap.unity", true, null));
    }
}
