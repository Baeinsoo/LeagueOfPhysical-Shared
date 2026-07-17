using NUnit.Framework;
using GameFramework.World;

namespace LOP.Tests
{
    public class WorldEventWireTests
    {
        // 매퍼가 모르는 WorldEvent — null 반환 검증용(테스트 로컬 더미)
        private sealed record UnmappedEvent : WorldEvent;

        [Test]
        public void ToWire_Damage_MapsToOneofAndFields()
        {
            var wire = WorldEventWire.ToWire(new DamageDealtEvent("t1", "a1", 30, isCritical: true, isDodged: false));
            Assert.That(wire.EventCase, Is.EqualTo(WorldEventToC.EventOneofCase.Damage));
            Assert.That(wire.Damage.TargetId, Is.EqualTo("t1"));
            Assert.That(wire.Damage.AttackerId, Is.EqualTo("a1"));
            Assert.That(wire.Damage.Damage, Is.EqualTo(30));
            Assert.That(wire.Damage.IsCritical, Is.True);
            Assert.That(wire.Damage.IsDodged, Is.False);
        }

        [Test]
        public void ToWire_Ability_MapsToOneofAndFields()
        {
            var wire = WorldEventWire.ToWire(new AbilityActivatedEvent("e1", 42));
            Assert.That(wire.EventCase, Is.EqualTo(WorldEventToC.EventOneofCase.AbilityActivated));
            Assert.That(wire.AbilityActivated.EntityId, Is.EqualTo("e1"));
            Assert.That(wire.AbilityActivated.AbilityId, Is.EqualTo(42));
        }

        [Test]
        public void ToWire_UnmappedEvent_ReturnsNull()
        {
            Assert.That(WorldEventWire.ToWire(new UnmappedEvent()), Is.Null);
        }

        [Test]
        public void FromWire_Damage_RoundTrips()
        {
            var wire = WorldEventWire.ToWire(new DamageDealtEvent("t1", "a1", 30, true, false));
            var e = (DamageDealtEvent)WorldEventWire.FromWire(wire);
            Assert.That(e.targetId, Is.EqualTo("t1"));
            Assert.That(e.attackerId, Is.EqualTo("a1"));
            Assert.That(e.amount, Is.EqualTo(30));
            Assert.That(e.isCritical, Is.True);
            Assert.That(e.isDodged, Is.False);
        }

        [Test]
        public void FromWire_Ability_RoundTrips()
        {
            var wire = WorldEventWire.ToWire(new AbilityActivatedEvent("e1", 42));
            var e = (AbilityActivatedEvent)WorldEventWire.FromWire(wire);
            Assert.That(e.entityId, Is.EqualTo("e1"));
            Assert.That(e.abilityId, Is.EqualTo(42));
        }

        [Test]
        public void FromWire_EmptyOneof_ReturnsNull()
        {
            Assert.That(WorldEventWire.FromWire(new WorldEventToC()), Is.Null);
        }
    }
}
