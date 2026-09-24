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
        /// <summary>
        /// 이 화살이 이 과녁을 가져갈 수 있나. <b>주인이 없으면 누구든</b>(원형 맵), 주인이 있으면
        /// 주인만(사거리 맵). 점수뿐 아니라 <b>사라지는 것까지</b> 이 한 번으로 막는다 — 점수만
        /// 막고 사라지게 두면 남의 과녁을 태워 버리는 방해가 열린다.
        /// 공유 과녁(한 발 승부)은 누구나 맞힌다.
        /// </summary>
        public static bool CanTake(in ArcheryTarget target, string shooterUserId)
        {
            return target.IsShared
                || string.IsNullOrEmpty(target.OwnerUserId)
                || target.OwnerUserId == shooterUserId;
        }

        /// <summary>
        /// 맞히고 나면 이 과녁이 <b>사라지는가.</b>
        ///
        /// <para><b>주인 없는 과녁만 사라진다</b>(원형 맵) — 거기선 먼저 맞힌 사람이 먹으므로
        /// 사라지지 않으면 한 과녁으로 여럿이 무한정 점수를 낸다.</para>
        ///
        /// <para><b>주인 있는 과녁은 안 사라진다</b>(사거리 맵). 주인만 맞힐 수 있어
        /// (<see cref="CanTake"/>) 훔쳐 갈 사람이 없고, 자리마다 여러 발을 <b>같은 과녁에</b>
        /// 꽂는 것이 실제 양궁의 모양이다. 사라지게 두면 첫 발이 맞는 순간 남은 화살이 쏠 곳을
        /// 잃고, 그 자리의 남은 시간(최대 17초)도 통째로 죽는다. 무엇보다 <b>착탄 기록판에
        /// 점이 하나밖에 안 찍혀</b> 군집이 안 보인다 — 리드를 고칠 근거가 사라진다.</para>
        ///
        /// <para>무한 득점은 화살 수(<c>ArcheryQuiver</c>)가 막는다.</para>
        ///
        /// <para>공유 과녁(한 발 승부)은 누구나 맞히고 안 사라진다 — 여럿이 같은 과녁에 꽂아야
        /// 비교가 된다.</para>
        /// </summary>
        public static bool ConsumedOnHit(in ArcheryTarget target)
        {
            //  공유 과녁은 모두의 화살이 한 과녁에 꽂혀야 비교가 된다.
            return target.IsShared == false && string.IsNullOrEmpty(target.OwnerUserId);
        }

        /// <summary>
        /// 맞은 결과를 정한다. <paramref name="normalizedOffset"/>은 맞은 자리가 중심에서 얼마나
        /// 벗어났는지를 과녁 반지름으로 나눈 값이다(0이 정중앙, 1이 가장자리).
        /// </summary>
        public static ArcheryHitOutcome Resolve(in ArcheryTarget target, float normalizedOffset)
        {
            int points = PointsAt(target, normalizedOffset);

            if (target.IsTrap)
            {
                //  데이터에 -3으로 적든 3으로 적든 같은 벌점이 되게 한다. 적는 사람이 부호를
                //  어느 쪽으로 쓸지 헷갈려도 값이 두 배로 틀리지 않는다.
                return new ArcheryHitOutcome(0, Mathf.Abs(points));
            }

            //  성한 과녁에 음수가 적혀 있으면 점수를 몰래 깎는 대신 아무것도 주지 않는다.
            return new ArcheryHitOutcome(Mathf.Max(points, 0), 0);
        }

        /// <summary>
        /// 맞은 자리가 속한 띠의 점수. 중심 쪽 띠부터 훑어 처음 걸리는 것을 쓴다.
        ///
        /// <para>⚠️ <b>띠 목록이 중심→바깥 순서라는 데 기대고 있다.</b> 순서가 뒤집히면 바깥 띠가
        /// 먼저 걸려서 한가운데를 맞혀도 낮은 점수가 나온다 — 예외도 경고도 없이 점수만 틀린다.
        /// 여기서 정렬하지 않는 이유는 이 함수가 <b>맞힐 때마다</b> 불리기 때문이다. 순서를 맞추는
        /// 것은 목록을 만드는 쪽(사이드 provider)의 몫이고, 그쪽이 그렇게 해야 한다.</para>
        /// </summary>
        private static int PointsAt(in ArcheryTarget target, float normalizedOffset)
        {
            var bands = target.Bands;
            if (bands == null || bands.Count == 0)
            {
                //  띠 데이터가 없는 과녁 — 어디를 맞히든 같은 점수다.
                return target.Points;
            }

            for (int i = 0; i < bands.Count; i++)
            {
                if (normalizedOffset <= bands[i].OuterRatio)
                {
                    return bands[i].Points;
                }
            }

            //  가장자리를 아주 조금 넘은 값(부동소수 오차)이 들어와도 점수가 0이 되면 안 된다.
            return bands[bands.Count - 1].Points;
        }
    }
}
