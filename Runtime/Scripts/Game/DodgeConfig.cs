using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 피하기 튜닝 값(마스터데이터 TbDodgeConfig 한 행). 클·서가 같은 값을 써야 그림과 판정이 같다.
    /// 시간은 초로 받고, 틱이 필요하면 <see cref="Ticks"/>로 바꾼다.
    /// </summary>
    public readonly struct DodgeConfig
    {
        public const int TicksPerSecond = 50;

        public readonly int Lives;
        public readonly float InvulnerableSeconds;
        public readonly float HitRadius;
        public readonly float LeadSeconds;
        public readonly float ArenaHalf;
        public readonly int TileCount;
        public readonly float FirstPatternDelaySeconds;
        public readonly float PatternIntervalSeconds;
        /// <summary>0이면 모든 종류를 돌아가며, 아니면 그 종류(<see cref="DodgePatternKind"/> 값)만 낸다. 한 종류씩 손맛을 볼 때.</summary>
        public readonly int OnlyKind;
        public readonly float WarnSeconds;
        public readonly float BulletSpeed;
        public readonly float BulletRadius;
        public readonly float BombRadius;
        public readonly float BombActiveSeconds;
        public readonly float LaserWidth;
        public readonly float LaserOnSeconds;
        public readonly float RockSpeed;
        public readonly float RockRadius;
        public readonly float TileOnSeconds;
        /// <summary>세기가 아무리 올라도 패턴 사이 간격은 이보다 짧지 않다.</summary>
        public readonly float MinIntervalSeconds;
        /// <summary>세기가 아무리 올라도 예고는 이보다 짧지 않다 — 예약 시간 + 반응 시간보다 길어야 피할 수 있다(스펙 §3.2).</summary>
        public readonly float MinWarnSeconds;
        public readonly float SuddenDeathBase;
        /// <summary>서든데스 1초마다 세기가 기본의 이만큼씩 오른다. 멈추지 않아 판이 반드시 끝난다.</summary>
        public readonly float SuddenDeathGrowth;

        public DodgeConfig(int lives, float invulnerableSeconds, float hitRadius, float leadSeconds, float arenaHalf,
                           int tileCount, float firstPatternDelaySeconds, float patternIntervalSeconds, int onlyKind,
                           float warnSeconds, float bulletSpeed, float bulletRadius, float bombRadius,
                           float bombActiveSeconds, float laserWidth, float laserOnSeconds, float rockSpeed,
                           float rockRadius, float tileOnSeconds,
                           float minIntervalSeconds = 0.5f, float minWarnSeconds = 0.8f,
                           float suddenDeathBase = 1.5f, float suddenDeathGrowth = 0.02f)
        {
            Lives = lives; InvulnerableSeconds = invulnerableSeconds; HitRadius = hitRadius; LeadSeconds = leadSeconds;
            ArenaHalf = arenaHalf; TileCount = tileCount; FirstPatternDelaySeconds = firstPatternDelaySeconds;
            PatternIntervalSeconds = patternIntervalSeconds; OnlyKind = onlyKind; WarnSeconds = warnSeconds;
            BulletSpeed = bulletSpeed; BulletRadius = bulletRadius; BombRadius = bombRadius;
            BombActiveSeconds = bombActiveSeconds; LaserWidth = laserWidth; LaserOnSeconds = laserOnSeconds;
            RockSpeed = rockSpeed; RockRadius = rockRadius; TileOnSeconds = tileOnSeconds;
            MinIntervalSeconds = minIntervalSeconds; MinWarnSeconds = minWarnSeconds;
            SuddenDeathBase = suddenDeathBase; SuddenDeathGrowth = suddenDeathGrowth;
        }

        /// <summary>초 → 틱. 0초를 0틱으로 두면 "켜졌다 바로 꺼져 아무도 못 보는" 위험이 생겨 최소 1틱이다.</summary>
        public static int Ticks(float seconds) => Mathf.Max(1, Mathf.RoundToInt(seconds * TicksPerSecond));

        public int InvulnerableTicks => Ticks(InvulnerableSeconds);
        public int LeadTicks => Ticks(LeadSeconds);
        public int FirstPatternDelayTicks => Ticks(FirstPatternDelaySeconds);
        public int PatternIntervalTicks => Ticks(PatternIntervalSeconds);
        public int WarnTicks => Ticks(WarnSeconds);
        public int BombActiveTicks => Ticks(BombActiveSeconds);
        public int LaserOnTicks => Ticks(LaserOnSeconds);
        public int TileOnTicks => Ticks(TileOnSeconds);
        public int MinIntervalTicks => Ticks(MinIntervalSeconds);
        public int MinWarnTicks => Ticks(MinWarnSeconds);

        /// <summary>탄·바위가 출발하는 선. 벽 안쪽보다 조금 바깥이라 벽 너머에서 날아 들어온다.</summary>
        public float EdgeDistance => ArenaHalf + 0.5f;
    }
}
