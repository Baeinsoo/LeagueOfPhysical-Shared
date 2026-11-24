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

        //  0.4 간격 네 점이 다 비었으면 지름 0.8짜리 몸이 빠질 자리다.
        private static bool BodyFits(List<StormDebrisPiece> pieces, Vector3 pt)
            => Covered(pieces, pt) == false
               && Covered(pieces, pt + new Vector3(0.4f, 0f, 0f)) == false
               && Covered(pieces, pt + new Vector3(0f, 0f, 0.4f)) == false
               && Covered(pieces, pt + new Vector3(0.4f, 0f, 0.4f)) == false;

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

            int holes = 0;
            for (float x = -59f; x <= 59f; x += 0.4f)
            {
                for (float z = -59f; z <= 59f; z += 0.4f)
                {
                    var pt = new Vector3(x, 0f, z);
                    float deg = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
                    if (deg < 0f) deg += 360f;
                    if (pt.magnitude > 58.9f) continue;
                    if (deg > 5f && deg < 85f && pt.magnitude > 4f)
                    {
                        Assert.IsFalse(Covered(logs, pt), $"틈 안 ({x},{z})이 막혔다");
                    }
                    else if (StormDebrisGeometry.OutsideGap(x, z, 90f, 3.5f) && BodyFits(logs, pt))
                    {
                        holes++;
                    }
                }
            }
            //  벽 쪽 가장자리(58.9m까지)를 포함해 틈 밖엔 몸(지름 0.8)이 빠질 빈칸이 하나도 없다.
            Assert.AreEqual(0, holes, "틈 밖에 몸이 빠질 구멍");
        }

        [Test]
        public void 뗏목_통나무는_벽을_파고들지_않는다()
        {
            foreach (var log in StormDebrisGeometry.Raft(Radius, 70f, 99))
            {
                foreach (var c in TopCorners(log))
                {
                    Assert.LessOrEqual(new Vector2(c.x, c.z).magnitude, Radius + 0.1f);
                }
            }
        }

        [Test]
        public void 거목은_날개_폭과_길이_안에_있다()
        {
            //  Build가 부르는 값 그대로(날개 메시 길이 58 + 2).
            const float width = 16f, length = 60f, deg = 120f;
            Vector3 dir = StormDebrisGeometry.Direction(deg);
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            var tree = StormDebrisGeometry.Tree(deg, length, width, 7);
            Assert.Greater(tree.Count, 5);
            foreach (var piece in tree)
            {
                foreach (var c in TopCorners(piece))
                {
                    Assert.LessOrEqual(Mathf.Abs(Vector3.Dot(c, side)), width * 0.5f + 0.01f, piece.Kind + " 가 날개 폭 밖");
                    Assert.LessOrEqual(new Vector2(c.x, c.z).magnitude, 59.1f, piece.Kind + " 가 벽을 파고든다");
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
                        Assert.LessOrEqual(new Vector2(c.x + pivot.x, c.z + pivot.z).magnitude, Radius + 0.1f, "판자가 벽을 파고든다");
                    }
                }
            }
            //  몸 반지름 0.4 — 지름 0.8짜리 원이 통째로 빈 곳이 있으면 빠진다. 0.4 간격 격자 점의 빈 곳을 본다.
            int holes = 0;
            for (float x = -59f; x <= 59f; x += 0.4f)
            {
                for (float z = -59f; z <= 59f; z += 0.4f)
                {
                    var pt = new Vector3(x, 0f, z);
                    //  벽 쪽 가장자리까지 본다(예전 판도 59m까지 막혀 있었다).
                    if (pt.magnitude > 58.5f || pt.magnitude < 3f) continue;
                    if (BodyFits(all, pt)) holes++;
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
