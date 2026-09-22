using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 사수가 선 <b>사대</b> — 움직일 수 있는 상자의 한가운데와 과녁 쪽 방향. 스폰 때 한 번
    /// 적고 판이 끝날 때까지 안 바뀐다.
    ///
    /// <para><b>왜 몸에 적어 두나</b>: 사대는 레인이 주는 값인데, 사수는 <b>자기 레인 사대에
    /// 스폰</b>된다(서버 <c>ArcheryRuleSystem</c>이 <c>lane.ShooterPosition</c>과 레인 방향으로
    /// 세운다). 그래서 스폰 정보만으로 사대가 결정되고, 클·서가 <b>같은 스폰 정보</b>를 받으므로
    /// 양쪽이 저절로 같은 상자를 쓴다. 레인을 따로 조회하면 맵 씬이 아직 안 떴거나 명단이
    /// 어긋나는 순간마다 한쪽만 안 잘리고, 그러면 조용히 어긋난다.</para>
    ///
    /// <para><see cref="Facing"/>은 <b>스폰 당시</b> 방향이다 — 몸의 현재 회전을 쓰면 안 된다.
    /// 걷는 방향으로 몸이 돌아가는 순간 상자의 앞뒤·좌우 축이 같이 돌아가 버린다.</para>
    /// </summary>
    public class ArcheryStance : GameFramework.World.Component
    {
        /// <summary>사대 한가운데(월드).</summary>
        public Vector3 Origin { get; }

        /// <summary>과녁 쪽. 사대의 "앞뒤" 축이다.</summary>
        public Vector3 Facing { get; }

        public ArcheryStance(Vector3 origin, Vector3 facing)
        {
            Origin = origin;
            Facing = facing;
        }
    }
}
