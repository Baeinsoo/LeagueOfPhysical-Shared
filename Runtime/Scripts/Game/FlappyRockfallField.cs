using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 판의 낙석 전부. <see cref="FlappyLiftField"/>와 같은 꼴 — 각자 자기 바위 자리만 대입하므로 순서가 결과를 안 바꾼다.
    /// </summary>
    public class FlappyRockfallField
    {
        private readonly List<FlappyRockfall> _rockfalls = new List<FlappyRockfall>();

        public int Count => _rockfalls.Count;

        public void Add(FlappyRockfall rockfall)
        {
            if (rockfall == null || _rockfalls.Contains(rockfall))
            {
                return;
            }
            _rockfalls.Add(rockfall);
        }

        public bool Remove(FlappyRockfall rockfall) => _rockfalls.Remove(rockfall);

        /// <summary>이 틱의 자리로 바위를 대입한다. 매 틱 다시 계산하므로 누적이 없다.</summary>
        public void PoseForTick(long tick, float tickSeconds)
        {
            bool posed = false;
            for (int i = 0; i < _rockfalls.Count; i++)
            {
                var rockfall = _rockfalls[i];
                if (rockfall == null || rockfall.Rock == null)
                {
                    continue;
                }
                bool falling = FlappyRockfallCurve.FallingAt(rockfall.Period, rockfall.Speed, rockfall.Drop, rockfall.Phase,
                                                             tick, tickSeconds, out float fallen);
                //  떨어지지 않는 동안은 천장 속(판정 밖)에 둔다 — 콜라이더를 껐다 켜는 상태를 따로 들지 않으려고.
                float y = falling ? -(fallen + rockfall.Size * 0.5f) : rockfall.HiddenRise;
                Vector3 p = rockfall.Rock.localPosition;
                rockfall.Rock.localPosition = new Vector3(p.x, y, p.z);
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
