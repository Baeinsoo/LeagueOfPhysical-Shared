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
                //  떨어지는 동안은 윗면이 천장선에서 fallen만큼 내려온 자리(가운데 = Size/2 − fallen) — 천장 속에서
                //  미끄러져 나온다. 10-24 전엔 아랫면 기준(가운데 = −(fallen + Size/2))이라 첫 틱에 바위 한 개가 통째로
                //  천장 밑에 나타나, 천장 가까이 날던 새는 볼 틈도 없이 맞았다. Drop(통로 + 바위)만큼 내려오면
                //  윗면이 바닥선에 닿아 그때 다 빠진다. 예전엔 같은 Drop에서 바위가 바닥 밑으로 한 변 더 내려가
                //  있었으니, 통로를 비우는 때는 Size/Speed(1.2/9 ≈ 0.13초)만큼 늦어졌다.
                float y = falling ? rockfall.Size * 0.5f - fallen : rockfall.HiddenRise;
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
