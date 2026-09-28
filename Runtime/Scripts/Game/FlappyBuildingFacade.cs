using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 빌딩 앞벽 표시. 클라 연출이 이걸 찾아, 내 새가 건물 안(<see cref="X0"/>~<see cref="X1"/>)에 있는 동안
    /// 자식 면을 반투명하게 한다. 로직이 없다 — 맵 씬을 서버도 읽으므로 공용 패키지에 둔다(한쪽에만 있으면
    /// missing script가 되어 씬 주입이 통째로 끊긴다 — 2026-09-28 홀로그램 사고).
    /// </summary>
    public class FlappyBuildingFacade : MonoBehaviour
    {
        public float X0;
        public float X1;
    }
}
