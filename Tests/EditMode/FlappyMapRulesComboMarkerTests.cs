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
        public void 초기값_마커는_기본_규칙이다()
        {
            var field = new FlappyMapRulesField();
            Marker().Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }

        [Test]
        public void 옛_번들에_박힌_배열은_무시하고_경고_없이_기본()
        {
            //  리뷰2 I1 — 번들 빌드는 그때 클래스의 모든 필드를 초기값까지 싣는다. 10-30 번들엔 옛 기본 배열이 들어 있다.
            var marker = Marker();
            JsonUtility.FromJsonOverwrite("{\"ComboMilestones\":[10,25,50,100],\"ComboDashGains\":[0.1,0.25,0.5,0.5]}", marker);
            var field = new FlappyMapRulesField();
            var warnings = new System.Collections.Generic.List<string>();
            void Capture(string message, string stack, LogType type)
            {
                if (type != LogType.Log) warnings.Add(message);
            }
            Application.logMessageReceived += Capture;
            try
            {
                marker.Construct(field);
            }
            finally
            {
                Application.logMessageReceived -= Capture;
            }

            CollectionAssert.IsEmpty(warnings, "판마다 클·서에 경고가 찍히면 안 된다");
            LogAssert.Expect(LogType.Log, new Regex("연속 날갯짓 보상 10마다 \\+0\\.06"));   //  찍히는 것은 이 한 줄뿐
            LogAssert.NoUnexpectedReceived();
            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }

        [Test]
        public void 덮어쓰기를_안_켠_마커는_어떤_값이_박혀_있어도_기본()
        {
            //  이 PR 뒤에 구운 번들엔 지금 표가 박힌다 — 나중에 기본 표를 바꿔도 옛 번들이 조용히 옛 표를 돌리면 안 된다.
            var marker = Marker();
            JsonUtility.FromJsonOverwrite("{\"ComboStep\":5,\"ComboDashGains\":[1.0],\"ComboTailGain\":1.0}", marker);
            var field = new FlappyMapRulesField();

            marker.Construct(field);

            Assert.IsFalse(marker.ComboOverride);
            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }

        [Test]
        public void 덮어쓰기를_켜면_간격과_표를_필드에_넣는다()
        {
            var marker = Marker();
            marker.ManualDash = true;
            marker.ComboOverride = true;
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
        public void 덮어쓰기를_켜고_간격이_0이면_보상_없는_맵()
        {
            var marker = Marker();
            marker.ComboOverride = true;
            marker.ComboStep = 0;
            var field = new FlappyMapRulesField();

            marker.Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Off, field.FlapCombo);
        }

        [Test]
        public void 덮어쓰기를_켠_값이_잘못되면_경고하고_기본으로()
        {
            var marker = Marker();
            marker.ComboOverride = true;
            marker.ComboDashGains = new[] { 0.5f, 0.25f };
            var field = new FlappyMapRulesField();

            LogAssert.Expect(LogType.Warning, new Regex("연속 날갯짓 보상 값이 잘못"));
            marker.Construct(field);

            Assert.AreSame(FlappyFlapComboRules.Default, field.FlapCombo);
        }
    }
}
