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

        public KnockbackConfig(float matchDurationSeconds, float moveSpeed, float moveGain, float arenaRadius,
                               float tileSize, float holeRadius, float fallDepth, float spawnRadius)
        {
            MatchDurationSeconds = matchDurationSeconds;
            MoveSpeed = moveSpeed;
            MoveGain = moveGain;
            ArenaRadius = arenaRadius;
            TileSize = tileSize;
            HoleRadius = holeRadius;
            FallDepth = fallDepth;
            SpawnRadius = spawnRadius;
        }

        public long MatchDurationTicks(float tickInterval)
            => (long)System.Math.Round(MatchDurationSeconds / tickInterval);

        public KnockbackFloor CreateFloor() => new KnockbackFloor(ArenaRadius, TileSize, HoleRadius);

        //  바닥은 월드와 이동 시스템이 같은 한 개를 나눠 쓴다(칸이 무너지면 둘이 같은 상태를 봐야 한다).
        public KnockbackMoveSystem CreateMoveSystem(KnockbackFloor floor, GameFramework.Physics.ICollisionQuery query, int layerMask)
            => new KnockbackMoveSystem(floor, query, layerMask, MoveSpeed, MoveGain);
    }
}
