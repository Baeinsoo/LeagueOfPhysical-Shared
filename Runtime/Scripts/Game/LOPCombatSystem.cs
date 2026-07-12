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

        public void Attack(GameFramework.World.Entity attacker, GameFramework.World.Entity target,
                           long tick, int effectIndex, ulong matchSeed)
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

            int damage = 10;
            GameFramework.World.Stats attackerStats = attacker?.Get<GameFramework.World.Stats>();
            GameFramework.World.Stats targetStats = target?.Get<GameFramework.World.Stats>();

            int attackerStrength = attackerStats != null ? Mathf.RoundToInt(statsSystem.GetValue(attackerStats, (int)GameFramework.World.EntityStatType.Strength)) : 0;
            int attackerDexterity = attackerStats != null ? Mathf.RoundToInt(statsSystem.GetValue(attackerStats, (int)GameFramework.World.EntityStatType.Dexterity)) : 0;
            int targetStrength = targetStats != null ? Mathf.RoundToInt(statsSystem.GetValue(targetStats, (int)GameFramework.World.EntityStatType.Strength)) : 0;
            int targetDexterity = targetStats != null ? Mathf.RoundToInt(statsSystem.GetValue(targetStats, (int)GameFramework.World.EntityStatType.Dexterity)) : 0;

            damage += attackerStrength;

            ulong seed = GameFramework.Hashing.Combine(
                GameFramework.Hashing.Combine(
                    GameFramework.Hashing.Combine(
                        GameFramework.Hashing.Combine(matchSeed, (ulong)tick),
                        GameFramework.Hashing.Fnv1a64(attacker.Id)),
                    GameFramework.Hashing.Fnv1a64(target.Id)),
                (ulong)effectIndex);
            var rng = new GameFramework.DeterministicRandom(seed);

            bool isDodged   = IsDodge(attackerDexterity, targetDexterity, ref rng);
            bool isCritical = IsCritical(attackerStrength, targetStrength, ref rng);
            if (isCritical)
            {
                damage = Mathf.RoundToInt(damage * rng.Range(1.25f, 1.75f));
            }

            int dealtAmount = isDodged ? 0 : damage;
            if (!isDodged)
            {
                healthSystem.TakeDamage(health, dealtAmount);
            }
            bool isDead = health.IsDead;

            worldEventBuffer.Append(new GameFramework.World.DamageDealtEvent(
                targetId:   target.Id,
                attackerId: attacker.Id,
                amount:     dealtAmount,
                isCritical: isCritical,
                isDodged:   isDodged));

            if (isDead)
            {
                worldEventBuffer.Append(new GameFramework.World.DeathEvent(
                    victimId:   target.Id,
                    attackerId: attacker.Id));
            }
        }

        public bool IsDodge(int attackerDex, int targetDex, ref GameFramework.DeterministicRandom rng)
        {
            float dodgeChance = (float)targetDex / (attackerDex + targetDex);
            dodgeChance = Mathf.Clamp(dodgeChance, 0.05f, 0.95f);
            double roll = rng.Range(0.0f, 1.0f);
            return roll < dodgeChance;
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
