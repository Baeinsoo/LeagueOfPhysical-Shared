using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
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

        [Test]
        public void 적중_사건은_와이어를_왕복해도_그대로다()
        {
            var original = new ArcheryTargetHitEvent("e7", 1234L, 4);
            var restored = (ArcheryTargetHitEvent)WorldEventWire.FromWire(WorldEventWire.ToWire(original));

            Assert.AreEqual(original.shooterId, restored.shooterId);
            Assert.AreEqual(original.fireTick,  restored.fireTick);
            Assert.AreEqual(original.points,    restored.points);
        }

        [Test]
        public void 라운드_결과는_와이어를_왕복해도_같다()
        {
            var placements = new List<ArcheryRoundPlacement>
            {
                new ArcheryRoundPlacement("e1", true, new Vector2(0.03f, -0.01f), 0.0316f, 0, 6),
                new ArcheryRoundPlacement("e2", false, Vector2.zero, 0f, 1, 0),
            };
            var wire = WorldEventWire.ToWire(new ArcheryRoundResultEvent(11, 2, placements));
            var back = (ArcheryRoundResultEvent)WorldEventWire.FromWire(wire);

            Assert.AreEqual(11, back.roundIndex);
            Assert.AreEqual(2, back.multiplier);
            Assert.AreEqual(2, back.placements.Count);
            Assert.AreEqual("e1", back.placements[0].ShooterId);
            Assert.IsTrue(back.placements[0].Hit);
            Assert.AreEqual(0.03f, back.placements[0].FaceOffset.x, 1e-6f);
            Assert.AreEqual(-0.01f, back.placements[0].FaceOffset.y, 1e-6f);
            Assert.AreEqual(0.0316f, back.placements[0].Distance, 1e-6f);
            Assert.AreEqual(6, back.placements[0].Points);
            Assert.IsFalse(back.placements[1].Hit);
            Assert.AreEqual(1, back.placements[1].Rank);
        }
    }
}
