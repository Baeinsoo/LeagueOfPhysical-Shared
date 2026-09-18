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
        /// <b>난수를 꺼내는 순서가 곧 계약이다</b> — 개수 → (함정 종류가 있으면) 함정 비율 →
        /// 슬롯마다 (함정인가 → 종류 → 각도 → 반지름 → 높이 → <b>솟는 높이</b>).
        /// 이 순서를 바꾸면 같은 씨앗이 다른 과녁을 내놓아 클·서가 갈린다.
        /// <b>뽑는 횟수도 계약의 일부다</b> — 함정 비율 뽑기는 <c>config.TrapKinds</c>가 있을 때만
        /// 일어난다(위 "함정 종류가 있으면"). 그래서 클·서가 서로 다른 마스터데이터를 갖고 있어서
        /// 한쪽만 함정 종류가 있으면, 그 뒤로 이어지는 모든 뽑기가 한 칸씩 밀려 어느 슬롯이든
        /// 완전히 다른 과녁을 내놓는다 — 어떤 종류가 뜨는지만 갈리는 게 아니라 과녁의 위치 자체가
        /// 클·서에서 통째로 어긋난다.
        /// <b>새 뽑기를 추가할 땐 반드시 슬롯 루프의 맨 끝에 붙여라</b> — "솟는 높이" 뒤에 끼워
        /// 넣으면 이 순서가 조용히 깨진다.
        /// <para>솟는 시각은 난수가 아니다 — 슬롯 번호에 간격을 곱한 값이라 리듬이 일정하다.</para>
        /// </summary>
        public static void Fill(List<ArcheryTarget> into, ulong matchSeed, int waveIndex,
                                ArcheryConfig config, long gameplayStartTick)
        {
            into.Clear();
            if (waveIndex < 0 || config.Kinds == null || config.Kinds.Count == 0)
            {
                return;
            }

            var rng = new GameFramework.Rng.DeterministicRandom(
                GameFramework.Rng.Hashing.Combine(matchSeed, (ulong)(long)waveIndex));

            int count = rng.Range(config.MinTargets, config.MaxTargets + 1);

            //  이 웨이브에 함정을 몇 개 둘지 먼저 정한다. 슬롯마다 따로 뽑으면 "전부 함정"이
            //  확률의 곱으로만 나와서, 그 순간의 빈도를 따로 조절할 수 없다.
            int trapCount = 0;
            if (config.TrapKinds.Count > 0)
            {
                float ratio = rng.Range(config.TrapRatioMin, config.TrapRatioMax);
                trapCount = Mathf.Clamp(Mathf.RoundToInt(ratio * count), 0, count);

                //  성한 종류가 아예 없으면 어느 자리를 뽑아도 함정이 나온다 — 아래 대비책이
                //  돌려주는 목록도 전부 함정이기 때문이다. 결과는 어느 쪽이든 같지만, 비율은
                //  "절반"이라 해 놓고 전부 함정이 뜨는 셈이라 읽는 사람이 헷갈린다.
                //  여기서 전부 함정임을 못박아 비율이 말하는 것과 실제가 같아지게 한다.
                if (config.CleanKinds.Count == 0)
                {
                    trapCount = count;
                }
            }

            int remainingTraps = trapCount;
            for (int slot = 0; slot < count; slot++)
            {
                //  남은 슬롯 중 남은 함정 수만큼의 확률로 이 자리를 함정으로 만든다. 슬롯 번호와
                //  함정 여부가 상관되지 않게 하려는 것이다 — 슬롯 번호는 와이어(먹힌 마스크)에
                //  그대로 드러나므로, 상관이 있으면 마스크만 보고 함정 자리를 알 수 있다.
                int remainingSlots = count - slot;
                bool isTrap = rng.Range(0, remainingSlots) < remainingTraps;
                if (isTrap)
                {
                    remainingTraps--;
                }

                var pool = isTrap ? config.TrapKinds : config.CleanKinds;
                if (pool.Count == 0)
                {
                    //  함정 자리는 TrapKinds.Count > 0일 때만, 성한 자리는 위 가드로 CleanKinds가
                    //  빈 경우를 걸러 두므로, 이 분기는 이제 도달할 수 없어야 정상이다. 그래도
                    //  지워 두지 않는다 — PickKind가 빈 목록을 받으면 예외를 던져 판이 죽는다.
                    pool = config.Kinds;
                }

                ArcheryTargetKind kind = PickKind(pool, ref rng);
                Vector3 center = PickCenter(into, config, ref rng);

                //  묶음 안에서 하나씩 연달아 솟는다 — 간격이 일정해야 리듬이 생기고, 리듬이
                //  있어야 손이 맞춰졌다가 그 속의 함정에 걸린다(spec 3.2).
                long spawnTick = ArcheryWaveGenerator.WaveStartTick(waveIndex, gameplayStartTick, config)
                               + (long)slot * config.StaggerTicks;

                //  솟는 높이는 과녁마다 다르다 — 고정이면 "언제쯤 정점"이 몸에 배어 리듬만으로
                //  쏘게 된다. 높이가 다르면 정점 시각도 달라져 매번 봐야 한다.
                //  (난수를 여기서 한 번 더 쓴다 — 순서가 계약이므로 반드시 슬롯 루프 맨 끝이다.)
                float riseHeight = rng.Range(config.RiseHeightMin, config.RiseHeightMax);
                float riseSpeed = ArcheryTargetMotion.RiseSpeedFor(riseHeight);

                into.Add(new ArcheryTarget(waveIndex, slot, center, riseSpeed, spawnTick,
                                           kind.Radius, kind.Points, kind.IsTrap,
                                           kind.Shape, kind.Bands, Vector3.zero,
                                           ArcheryTargetMotion.LifetimeFor(riseSpeed), string.Empty));
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
                if (Vector3.Distance(candidate, placed[i].Origin) < minSeparation)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
