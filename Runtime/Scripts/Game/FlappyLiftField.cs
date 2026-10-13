using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 판의 승강기 전부. <see cref="FlappyShutterField"/>와 같은 꼴 — 각자 자기 케이지 높이만 대입하므로 순서가 결과를 안 바꾼다.
    /// </summary>
    public class FlappyLiftField
    {
        private readonly List<FlappyLift> _lifts = new List<FlappyLift>();

        public int Count => _lifts.Count;

        public void Add(FlappyLift lift)
        {
            if (lift == null || _lifts.Contains(lift))
            {
                return;
            }
            _lifts.Add(lift);
        }

        public bool Remove(FlappyLift lift) => _lifts.Remove(lift);

        /// <summary>이 틱의 높이로 케이지를 대입한다. 매 틱 다시 계산하므로 누적이 없다.</summary>
        public void PoseForTick(long tick, float tickSeconds)
        {
            bool posed = false;
            for (int i = 0; i < _lifts.Count; i++)
            {
                var lift = _lifts[i];
                if (lift == null || lift.Cage == null)
                {
                    continue;
                }
                float offset = FlappyLiftCurve.OffsetAt(lift.Amplitude, lift.Period, lift.Phase, tick, tickSeconds);
                Vector3 p = lift.Cage.localPosition;
                lift.Cage.localPosition = new Vector3(p.x, offset, p.z);
                posed = true;
            }
            if (posed)
            {
                //  자동 동기화가 꺼져 있다 — 안 부르면 이어지는 sweep·겹침 질의가 옛 자리를 본다.
                Physics.SyncTransforms();
            }
        }
    }
}
