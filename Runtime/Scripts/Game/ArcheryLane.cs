using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 찍어 두는 <b>레인</b> — 사수 한 명이 서는 자리와 그 앞에 과녁이 설 자리들.
    /// 게임 룰이 매치를 시작할 때 찾아 쓴다. <see cref="SpawnPoint"/>와 같은 성격의 표식이다.
    ///
    /// <para>이 마커가 <b>공용 패키지</b>에 있는 이유도 같다: 맵 씬은 클라에서 만들고 서버가 읽는데,
    /// 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para>레인이 <b>보는 쪽</b>(transform.forward)이 사대에서 과녁 쪽이다. 과녁은 그 반대를
    /// 바라보고 선다 — 사수 쪽에서 온 화살만 맞는다.</para>
    /// </summary>
    public class ArcheryLane : MonoBehaviour
    {
        /// <summary>배정 순서. 작을수록 먼저 쓴다. 씬에서 찾아오는 순서는 보장되지 않아 이 값이 필요하다.</summary>
        public int Order;

        /// <summary>
        /// 과녁이 설 자리들. <b>가까운 것부터 먼 순서로</b> 넣는다 — 이 차례가 마스터데이터의
        /// <c>stand_index</c>와 짝이다. (양궁에서는 이 받침을 butt 또는 bale이라 부른다.)
        /// </summary>
        public Transform[] Stands;
    }
}
