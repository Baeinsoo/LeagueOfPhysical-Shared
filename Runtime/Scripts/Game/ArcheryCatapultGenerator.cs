using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 투석기 사냥의 한 무리(볼리)를 채운다. 상태 없는 순수 계산이라 클·서가 같은 씨앗·같은 무리 번호·
    /// 같은 레인·<b>같은 명단 수</b>를 넣으면 같은 목록이 나온다 — <b>그래서 표적을 통신하지 않는다</b>.
    ///
    /// <para><b>난수를 꺼내는 순서가 곧 계약이다</b>(2차, 3차도 그대로) — 금지물 여부 → 대박 여부 → (짝마다) 일반 뽑기 →
    /// (대박이 끼면) 대박 뽑기 → (금지물이 끼면) 금지물 뽑기. 뽑기 한 번 = 종류 → 거리 → 시작 높이 → 가로 위치 →
    /// 방향 → 가로 속도 → 호 → 솟음 → 위상(종류가 그 움직임을 안 써도 늘 아홉 번). 끼워 넣으면 그 뒤가 전부 밀려
    /// 클·서가 다른 표적을 본다. 새 뽑기는 맨 끝에만 붙인다.</para>
    ///
    /// <para><b>나오는 자리(3차 spec §1)</b> — 투석기 없이 표적마다 궤적에 맞는 자리에서 나온다. 같은 아홉 번의 값을
    /// 종류의 데이터가 다르게 읽을 뿐이라 뽑는 횟수는 그대로다: 바깥 띠(<see cref="ArcheryCatapultMotion.SpawnSideMaxM"/>)가
    /// 있으면 가로 위치의 부호가 쪽, 크기가 띠 안 자리이고 방향은 늘 가운데 쪽(뽑은 방향은 버린다). 없으면
    /// 가로 위치 × 나오는 범위가 출발점이고 방향은 뽑은 대로다.</para>
    ///
    /// <para><b>일반 표적 수 = min(6, max(2, ⌈인원 ÷ 2⌉))</b> — 표적 하나를 두 명 안팎이 노리게 해서 4인이든 8인이든
    /// 경쟁 강도가 비슷하다. 자리가 6개뿐이라(<see cref="MaxNormalSlots"/>) 인원이 많아도 여섯에서 멈춘다 —
    /// 대박 자리(<see cref="JackpotSlot"/> = 6)를 침범하지 않는다. 짝은 한 번 뽑아 거울로 둘(x ↔ −x, 방향 반대),
    /// 홀수면 마지막 짝은 한쪽만.</para>
    /// </summary>
    public static class ArcheryCatapultGenerator
    {
        //  웨이브 생성기(Combine(seed, waveIndex))·코스 순서 뽑기와 씨앗이 겹치지 않게 가른다.
        public static readonly ulong Salt = GameFramework.Rng.Hashing.Fnv1a64("archery-catapult");

        //  자리 번호 — 일반은 0부터 차례로, 대박·금지물은 고정(인원이 바뀌어도 번호가 안 움직인다).
        //  먹힘 마스크(ArcheryStateToC)는 이 번호의 비트다.
        public const int MaxNormalSlots = 6;
        public const int JackpotSlot = 6;
        public const int TrapSlot = 7;

        public const int MinNormalCount = 2;
        public const int PlayersPerTarget = 2;

        /// <summary>그 인원에 띄울 일반 표적 수. 인원 = 매치 시작 명단 수(중간에 나간 사람도 센다 — 클·서가 같은 수를 본다).
        /// 최대 <see cref="MaxNormalSlots"/>(6)에서 멈춘다 — 그래야 자리 번호 6번(<see cref="JackpotSlot"/>)에 안 닿는다.</summary>
        public static int NormalCountFor(int players)
            => Mathf.Min(MaxNormalSlots, Mathf.Max(MinNormalCount, (players + PlayersPerTarget - 1) / PlayersPerTarget));

        private readonly struct Throw
        {
            public readonly ArcheryTargetKind Kind;
            public readonly float Distance;
            public readonly float Height;
            public readonly float Lateral;     // −1~1 — 바깥 띠 종류는 부호 = 쪽, 크기 = 띠 안 자리
            public readonly float Direction;   // −1 또는 +1 (사수 기준 왼쪽/오른쪽으로 난다). 바깥 띠 종류는 안 쓴다
            public readonly float Speed;
            public readonly float Arc;
            public readonly float Climb;
            public readonly float Phase;       // 0~1, 출렁·회전의 시작 위상

            public Throw(ArcheryTargetKind kind, float distance, float height, float lateral, float direction,
                         float speed, float arc, float climb, float phase)
            {
                Kind = kind; Distance = distance; Height = height; Lateral = lateral; Direction = direction;
                Speed = speed; Arc = arc; Climb = climb; Phase = phase;
            }
        }

        public static void Fill(List<ArcheryTarget> into, ulong matchSeed, int volleyIndex, ArcheryConfig config, int rosterCount,
                                Vector3 laneOrigin, Vector3 laneForward, long gameplayStartTick, float tickInterval)
        {
            into.Clear();
            var s = config.Catapult;
            if (volleyIndex < 0 || volleyIndex >= s.VolleyCount || config.NormalKinds.Count == 0)
            {
                return;
            }

            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(
                GameFramework.Rng.Hashing.Combine(matchSeed, Salt), (ulong)(long)volleyIndex));

            //  1) 2) 낄지 말지를 먼저 다 뽑는다. 종류 데이터가 없어도 뽑는다 — 뽑는 횟수가 데이터에 따라 흔들리지 않게.
            //  일반 뽑기보다 앞이라 인원이 달라도 앞쪽 짝은 같은 값을 받는다.
            bool withTrap = rng.NextFloat01() < s.TrapChance;
            bool withJackpot = rng.NextFloat01() < s.JackpotChance;

            Vector3 right = ArcheryTargetMotion.ShooterRightAxis(-laneForward);
            long launchTick = ArcheryCatapultSchedule.LaunchTick(s, volleyIndex, gameplayStartTick);

            //  3) 일반 — 한 번 뽑아 거울로 둘. 홀수면 마지막 짝은 A(+1) 하나만.
            int normals = NormalCountFor(rosterCount);
            for (int slot = 0; slot < normals; slot += 2)
            {
                var pair = Draw(config.NormalKinds, ref rng);
                into.Add(Make(volleyIndex, slot, pair, +1f, laneOrigin, laneForward, right, launchTick, tickInterval, s));
                if (slot + 1 < normals)
                {
                    into.Add(Make(volleyIndex, slot + 1, pair, -1f, laneOrigin, laneForward, right, launchTick, tickInterval, s));
                }
            }

            //  4) 대박 5) 금지물 — 일반 뒤에 뽑으므로 이 데이터가 바뀌어도 일반 표적은 그대로다.
            if (withJackpot && config.JackpotKinds.Count > 0)
            {
                into.Add(Make(volleyIndex, JackpotSlot, Draw(config.JackpotKinds, ref rng), +1f,
                              laneOrigin, laneForward, right, launchTick, tickInterval, s));
            }
            if (withTrap && config.TrapKinds.Count > 0)
            {
                into.Add(Make(volleyIndex, TrapSlot, Draw(config.TrapKinds, ref rng), +1f,
                              laneOrigin, laneForward, right, launchTick, tickInterval, s));
            }
        }

        private static Throw Draw(IReadOnlyList<ArcheryTargetKind> pool, ref GameFramework.Rng.DeterministicRandom rng)
        {
            var kind = ArcheryWaveGenerator.PickKind(pool, ref rng);
            var m = kind.Motion;
            float distance = rng.Range(kind.DistanceMinM, kind.DistanceMaxM);
            float height = rng.Range(m.HeightMinM, m.HeightMaxM);
            float lateral = rng.Range(-1f, 1f);
            float direction = rng.NextFloat01() < 0.5f ? -1f : 1f;
            float speed = rng.Range(kind.DriftMinMps, kind.DriftMaxMps);
            float arc = rng.Range(m.ArcMinM, m.ArcMaxM);
            float climb = rng.Range(m.ClimbMinMps, m.ClimbMaxMps);
            float phase = rng.NextFloat01();
            return new Throw(kind, distance, height, lateral, direction, speed, arc, climb, phase);
        }

        //  출발점(3차) — 길의 가운데가 아니라 <b>나오는 자리</b>다. 빨리 나는 표적이 범위(±16 m) 밖으로 날아가도 된다:
        //  바깥에서 들어와 가운데를 지나므로 화면 안에 있는 시간은 충분하다(3차 spec §1).
        //  mirror(+1/−1)를 가로 성분에만 곱해 짝이 정확히 거울이 되게 한다(부호 뒤집기는 float에서 오차가 없다).
        private static ArcheryTarget Make(int volley, int slot, in Throw t, float mirror,
                                          Vector3 laneOrigin, Vector3 forward, Vector3 right,
                                          long launchTick, float tickInterval, ArcheryCatapultSettings s)
        {
            var kind = t.Kind;
            var m = kind.Motion;
            float startX, direction;
            if (m.SpawnSideMaxM > 0f)
            {
                //  접시·종·피냐타: 좌우 바깥 띠에서 나와 가운데 쪽으로 — 길 끝에서 나와 밖으로 사라지는 일이 없다.
                float side = t.Lateral < 0f ? -1f : 1f;
                startX = side * (m.SpawnSideMinM + (m.SpawnSideMaxM - m.SpawnSideMinM) * Mathf.Abs(t.Lateral));
                direction = -side;
            }
            else
            {
                float half = m.LateralHalfM > 0f ? m.LateralHalfM : s.LateralHalfRangeM;
                startX = t.Lateral * half;
                direction = t.Direction;
            }
            startX *= mirror;
            direction *= mirror;

            //  수명은 종류가 정할 수 있다 — 대박은 2초로 짧다(3차 spec §2). 0이면 무리 공통 수명.
            float life = (m.LifeTicks > 0 ? m.LifeTicks : s.TargetLifeTicks) * tickInterval;

            Vector3 origin = laneOrigin + right * startX + Vector3.up * t.Height + forward * t.Distance;
            return new ArcheryTarget(volley, slot, origin, 0f, launchTick,
                                     kind.Radius, kind.Points, kind.IsTrap, kind.Shape, kind.Bands,
                                     -forward, life, string.Empty, 0f, 0f, isShared: false,
                                     drift: right * (direction * t.Speed), arcHeight: t.Arc, stunTicks: kind.StunTicks, kindId: kind.Id,
                                     isJackpot: kind.IsJackpot, bobAmplitude: m.BobAmplitudeM,
                                     bobHz: m.BobHz, spinHz: m.SpinHz,
                                     climbMps: t.Climb, motionPhase: t.Phase,
                                     swing: right * (direction * m.SwingAmplitudeM), swingDip: m.SwingDipM);
        }
    }
}
