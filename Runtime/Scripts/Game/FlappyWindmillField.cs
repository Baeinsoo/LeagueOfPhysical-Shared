using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 이 판에 놓인 풍차 전부. 맵 씬의 <see cref="FlappyWindmill"/> 마커가 로드될 때 스스로 들어온다.
    ///
    /// <para><see cref="WindField"/>와 달리 정렬하지 않는다 — 바람은 겹친 볼륨의 합을 구해야 해서
    /// 순서가 부동소수 합에 새어 들어갔지만, 풍차는 각자 자기 자세만 대입하므로 순서가 결과를
    /// 바꾸지 않는다(<see cref="LaserField"/>와 같은 이유).</para>
    /// </summary>
    public class FlappyWindmillField
    {
        private readonly List<FlappyWindmill> _windmills = new List<FlappyWindmill>();

        public int Count => _windmills.Count;

        public void Add(FlappyWindmill windmill)
        {
            if (windmill == null || _windmills.Contains(windmill))
            {
                return;
            }
            _windmills.Add(windmill);
        }

        public bool Remove(FlappyWindmill windmill) => _windmills.Remove(windmill);

        /// <summary>이 틱의 각도로 날개를 돌려 놓는다. 매 틱 다시 계산하므로 누적이 없다.</summary>
        public void PoseForTick(long tick, float tickSeconds)
        {
            if (_windmills.Count == 0)
            {
                return;
            }

            bool posed = false;
            for (int i = 0; i < _windmills.Count; i++)
            {
                var windmill = _windmills[i];
                if (windmill == null)
                {
                    continue;   // 씬에서 지워졌다 — 다음 맵 로드 때 목록이 새로 채워진다
                }
                float angle = FlappyWindmillCurve.AngleAt(
                    windmill.StartAngle, windmill.RotSpeed, tick, tickSeconds);
                // Rotate(=더하기)가 아니라 대입이다. 누적하지 않는 것이 이 설계의 핵심이라,
                // 같은 틱을 두 번 굴려도(롤백 재생) 자세가 같은 자리에 선다.
                windmill.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                posed = true;
            }

            if (posed)
            {
                // 최신 유니티는 트랜스폼→물리 자동 동기화가 꺼져 있다. 안 부르면 이어지는 sweep이
                // 날개의 옛 자세를 보고, 화면과 판정이 한 틱씩 어긋난다.
                Physics.SyncTransforms();
            }
        }
    }
}
