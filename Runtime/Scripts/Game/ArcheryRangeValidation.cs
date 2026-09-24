using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 씬과 마스터데이터가 같은 말을 하는지 판 시작에 한 번 대조한다.
    /// <b>둘은 서로를 모른다</b> — 엇갈려도 예외가 안 나고 판만 이상해지므로 여기서 잡는다.
    /// </summary>
    public static class ArcheryRangeValidation
    {
        /// <summary>손으로 놓는 자리라 이만큼(m)까지는 어긋나도 같은 것으로 본다.</summary>
        public const float DistanceToleranceMeters = 1.5f;

        /// <summary>문제가 있으면 사람이 읽을 한 문장, 없으면 null.</summary>
        public static string Check(ArcheryRangeLayout layout, ArcheryRangeSettings range, int archerCount,
                                   ArcheryCourseKind courseKind = ArcheryCourseKind.Range)
        {
            //  한 발 승부는 모두가 레인 0 하나를 같이 쓰고, 자리를 여러 번 다시 쓴다.
            bool shootOff = courseKind == ArcheryCourseKind.ShootOff;

            if (layout == null || layout.IsEmpty)
            {
                return "맵에 ArcheryLane이 하나도 없다 — 사거리 맵인데 레인을 안 찍었거나 맵 씬이 안 떴다";
            }

            if (shootOff == false && layout.Lanes.Count < archerCount)
            {
                return $"레인이 {layout.Lanes.Count}개인데 사수는 {archerCount}명이다 — "
                     + "남는 사수는 과녁이 영영 안 뜬다";
            }

            if (shootOff == false && layout.StandCount != range.Stands.Count)
            {
                return $"씬의 과녁 자리가 {layout.StandCount}개인데 마스터데이터는 {range.Stands.Count}개를 말한다 "
                     + "— 화살 수(= 과녁 수)가 어긋난다";
            }

            for (int s = 0; s < range.Stands.Count; s++)
            {
                var declared = range.Stands[s];
                if (declared.StandIndex < 0 || declared.StandIndex >= layout.StandCount)
                {
                    //  이 값을 그냥 두면 ArcheryCourse.Fill이 lane.Stands[StandIndex]를 매 틱
                    //  인덱싱하다가 그 틱에 처음 죽는다 — 판 시작에 미리 잡아야 원인이 바로 보인다.
                    return $"마스터데이터가 자리 번호 {declared.StandIndex}를 가리키는데 "
                         + $"레인에는 자리가 {layout.StandCount}개(0~{layout.StandCount - 1})뿐이다";
                }
            }

            int lanesToCheck = shootOff ? 1 : layout.Lanes.Count;
            for (int lane = 0; lane < lanesToCheck; lane++)
            {
                for (int s = 0; s < range.Stands.Count; s++)
                {
                    var declared = range.Stands[s];
                    float actual = Vector3.Distance(
                        layout.Lanes[lane].ShooterPosition, layout.Lanes[lane].Stands[declared.StandIndex]);

                    if (Mathf.Abs(actual - declared.DistanceM) > DistanceToleranceMeters)
                    {
                        return $"레인 {lane}의 자리 {declared.StandIndex}가 실제로는 {actual:0.#}m인데 "
                             + $"마스터데이터는 {declared.DistanceM}m라고 적혀 있다 — "
                             + "배포 데이터 검사(사거리·노출 시간)가 틀린 거리로 판단하게 된다";
                    }
                }
            }

            return null;
        }
    }
}
