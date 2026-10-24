using System;
using System.Collections.Generic;

namespace LOP
{
    /// <summary>원 한 단계의 표 값. 쉼 -> 축소 -> 끝 반지름, 그리고 원 밖에서 초당 받는 피해.</summary>
    public readonly struct MountedArcheryZoneStageConfig
    {
        public readonly float restSeconds;
        public readonly float shrinkSeconds;
        public readonly float endRadius;
        public readonly float damagePerSecond;

        public MountedArcheryZoneStageConfig(float restSeconds, float shrinkSeconds, float endRadius, float damagePerSecond)
        {
            this.restSeconds = restSeconds;
            this.shrinkSeconds = shrinkSeconds;
            this.endRadius = endRadius;
            this.damagePerSecond = damagePerSecond;
        }
    }

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
        public int MaxHp { get; }
        public int ArrowDamage { get; }
        public float LagCompMaxSeconds { get; }
        public float ZoneInitialRadius { get; }
        public float ZoneFirstRevealSeconds { get; }
        /// <summary>조준점 둘레 이 각(도) 안의 적에게 조준 보정이 걸린다.</summary>
        public float AimAssistConeDeg { get; }
        public float AimAssistRange { get; }
        /// <summary>적이 조준 원뿔 안일 때 끌기 감도에 곱하는 값(작을수록 느려진다).</summary>
        public float AimAssistFriction { get; }
        /// <summary>적의 화면 움직임을 따라가 주는 비율(0~1).</summary>
        public float AimAssistTrack { get; }
        /// <summary>마지막 원이 다 줄어든 뒤 1초마다 더해지는 원 밖 피해(초당).</summary>
        public float ZoneOvertimeDpsGrowth { get; }
        /// <summary>순서대로(1단계부터).</summary>
        public IReadOnlyList<MountedArcheryZoneStageConfig> ZoneStages { get; }

        public MountedArcheryConfig(float matchDurationSeconds, float arrowMinSpeed, float arrowMaxSpeed,
                                    float riderEyeHeight, MountSettings mount,
                                    int maxHp, int arrowDamage, float lagCompMaxSeconds,
                                    float zoneInitialRadius, float zoneFirstRevealSeconds,
                                    IReadOnlyList<MountedArcheryZoneStageConfig> zoneStages,
                                    float aimAssistConeDeg, float aimAssistRange,
                                    float aimAssistFriction, float aimAssistTrack,
                                    float zoneOvertimeDpsGrowth)
        {
            MatchDurationSeconds = matchDurationSeconds;
            ArrowMinSpeed = arrowMinSpeed;
            ArrowMaxSpeed = arrowMaxSpeed;
            RiderEyeHeight = riderEyeHeight;
            Mount = mount;
            MaxHp = maxHp;
            ArrowDamage = arrowDamage;
            LagCompMaxSeconds = lagCompMaxSeconds;
            ZoneInitialRadius = zoneInitialRadius;
            ZoneFirstRevealSeconds = zoneFirstRevealSeconds;
            ZoneStages = zoneStages ?? Array.Empty<MountedArcheryZoneStageConfig>();
            AimAssistConeDeg = aimAssistConeDeg;
            AimAssistRange = aimAssistRange;
            AimAssistFriction = aimAssistFriction;
            AimAssistTrack = aimAssistTrack;
            ZoneOvertimeDpsGrowth = zoneOvertimeDpsGrowth;
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

        /// <summary>되감기 상한(초)을 틱 수로. 이만큼보다 오래 전 모습으로는 판정하지 않는다.</summary>
        public int LagCompMaxTicks(float tickInterval)
            => (int)Math.Round(LagCompMaxSeconds / tickInterval);

        public long MatchDurationTicks(float tickInterval)
            => (long)Math.Round(MatchDurationSeconds / tickInterval);
    }
}
