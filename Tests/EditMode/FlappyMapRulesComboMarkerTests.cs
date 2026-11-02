using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LOP.Tests
{
    /// <summary>
    /// 맵 마커의 연속 날갯짓 보상(spec 2026-10-27 §2, 11-02 개정). 초기값이 사용자 결정 숫자여야 이미 구운 맵(이 칸이 직렬화돼 있지 않다)이
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
        public void 마커_초기값은_사용자가_정한_표다()
        {
            var marker = Marker();

            Assert.AreEqual(10, marker.ComboStep);
            CollectionAssert.AreEqual(FlappyFlapComboRules.DefaultGains(), marker.ComboDashGains);
            Assert.AreEqual(0.25f, marker.ComboTailGain);
        }

        [Test]
        public void 초기값_마커는_기본_규칙과_같은_값을_낸다()
        {
            //  어느 맵에도 이 칸이 직렬화돼 있지 않다 — 초기값이 곧 판의 규칙이다(다시 굽기·콘텐츠 배포 없음).
            var field = new FlappyMapRulesField();
            LogAssert.Expect(LogType.Log, new Regex("연속 날갯짓 보상 10마다 \\+0.06"));
            Marker().Construct(field);

            for (int n = 0; n <= 300; n++)
            {
                Assert.AreEqual(FlappyFlapComboRules.Default.Gain(n), field.FlapCombo.Gain(n), n.ToString());
            }
        }

        [Test]
        public void 마커가_간격과_표를_필드에_넣는다()
        {
            var marker = Marker();
            marker.ManualDash = true;
            marker.ComboStep = 3;
            marker.ComboDashGains = new[] { 0.1f, 0.2f };
            marker.ComboTailGain = 0.3f;
            var field = new FlappyMapRulesField();

            marker.Construct(field);

            Assert.AreEqual(0.2f, field.FlapCombo.Gain(6));
            Assert.AreEqual(0.3f, field.FlapCombo.Gain(9));
            Assert.AreEqual(0f, field.FlapCombo.Gain(10));
        }

        [Test]
        public void 마커_간격이_0이면_보상_없는_맵()
        {
            var marker = Marker();
            marker.ComboStep = 0;
            var field = new FlappyMapRulesField();

            marker.Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Off, field.FlapCombo);
        }

        [Test]
        public void 마커_값이_잘못되면_경고하고_기본으로()
        {
            var marker = Marker();
            marker.ComboDashGains = new[] { 0.5f, 0.25f };
            var field = new FlappyMapRulesField();

            LogAssert.Expect(LogType.Warning, new Regex("연속 날갯짓 보상 값이 잘못"));
            marker.Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }
    }
}
