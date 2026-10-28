using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    /// <summary>
    /// 맵 마커의 연속 날갯짓 고비(spec 2026-10-27 §2). 초기값이 사용자 결정 숫자여야 이미 구운 맵(이 칸이 직렬화돼 있지 않다)이
    /// 다시 굽지 않고도 기본으로 돈다.
    /// </summary>
    public class FlappyMapRulesComboMarkerTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null)
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private FlappyMapRules Marker()
        {
            go = new GameObject("FlappyMapRules");
            return go.AddComponent<FlappyMapRules>();
        }

        [Test]
        public void 마커_초기값은_사용자가_정한_고비다()
        {
            var marker = Marker();

            CollectionAssert.AreEqual(new[] { 10, 25, 50, 100 }, marker.ComboMilestones);
            CollectionAssert.AreEqual(new[] { 0.25f, 0.5f, 0.5f, 1f }, marker.ComboDashGains);
        }

        [Test]
        public void 마커가_고비를_필드에_넣는다()
        {
            var marker = Marker();
            marker.ManualDash = true;
            marker.ComboMilestones = new[] { 3, 6 };
            marker.ComboDashGains = new[] { 0.1f, 0.2f };
            var field = new FlappyMapRulesField();

            marker.Construct(field);

            Assert.AreEqual(1, field.FlapCombo.MilestoneIndex(6));
            Assert.AreEqual(0.2f, field.FlapCombo.Gain(6));
            Assert.AreEqual(-1, field.FlapCombo.MilestoneIndex(10));
        }

        [Test]
        public void 마커_값이_잘못되면_경고하고_기본으로()
        {
            var marker = Marker();
            marker.ComboMilestones = new[] { 25, 10 };
            marker.ComboDashGains = new[] { 0.5f, 0.25f };
            var field = new FlappyMapRulesField();

            LogAssert.Expect(LogType.Warning, new Regex("연속 날갯짓 고비 값이 잘못"));
            marker.Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }
    }
}
