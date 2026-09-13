using UnityEngine;

namespace LOP
{
    /// <summary>과녁 하나를 맞힌 결과. 획득과 벌점을 따로 들고 있다 — 결과 화면이 내역을 보여준다.</summary>
    public readonly struct ArcheryHitOutcome
    {
        public readonly int Gained;
        public readonly int Lost;

        /// <summary>점수가 실제로 움직이는 양(부호 있음). 연출용으로 클라에 전달되지만, 지금은
        /// 이 값을 실제로 그리는 화면이 없다 — 함정을 맞히면 과녁이 사라지고 HUD 합계가
        /// 줄어드는 것으로만 드러난다.</summary>
        public int Delta => Gained - Lost;

        public ArcheryHitOutcome(int gained, int lost)
        {
            Gained = gained;
            Lost = lost;
        }
    }

    /// <summary>
    /// <b>과녁을 맞혔을 때 무슨 일이 일어나는가.</b> 이 게임에서 그것을 정하는 곳은 여기 하나뿐이다 —
    /// 나중에 벌칙을 "점수 차감" 말고 다른 것(예: 몇 초간 활을 못 당김)으로 바꾸고 싶어지면
    /// 이 함수만 고치면 된다.
    ///
    /// <para>일반화된 효과 시스템을 짓지 않는다. 지금 효과가 둘뿐이라 그건 낭비다.</para>
    /// </summary>
    public static class ArcheryHitRules
    {
        public static ArcheryHitOutcome Resolve(in ArcheryTarget target)
        {
            if (target.IsTrap)
            {
                //  데이터에 -3으로 적든 3으로 적든 같은 벌점이 되게 한다. 적는 사람이 부호를
                //  어느 쪽으로 쓸지 헷갈려도 값이 두 배로 틀리지 않는다.
                return new ArcheryHitOutcome(0, Mathf.Abs(target.Points));
            }

            //  성한 과녁에 음수가 적혀 있으면 점수를 몰래 깎는 대신 아무것도 주지 않는다.
            return new ArcheryHitOutcome(Mathf.Max(target.Points, 0), 0);
        }
    }
}
