using System.Collections.Generic;
using GameFramework.Physics;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    // 홀로그램 통과 자체는 FlappyWorld.MoveBlockedByMap의 배선을 본다 — 판정은 콜라이더가 그대로
    // 하니 여기서는 "대시 중엔 그 층이 sweep 마스크에서 빠지는가"만 마스크 기록으로 확인한다.
    public class FlappyWorldHologramTests
    {
        /// <summary>히트 없이 <see cref="CapsuleCast"/>가 받은 layerMask만 쌓아 두는 기록기.</summary>
        private class MaskRecorder : ICollisionQuery
        {
            public readonly List<int> Masks = new List<int>();

            public CollisionHit CapsuleCast(Vector3 p1, Vector3 p2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                Masks.Add(layerMask);
                return CollisionHit.None;
            }

            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => CollisionHit.None;

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => System.Array.Empty<CollisionHit>();
        }

        [Test]
        public void 대시_중에는_홀로그램_층을_마스크에서_뺀다()
        {
            int hologram = UnityEngine.LayerMask.NameToLayer(FlappyHologram.LayerName);
            Assume.That(hologram, Is.GreaterThanOrEqualTo(0), "TagManager에 Hologram 층이 있어야 잰다");
            var recorder = new MaskRecorder();
            int full = UnityEngine.LayerMask.GetMask("Default", FlappyHologram.LayerName);
            var world = FlappyWorldFixture.Create(recorder, out var bird, full);
            bird.Get<FlappyDash>().DashRemaining = 0.4f;

            world.Tick(FlappyWorldFixture.StartTick, 0.02f);

            Assert.That(recorder.Masks, Is.Not.Empty);
            Assert.That(recorder.Masks.TrueForAll(m => (m & (1 << hologram)) == 0), "대시 중 sweep에 홀로그램이 섞였다");
        }

        [Test]
        public void 대시가_아니면_홀로그램도_벽이다()
        {
            int hologram = UnityEngine.LayerMask.NameToLayer(FlappyHologram.LayerName);
            Assume.That(hologram, Is.GreaterThanOrEqualTo(0));
            var recorder = new MaskRecorder();
            int full = UnityEngine.LayerMask.GetMask("Default", FlappyHologram.LayerName);
            var world = FlappyWorldFixture.Create(recorder, out var bird, full);

            world.Tick(FlappyWorldFixture.StartTick, 0.02f);

            Assert.That(recorder.Masks.TrueForAll(m => (m & (1 << hologram)) != 0));
        }
    }
}
