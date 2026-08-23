using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 찍어 두는 플레이어 시작 지점. 게임 룰이 매치를 시작할 때 찾아 쓴다.
    ///
    /// 이 마커가 <b>공용 패키지</b>에 있는 이유: 맵 씬은 클라에서 만들고 서버가 읽는데, 스크립트가
    /// 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다.
    /// 양쪽이 같은 패키지를 참조하면 GUID가 같아 그 일이 생기지 않는다.
    /// (Unreal의 APlayerStart에 대응 — 시작 지점을 클래스로 두고 게임모드가 찾아 쓰는 방식.)
    /// </summary>
    public class SpawnPoint : MonoBehaviour
    {
        /// <summary>배정 순서. 작을수록 먼저 쓴다. 씬에서 찾아오는 순서는 보장되지 않아 이 값이 필요하다.</summary>
        public int Order;
    }
}
