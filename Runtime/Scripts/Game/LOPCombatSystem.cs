using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>전투 해소(공유 concrete) — 데미지/크리/회피 계산 + 이벤트 발행. 클·서 동일 코드(결정론).
    /// 씨앗은 caller가 사이드별로 전달(서버 MatchSeed, 클라 예측=보관 시드). 히트 판정은 caller(핸들러) 소관.</summary>
    public class LOPCombatSystem
    {
        private readonly GameFramework.World.WorldEventBuffer worldEventBuffer;
        private readonly GameFramework.World.HealthSystem healthSystem;
        private readonly GameFramework.World.StatsSystem statsSystem;

        public LOPCombatSystem(
            GameFramework.World.WorldEventBuffer worldEventBuffer,
            GameFramework.World.HealthSystem healthSystem,
            GameFramework.World.StatsSystem statsSystem)
        {
            this.worldEventBuffer = worldEventBuffer;
            this.healthSystem = healthSystem;
            this.statsSystem = statsSystem;
        }

        // 공격-대상 당 결정론 seed(effectIndex 없음). 닷지는 이걸, 크리는 여기에 effectIndex를 더한 seed를 쓴다.
        private static ulong AttackSeed(ulong matchSeed, long tick, string attackerId, string targetId)
            => GameFramework.Hashing.Combine(
                   GameFramework.Hashing.Combine(
                       GameFramework.Hashing.Combine(matchSeed, (ulong)tick),
                       GameFramework.Hashing.Fnv1a64(attackerId)),
                   GameFramework.Hashing.Fnv1a64(targetId));

        /// <summary>이 공격이 대상에게 회피당하는가(공격당 1회, effectIndex 무관). 결정론.</summary>
        public bool IsDodged(GameFramework.World.Entity attacker, GameFramework.World.Entity target, long tick, ulong matchSeed)
        {
            int attackerDex = DexOf(attacker);
            int targetDex = DexOf(target);
            var rng = new GameFramework.DeterministicRandom(AttackSeed(matchSeed, tick, attacker.Id, target.Id));
            float dodgeChance = (float)targetDex / (attackerDex + targetDex);
            dodgeChance = Mathf.Clamp(dodgeChance, 0.05f, 0.95f);
            return rng.Range(0.0f, 1.0f) < dodgeChance;
        }

        private int DexOf(GameFramework.World.Entity e)
        {
            var s = e?.Get<GameFramework.World.Stats>();
            return s != null ? Mathf.RoundToInt(statsSystem.GetValue(s, (int)GameFramework.World.EntityStatType.Dexterity)) : 0;
        }

        public void Attack(GameFramework.World.Entity attacker, GameFramework.World.Entity target,
                           int baseDamage, long tick, int effectIndex, ulong matchSeed, AttackHitContext hitContext)
        {
            bool attackerIsPlayer = attacker?.Has<GameFramework.World.Ownership>() == true;
            bool targetIsPlayer = target?.Has<GameFramework.World.Ownership>() == true;
            if (!attackerIsPlayer && !targetIsPlayer)
            {
                return;
            }

            GameFramework.World.Health health = target?.Get<GameFramework.World.Health>();
            if (health == null)
            {
                Debug.LogWarning($"[World] Attack: Health not found for entity {target?.Id}");
                return;
            }
            if (health.IsDead)
            {
                Debug.LogWarning($"Target {target.Id} is already dead.");
                return;
            }

            bool isDodged = IsDodged(attacker, target, tick, matchSeed);

            int attackerStrength = StrengthOf(attacker);
            int targetStrength = StrengthOf(target);
            int damage = (baseDamage + attackerStrength) * 3;   // 밸런스: 데미지 3배(크리 배수 적용 전)

            bool isCritical = false;
            if (!isDodged)
            {
                // 크리는 per-hit(effectIndex 포함 seed) — 닷지와 다른 스트림
                var critRng = new GameFramework.DeterministicRandom(
                    GameFramework.Hashing.Combine(AttackSeed(matchSeed, tick, attacker.Id, target.Id), (ulong)effectIndex));
                isCritical = IsCritical(attackerStrength, targetStrength, ref critRng);
                if (isCritical)
                {
                    damage = Mathf.RoundToInt(damage * critRng.Range(1.25f, 1.75f));
                }
                healthSystem.TakeDamage(health, damage);
                hitContext?.MarkLanded(target.Id);   // 명중 = on-hit 라이더 대상
            }

            int dealtAmount = isDodged ? 0 : damage;
            worldEventBuffer.Append(new GameFramework.World.DamageDealtEvent(
                targetId:   target.Id,
                attackerId: attacker.Id,
                amount:     dealtAmount,
                isCritical: isCritical,
                isDodged:   isDodged));

            if (health.IsDead)
            {
                worldEventBuffer.Append(new GameFramework.World.DeathEvent(
                    victimId:   target.Id,
                    attackerId: attacker.Id));
            }
        }

        private int StrengthOf(GameFramework.World.Entity e)
        {
            var s = e?.Get<GameFramework.World.Stats>();
            return s != null ? Mathf.RoundToInt(statsSystem.GetValue(s, (int)GameFramework.World.EntityStatType.Strength)) : 0;
        }

        public bool IsCritical(int attackerStr, int targetStr, ref GameFramework.DeterministicRandom rng)
        {
            float critChance = (float)attackerStr / (attackerStr + targetStr);
            critChance = Mathf.Clamp(critChance, 0.05f, 0.50f);
            double roll = rng.Range(0.0f, 1.0f);
            return roll < critChance;
        }
    }
}
