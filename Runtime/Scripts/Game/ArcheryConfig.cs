using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Archery 튜닝값. MasterData(<c>TbArcheryConfig</c>/<c>TbArcheryTarget</c>)에서 사이드 provider가
    /// 채워 시뮬에 넘긴다(Shared는 MasterData 패키지를 참조하지 않는다 — <see cref="SkydiveConfig"/>와 같은 짝).
    ///
    /// <para><b>클·서가 반드시 같은 값을 들어야 한다.</b> 하나라도 다르면 웨이브 계산이 갈려
    /// 서로 다른 과녁을 보게 되는데, 화면은 멀쩡해 보이고 점수만 이상해진다.</para>
    /// </summary>
    public sealed class ArcheryConfig
    {
        /// <summary>웨이브 하나가 서 있는 틱 수. 과녁 수명도 이것과 같다 — 다음 웨이브가 오면 사라진다.</summary>
        public int WavePeriodTicks { get; }

        /// <summary>한 웨이브의 최소 과녁 수.</summary>
        public int MinTargets { get; }

        /// <summary>한 웨이브의 최대 과녁 수(이 값 포함).</summary>
        public int MaxTargets { get; }

        /// <summary>과녁이 뜨는 원기둥의 반지름(m). 가운데는 원점이다.</summary>
        public float SpawnRadius { get; }

        /// <summary>과녁이 뜨는 가장 낮은 높이(m).</summary>
        public float SpawnMinY { get; }

        /// <summary>과녁이 뜨는 가장 높은 높이(m).</summary>
        public float SpawnMaxY { get; }

        /// <summary>같은 웨이브의 과녁 중심끼리 최소한 이만큼은 떨어뜨린다(m).</summary>
        public float MinSeparation { get; }

        /// <summary>뽑을 수 있는 과녁 종류. 비어 있으면 과녁이 안 뜬다.</summary>
        public IReadOnlyList<ArcheryTargetKind> Kinds { get; }

        /// <summary>
        /// 종류 중 가장 큰 과녁의 반경(m). 종류가 없으면 0이다.
        ///
        /// <para>겹침을 따질 때 기준이 된다 — <see cref="MinSeparation"/>은 중심 사이 거리 <b>하나로</b>
        /// 모든 조합을 막으므로, 가장 큰 둘이 맞닿는 거리(이 값의 두 배)보다 짧으면 간격을 지켜도
        /// 겹칠 수 있다. 배포 데이터가 그 관계를 지키는지는 MasterData 쪽 테스트가 본다.</para>
        /// </summary>
        public float MaxTargetRadius { get; }

        /// <summary>과녁이 솟아오르는 높이의 하한(m).</summary>
        public float RiseHeightMin { get; }

        /// <summary>
        /// 과녁이 솟아오르는 높이의 상한(m). <b>과녁마다 이 사이에서 뽑는다</b> — 고정이면 몇 번
        /// 보고 나서 "언제쯤 정점"이 몸에 배어 리듬만으로 쏘게 된다.
        ///
        /// <para>⚠️ 너무 높이 잡으면 과녁이 한 틱에 자기 반지름보다 많이 움직여 판정이 뚫린다.
        /// 중력 20·가장 작은 과녁 반지름 0.2m에서 상한은 약 2.5m다 — 배포 데이터 검사가 지킨다.</para>
        /// </summary>
        public float RiseHeightMax { get; }

        /// <summary>묶음 안에서 다음 과녁이 솟기까지의 간격(틱). 일정해야 리듬이 생긴다.</summary>
        public int StaggerTicks { get; }

        /// <summary>묶음이 끝나고 다음 묶음까지의 쉼(틱). 끊겼다 시작해야 매 묶음이 새로 긴장된다.</summary>
        public int RestTicks { get; }

        /// <summary>
        /// 묶음이 다 끝나기까지 걸리는 틱 수 — <b>가장 높이 솟는 과녁</b> 기준이다.
        /// <see cref="WavePeriodTicks"/>가 이보다 짧으면 마지막 과녁이 공중에서 잘려 사라진다 —
        /// 에러는 안 나므로 배포 데이터 검사가 지킨다.
        /// </summary>
        public int BurstTicks { get; }

        /// <summary>한 웨이브에서 함정이 차지하는 비율의 하한(0~1).</summary>
        public float TrapRatioMin { get; }

        /// <summary>
        /// 한 웨이브에서 함정이 차지하는 비율의 상한(0~1). 웨이브마다 이 사이에서 하나를 뽑는다 —
        /// 그래야 "전부 함정인 웨이브"의 빈도를 전체 함정 빈도와 <b>따로</b> 조절할 수 있다.
        /// </summary>
        public float TrapRatioMax { get; }

        /// <summary>이 시간(초)까지는 당기고 있어도 손이 안 떨린다.</summary>
        public float ShakeFreeSeconds { get; }

        /// <summary>흔들림이 0에서 최대까지 자라는 데 걸리는 시간(초).</summary>
        public float ShakeRampSeconds { get; }

        /// <summary>가장 심할 때의 흔들림 폭(도).</summary>
        public float ShakeMaxDegrees { get; }

        /// <summary>함정이 아닌 종류만. 비어 있으면 성한 과녁이 안 뜬다.</summary>
        public IReadOnlyList<ArcheryTargetKind> CleanKinds { get; }

        /// <summary>함정 종류만. 비어 있으면 함정이 안 뜬다(비율을 아무리 올려도).</summary>
        public IReadOnlyList<ArcheryTargetKind> TrapKinds { get; }

        public ArcheryConfig(int wavePeriodTicks, int minTargets, int maxTargets,
                             float spawnRadius, float spawnMinY, float spawnMaxY, float minSeparation,
                             float trapRatioMin, float trapRatioMax,
                             float shakeFreeSeconds, float shakeRampSeconds, float shakeMaxDegrees,
                             float riseHeightMin, float riseHeightMax, int staggerTicks, int restTicks,
                             IReadOnlyList<ArcheryTargetKind> kinds)
        {
            WavePeriodTicks = wavePeriodTicks;
            MinTargets = minTargets;
            MaxTargets = maxTargets;
            SpawnRadius = spawnRadius;
            SpawnMinY = spawnMinY;
            SpawnMaxY = spawnMaxY;
            MinSeparation = minSeparation;
            TrapRatioMin = trapRatioMin;
            TrapRatioMax = trapRatioMax;
            ShakeFreeSeconds = shakeFreeSeconds;
            ShakeRampSeconds = shakeRampSeconds;
            ShakeMaxDegrees = shakeMaxDegrees;
            RiseHeightMin = riseHeightMin;
            RiseHeightMax = riseHeightMax;
            StaggerTicks = staggerTicks;
            RestTicks = restTicks;

            //  가장 높이 솟는 과녁이 제일 오래 떠 있다 — 묶음 길이는 그 기준으로 잡아야 안전하다.
            float longestLifetime = 2f * ArcheryTargetMotion.RiseSpeedFor(riseHeightMax)
                                  / ArcheryTargetMotion.Gravity;
            //  틱은 정수라 올림한다 — 내림하면 마지막 한 틱이 모자라 과녁이 땅에 닿기 전에 잘린다.
            int lifetimeTicks = Mathf.CeilToInt(longestLifetime / 0.02f);
            BurstTicks = (maxTargets - 1) * staggerTicks + lifetimeTicks;
            Kinds = kinds;

            float largest = 0f;
            var clean = new List<ArcheryTargetKind>();
            var traps = new List<ArcheryTargetKind>();
            if (kinds != null)
            {
                for (int i = 0; i < kinds.Count; i++)
                {
                    if (kinds[i].Radius > largest)
                    {
                        largest = kinds[i].Radius;
                    }
                    if (kinds[i].IsTrap)
                    {
                        traps.Add(kinds[i]);
                    }
                    else
                    {
                        clean.Add(kinds[i]);
                    }
                }
            }
            MaxTargetRadius = largest;
            CleanKinds = clean;
            TrapKinds = traps;
        }
    }
}
