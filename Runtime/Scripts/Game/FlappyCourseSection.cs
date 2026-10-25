using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 코스 구간 표식(spec §5 흐름). 광산 굽기가 구간마다 하나씩 ComposedMap 자식으로 굽는다 — 놓인 x가 구간 시작이다.
    /// 클라 정보 HUD가 맵에서 읽어 구간 배너·LAST STRETCH!·진행 막대 띠를 그린다. 판정과 무관하다.
    ///
    /// <para><see cref="FlappyMapRules"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 굽고 서버도 같은 씬을 읽는다.
    /// 스크립트가 서버에 없으면 그 자리가 missing script가 되고, 씬 주입 스캔이 그 빈 칸에서 끊긴다.</para>
    ///
    /// <para>씬 주입 대상이 <b>아니다</b>(<c>[SceneInjectMonoBehaviour]</c> 없음). 서버는 이 값을 쓰지 않으므로 서버 스코프에
    /// 등록할 것이 없다 — 클라가 맵이 올라온 뒤 한 번 찾아 읽는다(클라 <c>CourseSections.FindInScene</c>).</para>
    /// </summary>
    [DisallowMultipleComponent]
    public class FlappyCourseSection : MonoBehaviour
    {
        /// <summary>배너에 쓰는 구간 이름. 화면 글꼴에 있는 글자만 — 굽기가 기호(⬆·⬇)를 글로 바꿔 넣는다.</summary>
        public string Title = string.Empty;

        /// <summary>몇 막인가(1부터). 배너의 "n막"과 진행 막대 띠 색.</summary>
        public int Act = 1;

        /// <summary>마지막 구간이면 구간 배너 대신 LAST STRETCH!와 음악 올리기.</summary>
        public bool Final;

        /// <summary>구간 시작 x(월드). 놓인 자리다.</summary>
        public float X => transform.position.x;
    }
}
