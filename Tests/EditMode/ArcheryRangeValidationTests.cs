using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 씬(자리)과 마스터데이터(거리·노출)는 서로를 모른다. 둘이 어긋나면 <b>에러 없이 판만 이상해진다</b> —
    /// 과녁이 선언보다 두 배 먼 데 서 있어도 아무 일도 안 일어난다. 판 시작에 한 번 대조한다.
    /// </summary>
    public class ArcheryRangeValidationTests
    {
        private readonly List<GameObject> spawned = new List<GameObject>();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in spawned) { Object.DestroyImmediate(go); }
            spawned.Clear();
        }

        private ArcheryRangeLayout Layout(int laneCount, params float[] distances)
        {
            var lanes = new List<ArcheryLane>();
            for (int i = 0; i < laneCount; i++)
            {
                var root = new GameObject("lane" + i);
                spawned.Add(root);
                root.transform.position = new Vector3(i * 4f, 0f, 0f);
                var lane = root.AddComponent<ArcheryLane>();
                lane.Order = i;
                lane.Stands = new Transform[distances.Length];
                for (int s = 0; s < distances.Length; s++)
                {
                    var stand = new GameObject("stand" + s);
                    stand.transform.SetParent(root.transform);
                    stand.transform.localPosition = new Vector3(0f, 1.3f, distances[s]);
                    lane.Stands[s] = stand.transform;
                }
                lanes.Add(lane);
            }
            return ArcheryRangeLayout.From(lanes);
        }

        private static ArcheryRangeSettings Settings(params float[] declaredDistances)
        {
            var stands = new List<ArcheryRangeStand>();
            for (int i = 0; i < declaredDistances.Length; i++)
            {
                stands.Add(new ArcheryRangeStand(i, declaredDistances[i], 200, 0f, 0f));
            }
            return new ArcheryRangeSettings(default, stands, 25);
        }

        [Test]
        public void 맞으면_아무_말도_안_한다()
        {
            Assert.IsNull(ArcheryRangeValidation.Check(Layout(2, 12f, 30f), Settings(12f, 30f), archerCount: 2));
        }

        [Test]
        public void 레인이_사수보다_적으면_말한다()
        {
            string problem = ArcheryRangeValidation.Check(Layout(1, 12f), Settings(12f), archerCount: 2);

            Assert.IsNotNull(problem, "레인이 모자라면 그 사수는 과녁이 영영 안 뜬다");
            StringAssert.Contains("레인", problem);
        }

        [Test]
        public void 자리_수가_데이터와_다르면_말한다()
        {
            //  씬 레인엔 자리가 셋(0,1,2)인데 데이터는 둘(0,1)만 말한다 — 선언한 자리 번호는
            //  전부 씬 범위 안이라 자리 번호 검사는 안 걸리고, 오직 개수 검사만 걸려야 한다.
            string problem = ArcheryRangeValidation.Check(Layout(2, 12f, 30f, 45f), Settings(12f, 30f), archerCount: 2);

            Assert.IsNotNull(problem);
            StringAssert.Contains("자리", problem);
        }

        [Test]
        public void 선언한_거리와_실제_자리가_멀면_말한다()
        {
            //  데이터는 30m라는데 씬은 45m에 세워 뒀다.
            string problem = ArcheryRangeValidation.Check(Layout(2, 12f, 45f), Settings(12f, 30f), archerCount: 2);

            Assert.IsNotNull(problem);
            StringAssert.Contains("30", problem, "어느 값이 어긋났는지 메시지에 있어야 고칠 수 있다");
        }

        [Test]
        public void 반올림_정도의_차이는_봐준다()
        {
            //  씬에서 손으로 놓은 자리다 — 센티미터 단위로 맞출 수는 없다.
            Assert.IsNull(ArcheryRangeValidation.Check(Layout(2, 12.4f, 29.7f), Settings(12f, 30f), archerCount: 2));
        }

        [Test]
        public void 선언한_자리_번호가_씬에_없으면_말한다()
        {
            //  데이터는 7번 자리를 말하는데 레인엔 자리가 6개뿐이다 — 그대로 두면 매 틱 인덱싱에서 죽는다.
            var stands = new List<ArcheryRangeStand> { new ArcheryRangeStand(6, 12f, 200, 0f, 0f) };
            var range = new ArcheryRangeSettings(default, stands, 25);

            string problem = ArcheryRangeValidation.Check(Layout(2, 12f), range, archerCount: 2);

            Assert.IsNotNull(problem, "씬에 없는 자리 번호를 가리키면 매 틱 인덱싱에서 죽는다");
        }

        [Test]
        public void ShootOff는_레인_하나로_네_명을_받는다()
        {
            var layout = Layout(1, 10f, 20f, 30f);
            var stands = new[] { new ArcheryRangeStand(0, 10f, 250, 0f, 0f), new ArcheryRangeStand(0, 10f, 250, 0f, 0f),
                                 new ArcheryRangeStand(2, 30f, 250, 0f, 0f), new ArcheryRangeStand(1, 20f, 250, 0f, 0f) };
            var range = new ArcheryRangeSettings(default, stands, 200);
            Assert.IsNull(ArcheryRangeValidation.Check(layout, range, 4, ArcheryCourseKind.ShootOff));
        }

        [Test]
        public void ShootOff도_범위_밖_자리_번호는_거절()
        {
            var layout = Layout(1, 10f, 20f, 30f);
            var range = new ArcheryRangeSettings(default, new[] { new ArcheryRangeStand(5, 10f, 250, 0f, 0f) }, 200);
            Assert.IsNotNull(ArcheryRangeValidation.Check(layout, range, 4, ArcheryCourseKind.ShootOff));
        }
    }
}
