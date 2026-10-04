using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 판의 셔터 전부. <see cref="FlappyPendulumField"/>와 같은 꼴 — 각자 자기 문 높이만 대입하므로 순서가 결과를 안 바꾼다.
    /// </summary>
    public class FlappyShutterField
    {
        private readonly List<FlappyShutter> _shutters = new List<FlappyShutter>();

        public int Count => _shutters.Count;

        public void Add(FlappyShutter shutter)
        {
            if (shutter == null || _shutters.Contains(shutter))
            {
                return;
            }
            _shutters.Add(shutter);
        }

        public bool Remove(FlappyShutter shutter) => _shutters.Remove(shutter);

        /// <summary>이 틱의 열림으로 문 높이를 대입한다. 매 틱 다시 계산하므로 누적이 없다.</summary>
        public void PoseForTick(long tick, float tickSeconds)
        {
            bool posed = false;
            for (int i = 0; i < _shutters.Count; i++)
            {
                var shutter = _shutters[i];
                if (shutter == null || shutter.Door == null)
                {
                    continue;
                }
                float open = FlappyShutterCurve.OpenAt(shutter.Period, shutter.OpenShare, shutter.MoveShare,
                                                       shutter.Phase, tick, tickSeconds);
                Vector3 p = shutter.Door.localPosition;
                shutter.Door.localPosition = new Vector3(p.x, shutter.Travel * open, p.z);
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
