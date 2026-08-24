using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class CollisionHitExtensionsTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        private GameObject MakeBody(string entityId)
        {
            var go = new GameObject("body");
            go.AddComponent<SphereCollider>();
            go.AddComponent<EntityActor>().SetEntityId(entityId);
            spawned.Add(go);
            return go;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned)
            {
                Object.DestroyImmediate(go);
            }
            spawned.Clear();
        }

        private static GameFramework.Physics.CollisionHit HitOn(Collider collider)
            => new GameFramework.Physics.CollisionHit(true, 0f, Vector3.zero, Vector3.zero, collider);

        [Test]
        public void 콜라이더에_붙은_엔티티_id를_돌려준다()
        {
            GameObject body = MakeBody("entity-7");

            string id = HitOn(body.GetComponent<Collider>()).GetEntityId();

            Assert.AreEqual("entity-7", id);
        }

        [Test]
        public void 자식_콜라이더면_부모에서_찾는다()
        {
            GameObject body = MakeBody("entity-7");
            var child = new GameObject("visual");
            child.transform.SetParent(body.transform);
            var childCollider = child.AddComponent<BoxCollider>();

            string id = HitOn(childCollider).GetEntityId();

            Assert.AreEqual("entity-7", id);
        }

        [Test]
        public void 엔티티가_아닌_것을_맞으면_null()
        {
            var plain = new GameObject("board");
            plain.AddComponent<BoxCollider>();
            spawned.Add(plain);

            string id = HitOn(plain.GetComponent<Collider>()).GetEntityId();

            Assert.IsNull(id);
        }

        [Test]
        public void 아무것도_안_맞았으면_null()
        {
            Assert.IsNull(GameFramework.Physics.CollisionHit.None.GetEntityId());
        }
    }
}
