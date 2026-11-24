using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>태풍의 눈 잔해 판정(시제품) — 원래 관문 모양(틈·폭·벽 안)을 지키면서 통나무·거목·판자로 짓는다.</summary>
    public class StormDebrisGeometryTests
    {
        private const float Radius = 59f;

        private static IEnumerable<Vector3> TopCorners(StormDebrisPiece p)
        {
            Quaternion q = p.Rotation;
            for (int sx = -1; sx <= 1; sx += 2)
                for (int sz = -1; sz <= 1; sz += 2)
                    yield return p.Center + q * new Vector3(sx * p.Size.x * 0.5f, 0f, sz * p.Size.z * 0.5f);
        }

        private static bool Covered(List<StormDebrisPiece> pieces, Vector3 point)
        {
            foreach (var p in pieces)
            {
                Vector3 local = Quaternion.Inverse(p.Rotation) * (point - p.Center);
                if (Mathf.Abs(local.x) <= p.Size.x * 0.5f && Mathf.Abs(local.z) <= p.Size.z * 0.5f) return true;
            }
            return false;
        }

        [Test]
        public void 뗏목은_틈을_덮지_않고_틈_밖은_대부분_덮는다()
        {
            var logs = StormDebrisGeometry.Raft(Radius, 90f, 1234);
            Assert.Greater(logs.Count, 20, "통나무가 실제로 생겼다");

            int outside = 0, covered = 0;
            for (float x = -55f; x <= 55f; x += 1f)
            {
                for (float z = -55f; z <= 55f; z += 1f)
                {
                    var pt = new Vector3(x, 0f, z);
                    float deg = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
                    if (deg < 0f) deg += 360f;
                    if (pt.magnitude > 50f) continue;
                    if (deg > 5f && deg < 85f && pt.magnitude > 4f)
                    {
                        Assert.IsFalse(Covered(logs, pt), $"틈 안 ({x},{z})이 막혔다");
                    }
                    else if (StormDebrisGeometry.OutsideGap(x, z, 90f, 3.5f))
                    {
                        outside++;
                        if (Covered(logs, pt)) covered++;
                    }
                }
            }
            Assert.Greater(covered / (float)outside, 0.95f, "틈 밖은 통나무로 막혀 있다(몸이 빠질 구멍 없음)");
        }

        [Test]
        public void 뗏목_통나무는_벽을_파고들지_않는다()
        {
            foreach (var log in StormDebrisGeometry.Raft(Radius, 70f, 99))
            {
                foreach (var c in TopCorners(log))
                {
                    Assert.LessOrEqual(new Vector2(c.x, c.z).magnitude, Radius + 0.01f);
                }
            }
        }

        [Test]
        public void 거목은_날개_폭과_길이_안에_있다()
        {
            const float width = 16f, length = 58f, deg = 120f;
            Vector3 dir = StormDebrisGeometry.Direction(deg);
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            var tree = StormDebrisGeometry.Tree(deg, length, width, 7);
            Assert.Greater(tree.Count, 5);
            foreach (var piece in tree)
            {
                foreach (var c in TopCorners(piece))
                {
                    Assert.LessOrEqual(Mathf.Abs(Vector3.Dot(c, side)), width * 0.5f + 0.01f, piece.Kind + " 가 날개 폭 밖");
                    Assert.LessOrEqual(Vector3.Dot(c, dir), length + 0.01f, piece.Kind + " 가 날개 끝 밖");
                }
            }
        }

        [Test]
        public void 닫힌_조리개_판자는_몸이_빠질_틈을_남기지_않는다()
        {
            const int blades = 6;
            float w = 360f / blades;
            var all = new List<StormDebrisPiece>();
            for (int b = 0; b < blades; b++)
            {
                Vector3 pivot = StormDebrisGeometry.Direction((b + 0.5f) * w);
                foreach (var p in StormDebrisGeometry.PlankPile(b * w, (b + 1) * w, Radius, pivot, b))
                {
                    //  날개 자리(피벗)로 되돌려 조리개 좌표로
                    all.Add(new StormDebrisPiece(p.Kind, p.Center + pivot, p.Size, p.Yaw));
                    foreach (var c in TopCorners(p))
                    {
                        Assert.LessOrEqual(new Vector2(c.x + pivot.x, c.z + pivot.z).magnitude, Radius + 0.01f, "판자가 벽을 파고든다");
                    }
                }
            }
            //  몸 반지름 0.4 — 지름 0.8짜리 원이 통째로 빈 곳이 있으면 빠진다. 0.4 간격 격자 점의 빈 곳을 본다.
            int holes = 0;
            for (float x = -50f; x <= 50f; x += 0.4f)
            {
                for (float z = -50f; z <= 50f; z += 0.4f)
                {
                    var pt = new Vector3(x, 0f, z);
                    if (pt.magnitude > 50f || pt.magnitude < 3f) continue;
                    if (Covered(all, pt) == false
                        && Covered(all, pt + new Vector3(0.4f, 0f, 0f)) == false
                        && Covered(all, pt + new Vector3(0f, 0f, 0.4f)) == false
                        && Covered(all, pt + new Vector3(0.4f, 0f, 0.4f)) == false)
                    {
                        holes++;
                    }
                }
            }
            Assert.AreEqual(0, holes, "닫힌 조리개에 몸이 빠질 구멍");
        }

        [Test]
        public void 같은_씨앗이면_같은_잔해()
        {
            var a = StormDebrisGeometry.Raft(Radius, 70f, 5);
            var b = StormDebrisGeometry.Raft(Radius, 70f, 5);
            Assert.AreEqual(a.Count, b.Count);
            for (int i = 0; i < a.Count; i++)
            {
                Assert.AreEqual(a[i].Center, b[i].Center);
                Assert.AreEqual(a[i].Size, b[i].Size);
            }
        }

        [Test]
        public void 갈매기_판_대형은_엇갈린_줄로_늘어선다()
        {
            var flock = new GullFlock(300f, 0f, 0f, 0.4f, 160f, 30, 0, columns: 10);
            Vector3 first = flock.BirdAt(0, 100), second = flock.BirdAt(1, 100), nextLine = flock.BirdAt(10, 100);
            Assert.AreEqual(3.4f, Vector3.Distance(first, second), 1e-3f, "옆 새와 3.4m");
            Assert.Less(nextLine.x, first.x, "둘째 줄은 뒤에");
        }
    }
}
