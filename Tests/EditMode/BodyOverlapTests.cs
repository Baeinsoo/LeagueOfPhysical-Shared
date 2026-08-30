using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class BodyOverlapTests
    {
        const float Tolerance = 1e-4f;

        // TbFlappyConfig 기본 몸 규격 — 높이가 반지름의 2배라 캡슐이 구가 된다
        const float Radius = 0.45f;
        const float Height = 0.9f;

        [Test]
        public void 떨어져_있으면_안_겹친다()
        {
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(5f, 0f, 0f), Radius, Height, out _, out _);

            Assert.IsFalse(overlapped);
        }

        [Test]
        public void 위아래로_겹치면_아래_새를_아래로_민다()
        {
            // a가 아래, b가 위 — 중심 간격 0.5, 서로 닿는 거리 0.9
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(0f, 0.5f, 0f), Radius, Height,
                out Vector3 pushDir, out float depth);

            Assert.IsTrue(overlapped);
            Assert.AreEqual(0.4f, depth, Tolerance);          // 0.9 - 0.5
            Assert.AreEqual(Vector3.down, pushDir);
        }

        [Test]
        public void 옆으로_겹치면_옆으로_민다()
        {
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(0.5f, 0f, 0f), Radius, Height,
                out Vector3 pushDir, out float depth);

            Assert.IsTrue(overlapped);
            Assert.AreEqual(0.4f, depth, Tolerance);
            Assert.AreEqual(Vector3.left, pushDir);           // a는 b 반대쪽(-X)으로
        }

        [Test]
        public void 키가_있는_몸은_높이가_겹치는_동안만_옆거리로_판정한다()
        {
            // 길쭉한 캡슐(반지름 0.4, 높이 2.0): 심이 y+0.4 ~ y+1.6
            // 두 새가 옆으로 0.5 떨어져 있고 세로로 0.6 어긋나 있어도 심 높이가 겹쳐 옆거리(0.5)로만 잰다
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(0.5f, 0.6f, 0f), 0.4f, 2.0f,
                out Vector3 pushDir, out float depth);

            Assert.IsTrue(overlapped);
            Assert.AreEqual(0.3f, depth, Tolerance);          // 0.8 - 0.5
            Assert.AreEqual(Vector3.left, pushDir);           // 세로 성분이 없다 = 세로 속도를 안 뺏는다
        }

        [Test]
        public void 심_높이가_안_겹치면_그_간격까지_거리에_넣는다()
        {
            // 같은 길쭉한 캡슐을 세로로 3.0 띄우면 심 간격이 3.4-1.6=1.8 > 0.8이라 안 닿는다
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, new Vector3(0f, 3.0f, 0f), 0.4f, 2.0f, out _, out _);

            Assert.IsFalse(overlapped);
        }

        [Test]
        public void 완전히_같은_자리면_정해진_방향으로_가른다()
        {
            // 방향을 구할 수 없는 자리 — 아무 방향이나 고르면 클·서가 갈리므로 규칙을 박아 둔다
            bool overlapped = BodyOverlap.TryCompute(
                Vector3.zero, Vector3.zero, Radius, Height,
                out Vector3 pushDir, out float depth);

            Assert.IsTrue(overlapped);
            Assert.AreEqual(0.9f, depth, Tolerance);
            Assert.AreEqual(Vector3.down, pushDir);
        }
    }
}
