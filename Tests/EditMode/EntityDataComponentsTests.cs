using System;
using GameFramework.World;
using NUnit.Framework;

namespace LOP.Tests
{
    public class EntityDataComponentsTests
    {
        [Test]
        public void Appearance_StoresVisualId()
        {
            Assert.AreEqual("Assets/Art/x.prefab", new Appearance("Assets/Art/x.prefab").VisualId);
        }

        [Test]
        public void Appearance_NullVisualId_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new Appearance(null));
        }

        [Test]
        public void MasterDataRef_StoresCode()
        {
            Assert.AreEqual("knight", new MasterDataRef("knight").Code);
        }

        [Test]
        public void MasterDataRef_NullCode_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new MasterDataRef(null));
        }

        [Test]
        public void EntityKind_StoresKind()
        {
            Assert.AreEqual(EntityType.Character, new EntityKind(EntityType.Character).Kind);
        }

        [Test]
        public void Entity_AddGet_RoundTrips()
        {
            var e = new Entity("e1");
            e.Add(new Appearance("v"));
            e.Add(new MasterDataRef("c"));
            e.Add(new EntityKind(EntityType.Item));
            Assert.AreEqual("v", e.Get<Appearance>().VisualId);
            Assert.AreEqual("c", e.Get<MasterDataRef>().Code);
            Assert.AreEqual(EntityType.Item, e.Get<EntityKind>().Kind);
        }
    }
}
