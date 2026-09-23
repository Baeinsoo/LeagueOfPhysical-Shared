using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 부스트 패드 하나가 덮는 사각형. <see cref="WindCylinder"/>와 같은 자리의 값 객체다 —
    /// 씬 마커(<see cref="FlappyBoostPad"/>)가 이걸 만들어 필드에 넣고, 사라질 때 같은 것을 뺀다.
    /// </summary>
    public class FlappyBoostRect
    {
        public readonly float X0, X1, Y0, Y1;

        /// <summary>밟으면 몇 초 동안 대시 상태가 되나.</summary>
        public readonly float Duration;

        public FlappyBoostRect(float x0, float x1, float y0, float y1, float duration)
        {
            //  거꾸로 들어와도 같은 자리를 덮게 한다 — 씬에서 음수 스케일로 놓는 일이 있다.
            X0 = System.Math.Min(x0, x1);
            X1 = System.Math.Max(x0, x1);
            Y0 = System.Math.Min(y0, y1);
            Y1 = System.Math.Max(y0, y1);
            Duration = duration;
        }

        public bool Contains(float x, float y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>
    /// 이 판에 놓인 부스트 패드 전부. 맵 씬의 <see cref="FlappyBoostPad"/> 마커가 로드될 때
    /// 스스로 들어온다(<see cref="WindField"/>·<see cref="FlappyWindmillField"/>와 같은 모양).
    ///
    /// <para><b>물리 질의를 쓰지 않는다.</b> 사각형 포함 여부를 산술로만 계산한다 — 트리거
    /// 콜라이더로 판정하면 클·서가 다른 틱에 다른 답을 내고, 롤백 재생에서는 물리를 아예 안 돌려
    /// 답이 없다.</para>
    ///
    /// <para><b>겹치면 긴 쪽이 이긴다.</b> 바람처럼 합하지 않는 이유는, 부스트는 "남은 시간"
    /// 하나뿐이라 두 패드를 더할 자리가 없기 때문이다. 긴 쪽으로 고정하면 등록 순서가 결과를
    /// 바꾸지 못한다.</para>
    /// </summary>
    public class FlappyBoostPadField
    {
        private readonly List<FlappyBoostRect> _rects = new List<FlappyBoostRect>();

        public int Count => _rects.Count;

        public void Add(FlappyBoostRect rect)
        {
            if (rect == null || _rects.Contains(rect))
            {
                return;
            }
            _rects.Add(rect);
        }

        public bool Remove(FlappyBoostRect rect) => _rects.Remove(rect);

        /// <summary>
        /// (x, y)가 어느 패드 안이면 그 지속시간을 준다. 겹치면 가장 긴 것.
        /// </summary>
        public bool TryDuration(float x, float y, out float duration)
        {
            duration = 0f;
            bool found = false;
            for (int i = 0; i < _rects.Count; i++)
            {
                var rect = _rects[i];
                if (rect.Contains(x, y) && (found == false || rect.Duration > duration))
                {
                    duration = rect.Duration;
                    found = true;
                }
            }
            return found;
        }
    }
}
