using System.Collections.Generic;
using GameFramework.World;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>세이브 발판 — 내려앉아 저장, 죽으면 내 저장 발판(스펙 2026-10-06-skydive-save-pads-design).</summary>
    public class SkydiveSavePadTests
    {
        //  섬 구석 발판: 윗면 y = 762, x 210~220, z -35~-25
        private static readonly Bounds IslandPad = new Bounds(new Vector3(215f, 761f, -30f), new Vector3(10f, 2f, 10f));
        private static readonly Bounds TerracePad = new Bounds(new Vector3(-60f, 1101f, -60f), new Vector3(10f, 2f, 10f));

        private static SavePadField Pads()
        {
            var pads = new SavePadField();
            pads.Add(1, TerracePad, "테라스 1100");
            pads.Add(2, IslandPad, "섬");
            return pads;
        }

        private static Entity Diver(Vector3 feet, bool grounded)
        {
            var e = new Entity("d1");
            e.Add(new GameFramework.World.Transform());
            e.Add(new Velocity());
            e.Add(new GroundState { IsGrounded = grounded });
            e.Add(new SkydiveSave());
            e.Teleport(feet);
            return e;
        }

        [Test]
        public void 발판_위에_서면_저장한다()
        {
            var d = Diver(new Vector3(214f, 762f, -31f), grounded: true);
            SkydiveSaveSystem.Tick(d, Pads());
            Assert.AreEqual(2, d.Get<SkydiveSave>().PadId);
        }

        [Test]
        public void 공중에서_발판_위를_지나가기만_하면_저장하지_않는다()
        {
            var d = Diver(new Vector3(214f, 762.3f, -31f), grounded: false);
            SkydiveSaveSystem.Tick(d, Pads());
            Assert.AreEqual(SkydiveSave.None, d.Get<SkydiveSave>().PadId);
        }

        [Test]
        public void 발판_밖_땅에_서면_저장하지_않는다()
        {
            var d = Diver(new Vector3(190f, 760f, -31f), grounded: true);   // 섬 바닥, 발판 옆
            SkydiveSaveSystem.Tick(d, Pads());
            Assert.AreEqual(SkydiveSave.None, d.Get<SkydiveSave>().PadId);
        }

        [Test]
        public void 마지막에_밟은_발판이_이긴다()
        {
            var pads = Pads();
            var d = Diver(new Vector3(214f, 762f, -31f), grounded: true);
            SkydiveSaveSystem.Tick(d, pads);
            d.Teleport(new Vector3(-61f, 1102f, -59f));   // 위 발판으로 되돌아가도 마지막 것
            SkydiveSaveSystem.Tick(d, pads);
            Assert.AreEqual(1, d.Get<SkydiveSave>().PadId);
            d.Get<GroundState>().IsGrounded = false;
            d.Teleport(new Vector3(0f, 500f, 0f));
            SkydiveSaveSystem.Tick(d, pads);
            Assert.AreEqual(1, d.Get<SkydiveSave>().PadId, "떨어지는 동안 유지");
        }

        [Test]
        public void 되감으면_저장도_그_틱으로_돌아간다()
        {
            var d = Diver(new Vector3(214f, 762f, -31f), grounded: true);
            var before = SkydiveSavedState.Capture(d);   // 저장 전
            SkydiveSaveSystem.Tick(d, Pads());
            var after = SkydiveSavedState.Capture(d);

            before.RestoreTo(d);
            Assert.AreEqual(SkydiveSave.None, d.Get<SkydiveSave>().PadId);
            after.RestoreTo(d);
            Assert.AreEqual(2, d.Get<SkydiveSave>().PadId);
        }

        [Test]
        public void 발판_맵에서_죽으면_내_저장_발판_위로()
        {
            var d = Diver(new Vector3(214f, 762f, -31f), grounded: true);
            var pads = Pads();
            SkydiveSaveSystem.Tick(d, pads);
            var auto = new Dictionary<float, Vector3> { { 1500f, new Vector3(0f, 1500f, 40f) } };

            Vector3 p = SkydiveRespawn.BasePoint(d, deathY: 400f, new List<float> { 1500f }, 1500f, auto, pads);
            Assert.AreEqual(new Vector3(215f, 762f, -30f), p);
        }

        [Test]
        public void 발판_맵에서_저장_없이_죽으면_출발로()
        {
            //  아래에 자동 체크포인트가 있어도 쓰지 않는다 — 저장을 안 한 대가다.
            var d = Diver(new Vector3(0f, 500f, 0f), grounded: false);
            var auto = new Dictionary<float, Vector3> { { 1500f, new Vector3(0f, 1500f, 40f) }, { 900f, new Vector3(-45f, 900f, -45f) } };

            Vector3 p = SkydiveRespawn.BasePoint(d, deathY: 400f, new List<float> { 1500f, 900f }, 1500f, auto, Pads());
            Assert.AreEqual(new Vector3(0f, 1500f, 40f), p);
        }

        [Test]
        public void 발판_없는_맵은_지나온_체크포인트로()
        {
            var d = Diver(new Vector3(0f, 500f, 0f), grounded: false);
            var auto = new Dictionary<float, Vector3> { { 1500f, new Vector3(0f, 1500f, 40f) }, { 900f, new Vector3(-45f, 900f, -45f) } };

            Vector3 p = SkydiveRespawn.BasePoint(d, deathY: 400f, new List<float> { 1500f, 900f }, 1500f, auto, new SavePadField());
            Assert.AreEqual(new Vector3(-45f, 900f, -45f), p);
        }
    }
}
