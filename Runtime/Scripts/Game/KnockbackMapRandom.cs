using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 판마다 달라지는 맵 안의 무작위 - 기둥 고리의 회전각과 드럼통 시작 자리.
    /// 매치 씨앗만 넣으면 답이 나오는 순수 식이라, 같은 씨앗을 아는 클·서가 각자 계산해도 같은 값이다.
    /// 각은 KnockbackPillars·KnockbackSpawn과 같게 +z가 0도, x=sin / z=cos 이다.
    /// </summary>
    public static class KnockbackMapRandom
    {
        public static readonly ulong PillarSalt = GameFramework.Rng.Hashing.Fnv1a64("knockback-pillar-ring");
        public static readonly ulong BarrelSalt = GameFramework.Rng.Hashing.Fnv1a64("knockback-barrel-spawn");

        //  드럼통이 자기 몫의 각에서 흔들리는 폭(±도).
        private const float BarrelJitterDegrees = 20f;
        //  기둥에 걸리면 이만큼씩 돌려 가며 빈 자리를 찾는다.
        private const float NudgeStepDegrees = 2f;
        //  기둥 가장자리와 드럼통 가장자리 사이에 띄울 여유(m).
        private const float PillarMargin = 0.3f;

        /// <summary>기둥 고리를 돌릴 각(도). 기둥 한 칸 간격(360/개수) 안의 값이라 더 돌려도 같은 모양이 된다.</summary>
        public static float PillarRotationDegrees(ulong matchSeed, int pillarCount)
        {
            if (pillarCount <= 0) return 0f;
            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(matchSeed, PillarSalt));
            return rng.Range(0f, 360f / pillarCount);
        }

        /// <summary>
        /// 드럼통 시작 자리(x, z). 고리를 개수로 나눈 각 둘레에서 씨앗으로 흔들고,
        /// 기둥 둘레(기둥 반지름 + 드럼통 반지름 + 여유)와 겹치면 빈 자리가 나올 때까지 조금씩 돌린다.
        /// </summary>
        public static Vector2[] BarrelSpawns(ulong matchSeed, KnockbackBarrelLayout barrels,
                                             IReadOnlyList<Vector2> pillarCenters, float pillarRadius)
        {
            int n = barrels.Count;
            var spawns = new Vector2[n];
            float clearance = pillarRadius + barrels.Radius + PillarMargin;
            int maxNudges = Mathf.CeilToInt(360f / NudgeStepDegrees);
            for (int i = 0; i < n; i++)
            {
                //  통마다 따로 뽑아 다른 통의 개수·위치가 바뀌어도 이 통은 그대로다.
                var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(
                    GameFramework.Rng.Hashing.Combine(matchSeed, BarrelSalt), (ulong)i));
                float angle = i * 360f / n + rng.Range(-BarrelJitterDegrees, BarrelJitterDegrees);
                for (int step = 0; step < maxNudges; step++)
                {
                    if (!Overlaps(OnRing(angle, barrels.RingRadius), pillarCenters, clearance)) break;
                    angle += NudgeStepDegrees;
                }
                spawns[i] = OnRing(angle, barrels.RingRadius);
            }
            return spawns;
        }

        private static Vector2 OnRing(float degrees, float radius)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * radius;
        }

        private static bool Overlaps(Vector2 p, IReadOnlyList<Vector2> pillars, float clearance)
        {
            for (int i = 0; i < pillars.Count; i++)
            {
                if ((p - pillars[i]).sqrMagnitude < clearance * clearance) return true;
            }
            return false;
        }
    }
}
