using UnityEngine;

namespace LOP.Tests
{
    /// <summary>투석기 사냥 시험 공용 값. spec §2.3 표·리듬과 같은 숫자다(가중치는 ×10 정수).</summary>
    internal static class ArcheryCatapultFixture
    {
        public const long StartTick = 1000;
        public const float Dt = 0.02f;
        public static readonly Vector3 LaneOrigin = Vector3.zero;
        public static readonly Vector3 Forward = Vector3.forward;
        //  사수 기준 오른쪽 — ArcheryTargetMotion.ShooterRightAxis(-Forward)와 같다.
        public static readonly Vector3 Right = Vector3.right;

        //  소개 1.6초(80) · 사냥 25초(1250) · 결과 2.2초(110), 3라운드. 3.2초(160)마다, 준비 0.7초(35),
        //  마지막 3초(150)는 새 무리 없음, 수명 2.6초(130), 함정 35%, 가로 ±16 m.
        public static ArcheryCatapultSettings Settings()
            => new ArcheryCatapultSettings(3, 80, 1250, 110, 160, 35, 150, 130, 0.35f, 16f);

        public static ArcheryTargetKind[] Kinds(bool withTraps = true)
        {
            var clean = new[]
            {
                Kind(8, 1.2f, 1, 50, false, 30f, 38f, 2.0f, 3.0f, 0),
                Kind(9, 0.9f, 2, 40, false, 38f, 46f, 3.5f, 5.0f, 0),
                Kind(10, 0.6f, 3, 30, false, 46f, 55f, 5.5f, 7.0f, 0),
                Kind(11, 0.5f, 5, 6, false, 48f, 55f, 8.0f, 10.0f, 0),
            };
            if (withTraps == false)
            {
                return clean;
            }
            return new[]
            {
                clean[0], clean[1], clean[2], clean[3],
                Kind(12, 0.85f, -3, 1, true, 32f, 50f, 3.0f, 5.0f, 75),
                Kind(13, 0.85f, -3, 1, true, 32f, 50f, 3.0f, 5.0f, 75),
            };
        }

        public static ArcheryConfig Config(bool withTraps = true)
            => new ArcheryConfig(
                wavePeriodTicks: 0, minTargets: 0, maxTargets: 0,
                spawnRadius: 16f, spawnMinY: 3.5f, spawnMaxY: 13f, minSeparation: 0f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: 0f, shakeRampSeconds: 2.5f, shakeMaxDegrees: 0f,
                riseHeightMin: 0.5f, riseHeightMax: 1.5f, staggerTicks: 0, restTicks: 0,
                kinds: Kinds(withTraps), courseKind: ArcheryCourseKind.Catapult,
                arrowMinSpeedMps: 50f, arrowMaxSpeedMps: 100f, catapult: Settings());

        private static ArcheryTargetKind Kind(int id, float radius, int points, int weight, bool trap,
                                              float dMin, float dMax, float vMin, float vMax, int stun)
            => new ArcheryTargetKind(radius, points, weight, trap, ArcheryTargetShape.Sphere,
                                     new[] { new ArcheryRingBand(1f, points) },
                                     id: id, distanceMinM: dMin, distanceMaxM: dMax,
                                     driftMinMps: vMin, driftMaxMps: vMax, stunTicks: stun);
    }
}
