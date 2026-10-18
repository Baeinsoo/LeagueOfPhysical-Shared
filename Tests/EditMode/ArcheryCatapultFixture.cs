using UnityEngine;

namespace LOP.Tests
{
    /// <summary>투석기 사냥 시험 공용 값. 2차 spec §2 표·1차 리듬과 같은 숫자다(데이터 행과 같아야 한다).</summary>
    internal static class ArcheryCatapultFixture
    {
        public const long StartTick = 1000;
        public const float Dt = 0.02f;
        public static readonly Vector3 LaneOrigin = Vector3.zero;
        public static readonly Vector3 Forward = Vector3.forward;
        //  사수 기준 오른쪽 — ArcheryTargetMotion.ShooterRightAxis(-Forward)와 같다.
        public static readonly Vector3 Right = Vector3.right;

        //  소개 1.6초(80) · 사냥 25초(1250) · 결과 2.2초(110), 3라운드. 3.2초(160)마다, 준비 0.7초(35),
        //  마지막 3초(150)는 새 무리 없음, 수명 2.6초(130), 금지물 35%, 가로 ±16 m, 대박 28%.
        public static ArcheryCatapultSettings Settings()
            => new ArcheryCatapultSettings(3, 80, 1250, 110, 160, 35, 150, 130, 0.35f, 16f, jackpotChance: 0.28f);

        //  2차 spec §2 표 그대로(데이터 #ArcheryTarget 14~22와 같다). 시작 높이를 spec이 안 적은 것은 시안의 4~10 m.
        public static ArcheryTargetKind[] Kinds(bool withTraps = true, bool withJackpots = true)
        {
            var list = new System.Collections.Generic.List<ArcheryTargetKind>
            {
                Kind(14, 1.0f, 1, 40, false, false, 30f, 36f, 2.0f, 3.0f, 0,
                     new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, bobAmplitudeM: 0.6f, bobHz: 0.48f)),
                Kind(15, 0.75f, 2, 25, false, false, 38f, 46f, 2.5f, 3.5f, 0,
                     new ArcheryCatapultMotion(heightMinM: 2.5f, heightMaxM: 5f, arcMinM: 3f, arcMaxM: 4.5f)),
                Kind(16, 0.85f, 2, 25, false, false, 36f, 44f, 5.5f, 7.0f, 0,
                     new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, spinHz: 1.1f)),
                Kind(17, 0.45f, 3, 20, false, false, 46f, 55f, 0.5f, 1.5f, 0,
                     new ArcheryCatapultMotion(heightMinM: 1f, heightMaxM: 3f, climbMinMps: 3.5f, climbMaxMps: 5f)),
            };
            if (withJackpots)
            {
                list.Add(Kind(18, 0.8f, 5, 1, false, true, 46f, 54f, 1.6f, 2.2f, 0,
                              new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, bobAmplitudeM: 0.3f, bobHz: 0.32f, lateralHalfM: 8f)));
                list.Add(Kind(19, 0.95f, 5, 1, false, true, 44f, 52f, 1.8f, 2.4f, 0,
                              new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, bobAmplitudeM: 0.3f, bobHz: 0.32f, lateralHalfM: 8f)));
            }
            if (withTraps)
            {
                var forbidden = new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, lateralHalfM: 10f);
                list.Add(Kind(20, 0.75f, -3, 1, true, false, 32f, 48f, 2.5f, 3.5f, 75, forbidden));
                list.Add(Kind(21, 0.8f, -3, 1, true, false, 32f, 48f, 2.5f, 3.5f, 75, forbidden));
                list.Add(Kind(22, 0.8f, -3, 1, true, false, 32f, 48f, 2.0f, 3.0f, 75, forbidden));
            }
            return list.ToArray();
        }

        public static ArcheryConfig Config(bool withTraps = true, bool withJackpots = true)
            => new ArcheryConfig(
                wavePeriodTicks: 0, minTargets: 0, maxTargets: 0,
                spawnRadius: 16f, spawnMinY: 3.5f, spawnMaxY: 13f, minSeparation: 0f,
                trapRatioMin: 0f, trapRatioMax: 0f,
                shakeFreeSeconds: 0f, shakeRampSeconds: 2.5f, shakeMaxDegrees: 0f,
                riseHeightMin: 0.5f, riseHeightMax: 1.5f, staggerTicks: 0, restTicks: 0,
                kinds: Kinds(withTraps, withJackpots), courseKind: ArcheryCourseKind.Catapult,
                arrowMinSpeedMps: 50f, arrowMaxSpeedMps: 100f, catapult: Settings());

        private static ArcheryTargetKind Kind(int id, float radius, int points, int weight, bool trap, bool jackpot,
                                              float dMin, float dMax, float vMin, float vMax, int stun,
                                              ArcheryCatapultMotion motion)
            => new ArcheryTargetKind(radius, points, weight, trap, ArcheryTargetShape.Sphere,
                                     new[] { new ArcheryRingBand(1f, points) },
                                     id: id, distanceMinM: dMin, distanceMaxM: dMax,
                                     driftMinMps: vMin, driftMaxMps: vMax, stunTicks: stun,
                                     isJackpot: jackpot, motion: motion);
    }
}
