using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 찍어 두는 결승선. 게임 룰이 이 지점의 x를 읽어 "전원이 넘었나"를 판정한다.
    ///
    /// <see cref="SpawnPoint"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 만들고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가
    /// 씬 주입을 끊는다. (Unreal의 ATriggerVolume 계열 골 마커에 대응 — 좌표를 코드가 아니라
    /// 맵이 정한다.)
    ///
    /// 어느 축을 읽을지는 마커가 아니라 <b>게임 룰이 정한다</b> — 마커는 좌표만 내줄 뿐이다.
    /// Flappy Race는 x(<see cref="FlappyRaceProgress"/>), Skydive는 y(<see cref="SkydiveProgress"/>)를 읽는다.
    /// </summary>
    public class FinishLine : MonoBehaviour
    {
    }
}
