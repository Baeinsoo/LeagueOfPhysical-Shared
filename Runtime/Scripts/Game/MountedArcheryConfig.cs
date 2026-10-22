using System;

namespace LOP
{
    /// <summary>
    /// 말 타고 활쏘기 튜닝값. MasterData 한 행을 사이드 provider가 이 모양으로 옮겨 시뮬에 넘긴다.
    /// 클·서가 같은 값을 들어야 한다 — 다르면 화살 속도와 이동이 갈린다.
    /// </summary>
    public sealed class MountedArcheryConfig
    {
        public float MatchDurationSeconds { get; }
        public float ArrowMinSpeed { get; }
        public float ArrowMaxSpeed { get; }
        public float RiderEyeHeight { get; }
        public MountSettings Mount { get; }

        public MountedArcheryConfig(float matchDurationSeconds, float arrowMinSpeed, float arrowMaxSpeed,
                                    float riderEyeHeight, MountSettings mount)
        {
            MatchDurationSeconds = matchDurationSeconds;
            ArrowMinSpeed = arrowMinSpeed;
            ArrowMaxSpeed = arrowMaxSpeed;
            RiderEyeHeight = riderEyeHeight;
            Mount = mount;
        }

        /// <summary>조준 시스템이 요구하는 큰 설정에서 활 값만 채운 것. 웨이브·흔들림 값은 쓰이지 않는다.</summary>
        public ArcheryConfig ToBowConfig() => new ArcheryConfig(
            wavePeriodTicks: 100, minTargets: 1, maxTargets: 1,
            spawnRadius: 1f, spawnMinY: 0f, spawnMaxY: 1f, minSeparation: 1f,
            trapRatioMin: 0f, trapRatioMax: 0f,
            shakeFreeSeconds: 1f, shakeRampSeconds: 1f, shakeMaxDegrees: 0f,
            riseHeightMin: 0.1f, riseHeightMax: 0.1f, staggerTicks: 1, restTicks: 1,
            kinds: null,
            arrowMinSpeedMps: ArrowMinSpeed, arrowMaxSpeedMps: ArrowMaxSpeed);

        public long MatchDurationTicks(float tickInterval)
            => (long)Math.Round(MatchDurationSeconds / tickInterval);
    }
}
