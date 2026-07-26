using NUnit.Framework;
using GameFramework.World;

namespace LOP.Tests
{
    public class AbilitySlotResolutionTests
    {
        private static Entity MakeCaster()
        {
            var entity = new Entity("caster");
            entity.Add(new Abilities());
            return entity;
        }

        private static AbilitySystem MakeSystem() => new AbilitySystem(new ManaSystem());

        [Test]
        public void GrantStoresSlot()
        {
            var entity = MakeCaster();
            MakeSystem().Grant(entity, abilityId: 3, slot: 1);

            Assert.AreEqual(1, entity.Get<Abilities>().Granted[3].Slot);
        }

        [Test]
        public void ResolvesAbilityIdFromSlot()
        {
            var entity = MakeCaster();
            var system = MakeSystem();
            system.Grant(entity, abilityId: 3, slot: 1);
            system.Grant(entity, abilityId: 2, slot: 2);

            Assert.IsTrue(system.TryGetAbilityIdBySlot(entity, 2, out int abilityId));
            Assert.AreEqual(2, abilityId);
        }

        [Test]
        public void UnboundSlotResolvesToFalse()
        {
            var entity = MakeCaster();
            var system = MakeSystem();
            system.Grant(entity, abilityId: 3, slot: 1);

            Assert.IsFalse(system.TryGetAbilityIdBySlot(entity, 4, out _));
        }

        [Test]
        public void SlotZeroIsNotResolvable()
        {
            var entity = MakeCaster();
            var system = MakeSystem();
            system.Grant(entity, abilityId: 7, slot: 0);   // 입력에 붙지 않는 부여

            Assert.IsFalse(system.TryGetAbilityIdBySlot(entity, 0, out _));
        }

        [Test]
        public void RegrantUpdatesSlot()
        {
            var entity = MakeCaster();
            var system = MakeSystem();
            system.Grant(entity, abilityId: 3, slot: 1);
            system.Grant(entity, abilityId: 3, slot: 4);

            Assert.AreEqual(4, entity.Get<Abilities>().Granted[3].Slot);
        }

        [Test]
        public void ActivationPreservesSlot()
        {
            var entity = MakeCaster();
            entity.Add(new Mana(100));
            var system = MakeSystem();
            system.Grant(entity, abilityId: 3, slot: 1);

            var data = new AbilityData(3, cooldownTicks: 10, mpCost: 0,
                startupTicks: 1, activeTicks: 1, recoveryTicks: 1,
                effects: System.Array.Empty<AbilityEffect>());

            Assert.IsTrue(system.TryActivate(entity, data, entity, currentTick: 100));
            Assert.AreEqual(1, entity.Get<Abilities>().Granted[3].Slot,
                "발동 시 쿨다운만 갱신되어야 하고 슬롯은 보존되어야 한다");
        }
    }
}

