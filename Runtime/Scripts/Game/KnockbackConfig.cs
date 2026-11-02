using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>넉백 생존전의 숫자 모음. 마스터데이터 한 행을 그대로 옮긴다.</summary>
    public sealed class KnockbackConfig
    {
        public float MatchDurationSeconds { get; }
        public float MoveSpeed { get; }
        public float MoveGain { get; }
        public float ArenaRadius { get; }
        public float TileSize { get; }
        public float HoleRadius { get; }
        public float FallDepth { get; }
        public float SpawnRadius { get; }
        public float ShotSpeed { get; }
        public float ShotRange { get; }
        public float ShotRadius { get; }
        public float HitRadius { get; }
        public int AmmoMax { get; }
        public float ReloadSeconds { get; }
        public float FireCooldownSeconds { get; }
        public float Recoil { get; }
        public int DamagePerHit { get; }
        public float KnockbackBase { get; }
        public float KnockbackScale { get; }
        public float KnockbackCarry { get; }
        public float StunBase { get; }
        public float StunScale { get; }
        public float StunGain { get; }
        public float BodyRestitution { get; }
        public float ZoneFirstRevealSeconds { get; }
        public IReadOnlyList<MountedArcheryZoneStageConfig> ZoneStages { get; }
        public KnockbackPillarLayout Pillars { get; }
        /// <summary>0 낭떠러지(원 밖 칸이 무너짐), 1 위험 지대(원 밖 칸은 남고 위험해짐).</summary>
        public int EdgeMode { get; }
        public float HazardDamagePerSecond { get; }
        public float HazardSpeedScale { get; }
        /// <summary>첫 상자가 떨어지기까지(경기 시작 기준), 그 뒤 간격, 떨어지기 전 예고 시간(초).</summary>
        public float CrateFirstSeconds { get; }
        public float CrateIntervalSeconds { get; }
        public float CrateWarnSeconds { get; }
        public float CratePickupRadius { get; }
        /// <summary>충격파 폭탄이 터질 때 밀리는 반경(m).</summary>
        public float BlastRadius { get; }

        //  시뮬 틱 간격. 초를 틱으로 바꿀 때 클·서가 같은 값을 쓴다.
        public const float TickInterval = 0.02f;

        public KnockbackConfig(float matchDurationSeconds, float moveSpeed, float moveGain, float arenaRadius,
                               float tileSize, float holeRadius, float fallDepth, float spawnRadius,
                               float shotSpeed,
                               float shotRange,
                               float shotRadius,
                               float hitRadius,
                               int ammoMax,
                               float reloadSeconds,
                               float fireCooldownSeconds,
                               float recoil,
                               int damagePerHit,
                               float knockbackBase,
                               float knockbackScale,
                               float knockbackCarry,
                               float stunBase,
                               float stunScale,
                               float stunGain,
                               float bodyRestitution,
                               float zoneFirstRevealSeconds,
                               IReadOnlyList<MountedArcheryZoneStageConfig> zoneStages,
                               KnockbackPillarLayout pillars = null,
                               int edgeMode = 0,
                               float hazardDamagePerSecond = 0f,
                               float hazardSpeedScale = 1f,
                               float crateFirstSeconds = 20f,
                               float crateIntervalSeconds = 20f,
                               float crateWarnSeconds = 1.5f,
                               float cratePickupRadius = 0.9f,
                               float blastRadius = 2.5f)
        {
            EdgeMode = edgeMode;
            HazardDamagePerSecond = hazardDamagePerSecond;
            HazardSpeedScale = hazardSpeedScale;
            CrateFirstSeconds = crateFirstSeconds;
            CrateIntervalSeconds = crateIntervalSeconds;
            CrateWarnSeconds = crateWarnSeconds;
            CratePickupRadius = cratePickupRadius;
            BlastRadius = blastRadius;
            MatchDurationSeconds = matchDurationSeconds;
            MoveSpeed = moveSpeed;
            MoveGain = moveGain;
            ArenaRadius = arenaRadius;
            TileSize = tileSize;
            HoleRadius = holeRadius;
            FallDepth = fallDepth;
            SpawnRadius = spawnRadius;
            ShotSpeed = shotSpeed;
            ShotRange = shotRange;
            ShotRadius = shotRadius;
            HitRadius = hitRadius;
            AmmoMax = ammoMax;
            ReloadSeconds = reloadSeconds;
            FireCooldownSeconds = fireCooldownSeconds;
            Recoil = recoil;
            DamagePerHit = damagePerHit;
            KnockbackBase = knockbackBase;
            KnockbackScale = knockbackScale;
            KnockbackCarry = knockbackCarry;
            StunBase = stunBase;
            StunScale = stunScale;
            StunGain = stunGain;
            BodyRestitution = bodyRestitution;
            ZoneFirstRevealSeconds = zoneFirstRevealSeconds;
            ZoneStages = zoneStages;
            Pillars = pillars ?? KnockbackPillarLayout.None;
        }

        //  올림이라 0.26초는 13틱 — 시간이 모자라 일찍 풀리지 않는다.
        public int Ticks(float seconds) => (int)System.Math.Ceiling(seconds / TickInterval - 1e-4);

        public int CrateFirstTicks => Ticks(CrateFirstSeconds);
        public int CrateIntervalTicks => Ticks(CrateIntervalSeconds);
        public int CrateWarnTicks => Ticks(CrateWarnSeconds);

        /// <summary>위험 지대에서 위험도가 1 오르는 간격(틱). 0이면 오르지 않는다.</summary>
        public int HazardDamageIntervalTicks => HazardDamagePerSecond > 0f
            ? Mathf.Max(1, Mathf.RoundToInt(1f / HazardDamagePerSecond / TickInterval))
            : 0;

        /// <summary>위험 지대 방식에서 마지막 원이 끝난 뒤 바깥부터 전부 무너지기까지의 시간. 값을 바꿔 보게 되면 그때 표로 뺀다.</summary>
        public const float SuddenDeathSeconds = 15f;

        public long MatchDurationTicks(float tickInterval)
            => (long)System.Math.Round(MatchDurationSeconds / tickInterval);

        public KnockbackFloor CreateFloor() => new KnockbackFloor(ArenaRadius, TileSize, HoleRadius, EdgeMode == 1, Ticks(SuddenDeathSeconds));

        //  바닥은 월드와 이동 시스템이 같은 한 개를 나눠 쓴다(칸이 무너지면 둘이 같은 상태를 봐야 한다).
        public KnockbackMoveSystem CreateMoveSystem(KnockbackFloor floor, GameFramework.Physics.ICollisionQuery query, int layerMask)
            => new KnockbackMoveSystem(floor, query, layerMask, MoveSpeed, MoveGain, StunGain, HazardSpeedScale);
    }
}
