using System;
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
        const int HasteEffectId = 100;

        private ManaSystem _mana;
        private StatsSystem _stats;
        private StatusEffectSystem _statusEffects;
        private AbilityEffectExecutor _executor;
        private AbilitySystem _system;

        [SetUp]
        public void SetUp()
        {
            _mana = new ManaSystem();
            _stats = new StatsSystem();
            _statusEffects = new StatusEffectSystem(_stats);
            // 상태효과 적용 핸들러(코어) — resolver 심으로 효과 id를 설정으로 푼다(MasterData 대신 테스트 데이터).
            var statusHandler = new StatusEffectApplyEffectHandler(_statusEffects, Resolve);
            _executor = new AbilityEffectExecutor(new IAbilityEffectHandler[] { statusHandler });
            _system = new AbilitySystem(_mana);
        }

        // 런타임 1틱 모사: world.Tick(페이즈 전진) → host가 executor로 effect 구동. entityManager는 헤이스트에 불필요(null).
        private void Advance(Entity e, long tick)
        {
            _system.Tick(e, tick);
            _executor.DriveActiveEntity(e, tick);
        }

        private static StatusEffectData? Resolve(int effectId)
            => effectId == HasteEffectId ? (StatusEffectData?)HasteData() : null;

        private static StatusEffectData HasteData()
            => new StatusEffectData(
                HasteEffectId, DurationPolicy.Duration, 5,
                new[] { new StatusModifierSpec((int)EntityStatType.Dexterity, 0.3f, ModifierType.PercentAdd) },
                StatusStackPolicy.Refresh, 1);

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

        // 기본 = 즉발 등가(startup0/active1/recovery0) + 헤이스트 상태효과 1개. 프레임 값은 인자로 변경.
        private static AbilityData Ability(long startup = 0, long active = 1, long recovery = 0)
            => new AbilityData(AbilityId, Cooldown, MpCost, startup, active, recovery,
                               new AbilityEffect[] { new StatusEffectApplyEffect(HasteEffectId) });

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

            bool ok = _system.TryActivate(e, Ability(), e, 0);

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
            _system.TryActivate(e, Ability(), e, 0);

            Advance(e,0);   // startup0 종료 → Active 진입 → executor가 상태효과 적용

            Assert.That(EffectCount(e), Is.EqualTo(1));
            Assert.That(Dex(e), Is.EqualTo(13f).Within(Tolerance)); // 10 * 1.3
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Active));
        }

        [Test]
        public void Tick_RunsThroughRecoveryToReady()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, 0);  // 0/1/0

            Advance(e,0);   // Startup -> Active
            Advance(e,1);   // Active -> Recovery
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Recovery));

            Advance(e,2);   // Recovery -> Ready
            Assert.That(Active(e), Is.Null, "Recovery 종료 후 Ready");
        }

        [Test]
        public void Busy_BlocksReactivation()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, 0);   // 진행 중

            Assert.That(_system.CanActivate(e, Ability(), 0), Is.False, "busy");

            bool again = _system.TryActivate(e, Ability(), e, 0);
            Assert.That(again, Is.False);
            Assert.That(Mana(e), Is.EqualTo(100 - MpCost), "busy 거절 시 추가 차감 없음");
        }

        [Test]
        public void Cooldown_AfterMachineDone_BlocksUntilElapsed()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, 0);   // cd end @10
            Advance(e,0); Advance(e,1); Advance(e,2); // 머신 종료 -> Ready

            Assert.That(Active(e), Is.Null);
            Assert.That(_system.CanActivate(e, Ability(), 3), Is.False, "not busy지만 쿨다운(3<10)");
            Assert.That(_system.CanActivate(e, Ability(), Cooldown), Is.True, "쿨다운 경과");
        }

        [Test]
        public void TryActivate_AfterCooldown_Succeeds()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);
            _system.TryActivate(e, Ability(), e, 0);   // cd end @10
            Advance(e,0); Advance(e,1); Advance(e,2); // -> Ready

            bool ok = _system.TryActivate(e, Ability(), e, Cooldown);

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

            bool ok = _system.TryActivate(e, Ability(), e, 0);
            Assert.That(ok, Is.False);
            Assert.That(Mana(e), Is.EqualTo(10), "부수효과 없음");
            Assert.That(Active(e), Is.Null);
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NotGranted_False()
        {
            var e = MakeEntity(100, 10f); // Grant 안 함

            bool ok = _system.TryActivate(e, Ability(), e, 0);

            Assert.That(ok, Is.False);
            Assert.That(Active(e), Is.Null);
            Assert.That(EffectCount(e), Is.EqualTo(0));
        }

        [Test]
        public void TryActivate_NoEffects_CommitsNoEffect()
        {
            var e = MakeEntity(100, 10f);
            _system.Grant(e, AbilityId);

            // effects 없는 어빌리티(빈 리스트) — 발동·쿨다운만, 효과 없음.
            var noEffectAbility = new AbilityData(AbilityId, Cooldown, MpCost, 0, 1, 0, new AbilityEffect[0]);
            bool ok = _system.TryActivate(e, noEffectAbility, e, 0);
            Advance(e,0);   // Active 진입 — 빈 리스트는 no-op

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
            _system.TryActivate(e, Ability(startup: 3, active: 1, recovery: 0), e, 0);

            Advance(e,0); Advance(e,1); Advance(e,2);  // 0,1,2 < startupEnd(3)
            Assert.That(EffectCount(e), Is.EqualTo(0), "startup 동안 효과 미적용");
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Startup));

            Advance(e,3);   // 3 >= startupEnd(3) -> Active -> 적용
            Assert.That(EffectCount(e), Is.EqualTo(1));
            Assert.That(Active(e).Value.Phase, Is.EqualTo(AbilityPhase.Active));
        }
    }
}
