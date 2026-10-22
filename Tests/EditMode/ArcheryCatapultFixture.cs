using UnityEngine;

namespace LOP.Tests
{
    /// <summary>투석기 사냥 시험 공용 값. 3차 spec 표·1차 리듬과 같은 숫자다(데이터 행과 같아야 한다).</summary>
    internal static class ArcheryCatapultFixture
    {
        public const long StartTick = 1000;
        public const float Dt = 0.02f;
        public static readonly Vector3 LaneOrigin = Vector3.zero;
        public static readonly Vector3 Forward = Vector3.forward;
        //  사수 기준 오른쪽 — ArcheryTargetMotion.ShooterRightAxis(-Forward)와 같다.
        public static readonly Vector3 Right = Vector3.right;

        //  소개 1.6초(80) · 사냥 25초(1250) · 결과 2.2초(110), 3라운드. 3.6초(180)마다, 준비 0.7초(35),
        //  마지막 4.3초(215 = 준비 + 수명)는 새 무리 없음, 수명 3.6초(180), 금지물 60%(3차), 가로 ±16 m, 대박 28%.
        public static ArcheryCatapultSettings Settings()
            => new ArcheryCatapultSettings(3, 80, 1250, 110, 180, 35, 215, 180, 0.6f, 16f, jackpotChance: 0.28f);

        //  3차 spec 표 그대로(데이터 #ArcheryTarget 14~22와 같다). 2차에서 바뀐 것: 물풍선·수박·로켓은 ±12 m 아무 데서나,
        //  접시는 바깥 6~14 m, 종은 바깥 4~8 m·초속 5~5.8·출렁 없음·2초, 피냐타는 바깥 3~8 m·그네(3 m, 1.6 m)·옆 흐름 없음·2초,
        //  금지물 −4점·사격 금지 2초(100틱).
        public static ArcheryTargetKind[] Kinds(bool withTraps = true, bool withJackpots = true)
        {
            var list = new System.Collections.Generic.List<ArcheryTargetKind>
            {
                Kind(14, 1.0f, 1, 40, false, false, 30f, 36f, 2.0f, 3.0f, 0,
                     new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, bobAmplitudeM: 0.6f, bobHz: 0.48f, lateralHalfM: 12f)),
                Kind(15, 0.75f, 2, 25, false, false, 38f, 46f, 2.5f, 3.5f, 0,
                     new ArcheryCatapultMotion(heightMinM: 2.5f, heightMaxM: 5f, arcMinM: 3f, arcMaxM: 4.5f, lateralHalfM: 12f)),
                Kind(16, 0.85f, 2, 25, false, false, 36f, 44f, 5.5f, 7.0f, 0,
                     new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, spinHz: 1.1f, spawnSideMinM: 6f, spawnSideMaxM: 14f)),
                Kind(17, 0.45f, 3, 20, false, false, 46f, 55f, 0.5f, 1.5f, 0,
                     new ArcheryCatapultMotion(heightMinM: 1f, heightMaxM: 3f, climbMinMps: 3.5f, climbMaxMps: 5f, lateralHalfM: 12f)),
            };
            if (withJackpots)
            {
                list.Add(Kind(18, 0.8f, 5, 1, false, true, 46f, 54f, 5.0f, 5.8f, 0,
                              new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, spawnSideMinM: 4f, spawnSideMaxM: 8f, lifeTicks: 100)));
                list.Add(Kind(19, 0.95f, 5, 1, false, true, 44f, 52f, 0f, 0f, 0,
                              new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, spawnSideMinM: 3f, spawnSideMaxM: 8f, lifeTicks: 100,
                                                        swingAmplitudeM: 3f, swingDipM: 1.6f)));
            }
            if (withTraps)
            {
                var forbidden = new ArcheryCatapultMotion(heightMinM: 4f, heightMaxM: 10f, lateralHalfM: 10f);
                list.Add(Kind(20, 0.75f, -4, 1, true, false, 32f, 48f, 2.5f, 3.5f, 100, forbidden));
                list.Add(Kind(21, 0.8f, -4, 1, true, false, 32f, 48f, 2.5f, 3.5f, 100, forbidden));
                list.Add(Kind(22, 0.8f, -4, 1, true, false, 32f, 48f, 2.0f, 3.0f, 100, forbidden));
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
                arrowMinSpeedMps: 90f, arrowMaxSpeedMps: 180f, catapult: Settings());

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
