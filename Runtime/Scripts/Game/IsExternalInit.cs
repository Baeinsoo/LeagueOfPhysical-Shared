// C# 9 record 지원을 위한 폴리필. Unity 6의 .NET Standard 2.1 BCL은 IsExternalInit를 제공하지 않는다
// (이 타입은 .NET 5+에서 추가됨).
// 이 어셈블리(baegames.LOP.Shared.Runtime)에서만 internal 노출, 다른 어셈블리에 영향 없음.
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
