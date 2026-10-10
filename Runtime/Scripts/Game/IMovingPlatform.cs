using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 위에 선 사람을 실어 나르는 움직이는 판(지금은 원판·풍차). 언리얼의 movement base, KCC의 PhysicsMover에 해당한다.
    /// 문·조리개처럼 열리면 떨어져야 하는 바닥은 구현하지 않는다(엔진의 "탈 수 없는 바닥" 설정과 같은 선택).
    /// <para>판이 여러 조각이면 사람이 어느 조각 위인지는 <b>발 위치로</b> 고른다 —
    /// 두 조각 이음매에서 물리 질의가 어느 쪽을 먼저 답하느냐에 맡기면 클·서가 다른 조각을 고를 수 있다.</para>
    /// </summary>
    public interface IMovingPlatform
    {
        /// <summary>발밑에서 찾은 콜라이더가 이 판 것이면, 발 위치로 고른 조각(자세 기준 트랜스폼)을 돌려준다.</summary>
        bool TryGetPart(Collider hit, Vector3 feet, out Transform part);

        /// <summary>그 조각의 그 틱 자세 행렬(부모까지). 트랜스폼을 옮기지 않고 판의 식으로만 낸다.</summary>
        Matrix4x4 PartWorldAt(Transform part, double tick);
    }
}
