using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 투석기 사냥의 한 무리(볼리)를 채운다. 상태 없는 순수 계산이라 클·서가 같은 씨앗·같은 무리 번호·
    /// 같은 레인을 넣으면 같은 목록이 나온다 — <b>그래서 표적을 통신하지 않는다</b>(<see cref="ArcheryWaveGenerator"/>와 같은 계약).
    ///
    /// <para><b>난수를 꺼내는 순서가 곧 계약이다</b> — 쏘면-안-됨 여부 → (성한 짝) 종류 → 거리 → 높이 → 가로 위치 →
    /// 방향 → 속도 → 솟는 높이 → (쏘면 안 되는 것이 끼면) 같은 일곱을 한 번 더. 끼워 넣으면 그 뒤가 전부 밀려
    /// 클·서가 다른 표적을 본다. 새 뽑기는 맨 끝에만 붙인다.</para>
    ///
    /// <para><b>짝은 한 번 뽑아 거울로 둘</b>을 만든다(x ↔ −x, 방향 반대). 레인 중심 양쪽이 같은 조건을 받는
    /// 것이 공정성의 핵심이다(spec §3.3).</para>
    /// </summary>
    public static class ArcheryCatapultGenerator
    {
        //  웨이브 생성기(Combine(seed, waveIndex))·코스 순서 뽑기와 씨앗이 겹치지 않게 가른다.
        public static readonly ulong Salt = GameFramework.Rng.Hashing.Fnv1a64("archery-catapult");

        public const int PairSlotA = 0;
        public const int PairSlotB = 1;
        public const int TrapSlot = 2;

        private readonly struct Throw
        {
            public readonly ArcheryTargetKind Kind;
            public readonly float Distance;
            public readonly float Height;
            public readonly float Lateral;     // −1~1, 길의 가운데
            public readonly float Direction;   // −1 또는 +1 (사수 기준 왼쪽/오른쪽으로 난다)
            public readonly float Speed;
            public readonly float Arc;

            public Throw(ArcheryTargetKind kind, float distance, float height, float lateral,
                         float direction, float speed, float arc)
            {
                Kind = kind; Distance = distance; Height = height; Lateral = lateral;
                Direction = direction; Speed = speed; Arc = arc;
            }
        }

        public static void Fill(List<ArcheryTarget> into, ulong matchSeed, int volleyIndex, ArcheryConfig config,
                                Vector3 laneOrigin, Vector3 laneForward, long gameplayStartTick, float tickInterval)
        {
            into.Clear();
            var s = config.Catapult;
            if (volleyIndex < 0 || volleyIndex >= s.VolleyCount || config.CleanKinds.Count == 0)
            {
                return;
            }

            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(
                GameFramework.Rng.Hashing.Combine(matchSeed, Salt), (ulong)(long)volleyIndex));

            //  1) 쏘면 안 되는 것이 끼나. 종류 데이터가 없어도 뽑는다 — 뽑는 횟수가 데이터에 따라 흔들리지 않게.
            bool withTrap = rng.NextFloat01() < s.TrapChance;

            Vector3 right = ArcheryTargetMotion.ShooterRightAxis(-laneForward);
            long launchTick = ArcheryCatapultSchedule.LaunchTick(s, volleyIndex, gameplayStartTick);
            float life = s.TargetLifeTicks * tickInterval;

            //  2) 성한 짝 — 한 번 뽑아 거울로 둘.
            var pair = Draw(config.CleanKinds, config, ref rng);
            into.Add(Make(volleyIndex, PairSlotA, pair, +1f, laneOrigin, laneForward, right, launchTick, life, s.LateralHalfRangeM));
            into.Add(Make(volleyIndex, PairSlotB, pair, -1f, laneOrigin, laneForward, right, launchTick, life, s.LateralHalfRangeM));

            //  3) 쏘면 안 되는 것 — 짝 뒤에 뽑으므로 함정 데이터가 바뀌어도 짝은 그대로다.
            if (withTrap && config.TrapKinds.Count > 0)
            {
                var trap = Draw(config.TrapKinds, config, ref rng);
                into.Add(Make(volleyIndex, TrapSlot, trap, +1f, laneOrigin, laneForward, right, launchTick, life, s.LateralHalfRangeM));
            }
        }

        private static Throw Draw(IReadOnlyList<ArcheryTargetKind> pool, ArcheryConfig config,
                                  ref GameFramework.Rng.DeterministicRandom rng)
        {
            var kind = ArcheryWaveGenerator.PickKind(pool, ref rng);
            float distance = rng.Range(kind.DistanceMinM, kind.DistanceMaxM);
            float height = rng.Range(config.SpawnMinY, config.SpawnMaxY);
            float lateral = rng.Range(-1f, 1f);
            float direction = rng.NextFloat01() < 0.5f ? -1f : 1f;
            float speed = rng.Range(kind.DriftMinMps, kind.DriftMaxMps);
            float arc = rng.Range(config.RiseHeightMin, config.RiseHeightMax);
            return new Throw(kind, distance, height, lateral, direction, speed, arc);
        }

        //  가로 위치 u는 출발점이 아니라 <b>길의 가운데</b>다 — 성배는 2.6초에 26 m를 가므로 출발점만
        //  ±16 m 안에 두면 화면 밖으로 날아간다. 가운데가 움직일 폭 = 16 − 이동/2(음수면 0, 가운데 고정).
        //  mirror(+1/−1)를 가로 성분에만 곱해 짝이 정확히 거울이 되게 한다(부호 뒤집기는 float에서 오차가 없다).
        private static ArcheryTarget Make(int volley, int slot, in Throw t, float mirror,
                                          Vector3 laneOrigin, Vector3 forward, Vector3 right,
                                          long launchTick, float life, float halfRange)
        {
            float travel = t.Speed * life;
            float centerLimit = Mathf.Max(0f, halfRange - travel * 0.5f);
            float startX = (t.Lateral * centerLimit - t.Direction * travel * 0.5f) * mirror;
            float vx = t.Direction * t.Speed * mirror;

            Vector3 origin = laneOrigin + right * startX + Vector3.up * t.Height + forward * t.Distance;
            return new ArcheryTarget(volley, slot, origin, 0f, launchTick,
                                     t.Kind.Radius, t.Kind.Points, t.Kind.IsTrap, t.Kind.Shape, t.Kind.Bands,
                                     -forward, life, string.Empty, 0f, 0f, isShared: false,
                                     drift: right * vx, arcHeight: t.Arc, stunTicks: t.Kind.StunTicks, kindId: t.Kind.Id);
        }
    }
}
