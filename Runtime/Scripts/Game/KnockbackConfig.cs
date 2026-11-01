using System.Collections.Generic;

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
                               KnockbackPillarLayout pillars = null)
        {
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

        public long MatchDurationTicks(float tickInterval)
            => (long)System.Math.Round(MatchDurationSeconds / tickInterval);

        public KnockbackFloor CreateFloor() => new KnockbackFloor(ArenaRadius, TileSize, HoleRadius);

        //  바닥은 월드와 이동 시스템이 같은 한 개를 나눠 쓴다(칸이 무너지면 둘이 같은 상태를 봐야 한다).
        public KnockbackMoveSystem CreateMoveSystem(KnockbackFloor floor, GameFramework.Physics.ICollisionQuery query, int layerMask)
            => new KnockbackMoveSystem(floor, query, layerMask, MoveSpeed, MoveGain, StunGain);
    }
}
