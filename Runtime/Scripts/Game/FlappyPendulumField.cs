using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 판의 진자 전부. <see cref="FlappyWindmillField"/>와 같은 꼴 — 각자 자기 자세만 대입하므로 순서가 결과를 안 바꾼다.
    /// </summary>
    public class FlappyPendulumField
    {
        private readonly List<FlappyPendulum> _pendulums = new List<FlappyPendulum>();

        public int Count => _pendulums.Count;

        public void Add(FlappyPendulum pendulum)
        {
            if (pendulum == null || _pendulums.Contains(pendulum))
            {
                return;
            }
            _pendulums.Add(pendulum);
        }

        public bool Remove(FlappyPendulum pendulum) => _pendulums.Remove(pendulum);

        /// <summary>이 틱의 각도로 진자를 세운다. 매 틱 다시 계산하므로 누적이 없다.</summary>
        public void PoseForTick(long tick, float tickSeconds)
        {
            bool posed = false;
            for (int i = 0; i < _pendulums.Count; i++)
            {
                var pendulum = _pendulums[i];
                if (pendulum == null)
                {
                    continue;
                }
                float angle = FlappyPendulumCurve.AngleAt(pendulum.Amplitude, pendulum.Period, pendulum.Phase, tick, tickSeconds);
                pendulum.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                if (pendulum.Upright != null)
                {
                    //  부모(진자)가 이미 angle만큼 돌았으니 반대로 되돌리면 전역 회전이 0으로 남는다
                    //  — 수레는 흔들리는 자리(원 위)는 그대로 따라가면서도 바닥과 수평을 유지한다.
                    pendulum.Upright.localRotation = Quaternion.Euler(0f, 0f, -angle);
                }
                posed = true;
            }
            if (posed)
            {
                //  자동 동기화가 꺼져 있다 — 안 부르면 이어지는 sweep·겹침 질의가 옛 자세를 본다.
                Physics.SyncTransforms();
            }
        }
    }
}
