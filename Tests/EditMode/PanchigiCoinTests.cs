using System.Collections.Generic;
using GameFramework;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class PanchigiCoinTests
    {
        //  동전은 전부 같은 면(+up)으로 놓인다 — 그 전제에서 "윗면이 아래를 보면 뒤집힘"이다.
        [Test]
        public void 놓인_그대로면_안_뒤집힌_것이다()
        {
            Assert.IsFalse(PanchigiCoin.IsFlipped(System.Numerics.Quaternion.Identity));
        }

        [Test]
        public void 백팔십도_돌면_뒤집힌_것이다()
        {
            var rotation = Quaternion.Euler(180f, 0f, 0f).ToNumerics();

            Assert.IsTrue(PanchigiCoin.IsFlipped(rotation));
        }

        [Test]
        public void 모로_선_동전은_뒤집힌_것으로_치지_않는다()
        {
            var rotation = Quaternion.Euler(90f, 0f, 0f).ToNumerics();

            Assert.IsFalse(PanchigiCoin.IsFlipped(rotation));
        }

        [Test]
        public void 동전만_세고_다른_엔티티는_빼고_센다()
        {
            var entities = new List<Entity>
            {
                Coin(Quaternion.Euler(180f, 0f, 0f)),
                Coin(Quaternion.identity),
                Coin(Quaternion.Euler(0f, 30f, 180f)),
                Player(),
            };

            PanchigiCoin.CountFlipped(entities, out int flipped, out int total);

            Assert.AreEqual(3, total);
            Assert.AreEqual(2, flipped);
        }

        [Test]
        public void 동전이_하나도_없으면_영이다()
        {
            PanchigiCoin.CountFlipped(new List<Entity>(), out int flipped, out int total);

            Assert.AreEqual(0, total);
            Assert.AreEqual(0, flipped);
        }

        private static Entity Coin(Quaternion rotation)
        {
            var entity = new Entity(System.Guid.NewGuid().ToString());
            entity.Add(new EntityKind(EntityType.Coin));
            entity.Add(new GameFramework.World.Transform { Rotation = rotation.ToNumerics() });
            return entity;
        }

        private static Entity Player()
        {
            var entity = new Entity(System.Guid.NewGuid().ToString());
            entity.Add(new EntityKind(EntityType.Character));
            entity.Add(new GameFramework.World.Transform());
            return entity;
        }
    }
}
