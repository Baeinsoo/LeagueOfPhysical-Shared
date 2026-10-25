using System.Reflection;
using GameFramework;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 코스 구간 표식(spec §5). 맵 데이터일 뿐이다 — 판정·시뮬은 안 읽고, 클라 HUD만 읽는다.
    /// <b>씬 주입 대상이 아니라는 것</b>이 요점이다: 주입 대상이면 서버 스코프에도 받을 것을 등록해야 하고, 빠뜨리면 방이 안 뜬다.
    /// </summary>
    public class FlappyCourseSectionTests
    {
        private GameObject go;

        [TearDown]
        public void TearDown()
        {
            if (go != null) { UnityEngine.Object.DestroyImmediate(go); }
        }

        [Test]
        public void 구간_x는_놓인_자리다()
        {
            go = new GameObject("CourseSection_03_133");
            go.transform.position = new Vector3(133.4f, -48f, 0f);
            var section = go.AddComponent<FlappyCourseSection>();

            Assert.AreEqual(133.4f, section.X, 1e-4f);
        }

        [Test]
        public void 기본은_1막이고_마지막이_아니다()
        {
            //  옛 굽기 씬에 이 칸이 없을 때 유니티는 필드 초기값을 쓴다 — 빈 이름·1막·마지막 아님이어야 배너가 엉뚱하게 안 뜬다.
            go = new GameObject("CourseSection");
            var section = go.AddComponent<FlappyCourseSection>();

            Assert.AreEqual(string.Empty, section.Title);
            Assert.AreEqual(1, section.Act);
            Assert.IsFalse(section.Final);
        }

        [Test]
        public void 씬_주입_대상이_아니다()
        {
            //  서버는 이 표식을 안 읽는다. 주입 대상이 되면 서버 FlappyRaceLifetimeScope에도 등록이 필요해지고,
            //  빠뜨리면 맵 로드 때 씬 주입이 끊긴다(FlappyMapRulesField 주석 참고). 스크립트만 있으면 되게 둔다.
            Assert.IsNull(typeof(FlappyCourseSection).GetCustomAttribute<SceneInjectMonoBehaviourAttribute>());
        }
    }
}
