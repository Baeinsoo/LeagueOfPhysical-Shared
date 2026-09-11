using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 과녁이 언제 몇 개 어디에 뜨는지를 계산한다. 상태가 없는 순수 계산이라 클·서가 같은 입력을
    /// 넣으면 같은 답을 얻는다 — <b>그래서 과녁을 통신으로 보낼 필요가 없다</b>(핑 낮은 사람이 먼저
    /// 보는 일이 없다).
    /// </summary>
    public static class ArcheryWaveGenerator
    {
        // 겹치지 않는 자리를 못 찾아도 영원히 돌면 안 된다. 이 횟수를 넘기면 마지막 후보를 그냥 쓴다 —
        // 아주 드물게 두 과녁이 가깝게 뜨는 편이, 판이 멈추는 것보다 낫다.
        private const int MaxPlacementTries = 12;

        /// <summary>이 틱에 서 있는 웨이브 번호. 아직 출발 전이면 −1이다.</summary>
        public static int WaveIndexAt(long tick, long gameplayStartTick, ArcheryConfig config)
        {
            if (gameplayStartTick == long.MaxValue || tick < gameplayStartTick || config.WavePeriodTicks <= 0)
            {
                return -1;
            }
            return (int)((tick - gameplayStartTick) / config.WavePeriodTicks);
        }

        /// <summary>그 웨이브가 뜨는 틱. <see cref="WaveIndexAt"/>의 역이다.</summary>
        public static long WaveStartTick(int waveIndex, long gameplayStartTick, ArcheryConfig config)
        {
            return gameplayStartTick + (long)waveIndex * config.WavePeriodTicks;
        }

        /// <summary>
        /// 그 웨이브의 과녁을 <paramref name="into"/>에 채운다(먼저 비운다).
        /// <b>난수를 꺼내는 순서가 곧 계약이다</b> — 개수 → 슬롯마다 (종류 → 각도 → 반지름 → 높이).
        /// 이 순서를 바꾸면 같은 씨앗이 다른 과녁을 내놓아 클·서가 갈린다.
        /// </summary>
        public static void Fill(List<ArcheryTarget> into, ulong matchSeed, int waveIndex, ArcheryConfig config)
        {
            into.Clear();
            if (waveIndex < 0 || config.Kinds == null || config.Kinds.Count == 0)
            {
                return;
            }

            var rng = new GameFramework.Rng.DeterministicRandom(
                GameFramework.Rng.Hashing.Combine(matchSeed, (ulong)(long)waveIndex));

            int count = rng.Range(config.MinTargets, config.MaxTargets + 1);
            for (int slot = 0; slot < count; slot++)
            {
                ArcheryTargetKind kind = PickKind(config.Kinds, ref rng);
                Vector3 center = PickCenter(into, config, ref rng);
                into.Add(new ArcheryTarget(waveIndex, slot, center, kind.Radius, kind.Points));
            }
        }

        private static ArcheryTargetKind PickKind(IReadOnlyList<ArcheryTargetKind> kinds,
                                                  ref GameFramework.Rng.DeterministicRandom rng)
        {
            int total = 0;
            for (int i = 0; i < kinds.Count; i++)
            {
                total += Mathf.Max(kinds[i].Weight, 0);
            }
            if (total <= 0)
            {
                return kinds[0];   // 가중치를 다 0으로 넣어 둔 데이터 — 첫 종류로 버틴다
            }

            int roll = rng.Range(0, total);
            for (int i = 0; i < kinds.Count; i++)
            {
                roll -= Mathf.Max(kinds[i].Weight, 0);
                if (roll < 0)
                {
                    return kinds[i];
                }
            }
            return kinds[kinds.Count - 1];
        }

        // 이미 놓인 과녁과 너무 가까우면 다시 뽑는다. 난수 소비 횟수가 자리마다 달라지지만,
        // "앞 슬롯이 어디 놓였나"까지 양쪽이 똑같이 알고 있으므로 결과는 여전히 결정론적이다.
        private static Vector3 PickCenter(List<ArcheryTarget> placed, ArcheryConfig config,
                                          ref GameFramework.Rng.DeterministicRandom rng)
        {
            Vector3 candidate = default;
            for (int attempt = 0; attempt < MaxPlacementTries; attempt++)
            {
                float angle = rng.Range(0f, Mathf.PI * 2f);
                // 제곱근을 씌워야 원판 위에 고르게 퍼진다 — 그냥 뽑으면 가운데로 몰린다.
                float radius = config.SpawnRadius * Mathf.Sqrt(rng.NextFloat01());
                float y = rng.Range(config.SpawnMinY, config.SpawnMaxY);
                candidate = new Vector3(Mathf.Sin(angle) * radius, y, Mathf.Cos(angle) * radius);

                if (FarEnough(candidate, placed, config.MinSeparation))
                {
                    return candidate;
                }
            }
            return candidate;
        }

        private static bool FarEnough(Vector3 candidate, List<ArcheryTarget> placed, float minSeparation)
        {
            for (int i = 0; i < placed.Count; i++)
            {
                if (Vector3.Distance(candidate, placed[i].Center) < minSeparation)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
