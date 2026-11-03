using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 판마다 달라지는 맵 안의 무작위 - 기둥 고리의 회전각.
    /// 매치 씨앗만 넣으면 답이 나오는 순수 식이라, 같은 씨앗을 아는 클·서가 각자 계산해도 같은 값이다.
    /// 각은 KnockbackPillars·KnockbackSpawn과 같게 +z가 0도, x=sin / z=cos 이다.
    /// </summary>
    public static class KnockbackMapRandom
    {
        public static readonly ulong PillarSalt = GameFramework.Rng.Hashing.Fnv1a64("knockback-pillar-ring");

        /// <summary>기둥 고리를 돌릴 각(도). 기둥 한 칸 간격(360/개수) 안의 값이라 더 돌려도 같은 모양이 된다.</summary>
        public static float PillarRotationDegrees(ulong matchSeed, int pillarCount)
        {
            if (pillarCount <= 0) return 0f;
            var rng = new GameFramework.Rng.DeterministicRandom(GameFramework.Rng.Hashing.Combine(matchSeed, PillarSalt));
            return rng.Range(0f, 360f / pillarCount);
        }
    }
}
