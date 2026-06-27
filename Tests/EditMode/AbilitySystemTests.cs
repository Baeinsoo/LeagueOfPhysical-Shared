using GameFramework.World;
using LOP;
using NUnit.Framework;

namespace LOP.Tests
{
    public class AbilitySystemTests
    {
        const float Tolerance = 1e-4f;
        const int AbilityId = 1;
        const int MpCost = 20;
        const int Cooldown = 10;

        private ManaSystem _mana;
        private StatsSystem _stats;
        private StatusEffectSystem _statusEffects;
        private AbilitySystem _system;

        [SetUp]
        public void SetUp()
        {
            _mana = new ManaSystem();
            _stats = new StatsSystem();
            _statusEffects = new StatusEffectSystem(_stats);
            _system = new AbilitySystem(_mana, _statusEffects);
        }

        private Entity MakeEntity(int manaMax, float dexBase)
        {
            var entity = new Entity("caster");
            entity.Add(new Abilities());
            entity.Add(new Mana(manaMax));
            var stats = new Stats();
            stats.BaseStats[(int)EntityStatType.Dexterity] = dexBase;
            entity.Add(stats);
            entity.Add(new StatusEffects());
            return entity;
        }

        // 기본 = 즉발 등가(startup0/active1/recovery0). 프레임 값은 인자로 변경.
        private static AbilityData Ability(long startup = 0, long active = 1, long recovery = 0)
            => new AbilityData(AbilityId, Cooldown, MpCost, startup, active, recovery,
                               TargetingMode.Self, 0f, new[] { 100 });

        private static StatusEffectData[] HasteEffects()
            => new[]
            {
                new StatusEffectData(
                    100, DurationPolicy.Duration, 5,
                    new[] { new StatusModifierSpec((int)EntityStatType.Dexterity, 0.3f, ModifierType.PercentAdd) },
                    StatusStackPolicy.Refresh, 1)
            };

        private int Mana(Entity e) => e.Get<Mana>().Current;
        private long CooldownEnd(Entity e) => e.Get<Abilities>().Slots[AbilityId].CooldownEndTick;
        private int EffectCount(Entity e) => e.Get<StatusEffects>().Effects.Count;
        private float Dex(Entity e) => _stats.GetValue(e.Get<Stats>(), (int)EntityStatType.Dexterity);
        private ActiveAbility? Active(Entity e) => e.Get<Abilities>().ActiveAbility;

        [Test]
        public void Grant_AddsReadySlot()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.True);
        }

        [Test]
        public void TryActivate_Commits_StartsMachine_NoImmediateEffect()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost), "Commit: 마나 차감");
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown), "Commit: 쿨다운 설정 (0 + 10)");
            Assert.That(EffectCount(e), Is.EqualTo(0), "효과는 즉시 적용 안 함 (Active 이연)");
            Assert.That(Active(e), Is.Not.Null);
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Startup));
        }

        [Test]
        public void Tick_StartupToActive_AppliesEffect()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            _system.Tick(e, 0);   // startup0 종료 → Active 진입 → 효과 적용

            Assert.That(EffectCount(e), Is.EqualTo(1));
            Assert.That(Dex(e), Is.EqualTo(13f).Within(Tolerance)); // 10 * 1.3
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Active));
        }

        [Test]
        public void Tick_RunsThroughRecoveryToReady()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);  // 0/1/0

            _system.Tick(e, 0);   // Startup -> Active
            _system.Tick(e, 1);   // Active -> Recovery
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Recovery));

            _system.Tick(e, 2);   // Recovery -> Ready
            Assert.That(Active(e), Is.Null, "Recovery 종료 후 Ready");
        }

        [Test]
        public void Busy_BlocksReactivation()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);   // 진행 중

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.False, "busy");

            bool again = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);
            Assert.That(again, Is.False);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost), "busy 거절 시 추가 차감 없음");
        }

        [Test]
        public void Cooldown_AfterMachineDone_BlocksUntilElapsed()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);   // cd end @10
            _system.Tick(e, 0); _system.Tick(e, 1); _system.Tick(e, 2); // 머신 종료 -> Ready

            Assert.That(Active(e), Is.Null);
            Assert.That(_system.CanActivate(e, Ability(), 3), Is.False, "not busy지만 쿨다운(3<10)");
            Assert.That(_system.CanActivate(e, Ability(), Cooldown), Is.True, "쿨다운 경과");
        }

        [Test]
        public void TryActivate_AfterCooldown_Succeeds()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, HasteEffects(), 0);   // cd end @10
            _system.Tick(e, 0); _system.Tick(e, 1); _system.Tick(e, 2); // -> Ready

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), Cooldown);

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost * 2));
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown * 2)); // 10 + 10
        }

        [Test]
        public void CanActivate_InsufficientMana_False()
        {
            var e = MakeEntity(10, 10f); // max 10 < MpCost 20
            _system.Grant(e, AbilityId);

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.False);

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);
            Assert.That(ok, Is.False);
            Assert.That(Mana(e), Is.EqualTo(10), "부수효과 없음");
            Assert.That(Active(e), Is.Null);
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NotGranted_False()
        {
            var e = MakeEntity(100, 10f); // Grant 안 함

            bool ok = _system.TryActivate(e, Ability(), e, HasteEffects(), 0);

            Assert.That(ok, Is.False);
            Assert.That(Active(e), Is.Null);
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NullProducedEffects_CommitsNoEffect()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            bool ok = _system.TryActivate(e, Ability(), e, null, 0);
            _system.Tick(e, 0);   // Active 진입 — null 효과는 no-op

            Assert.That(ok, Is.True);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost));
            Assert.That(CooldownEnd(e), Is.EqualTo(Cooldown));
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void Startup_DelaysEffectUntilActive()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(startup: 3, active: 1, recovery: 0), e, HasteEffects(), 0);

            _system.Tick(e, 0); _system.Tick(e, 1); _system.Tick(e, 2);  // 0,1,2 < startupEnd(3)
            Assert.That(EffectCount(e), Is.EqualTo(0), "startup 동안 효과 미적용");
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Startup));

            _system.Tick(e, 3);   // 3 >= startupEnd(3) -> Active -> 적용
            Assert.That(EffectCount(e), Is.EqualTo(1));
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Active));
        }
    }
}
