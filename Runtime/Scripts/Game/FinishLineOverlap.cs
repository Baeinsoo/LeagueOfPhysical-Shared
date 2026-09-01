using UnityEngine;

namespace LOP
{
    /// <summary>코스가 나아가는 축. 결승선을 어느 방향으로 넘는지는 게임이 정한다.</summary>
    public enum FinishAxis
    {
        X,
        Y,
        Z,
    }

    /// <summary>
    /// 결승선에 <b>형상이 닿았는지</b>와 <b>얼마나 넘어갔는지</b>를 한 값으로 답한다.
    /// 좌표 한 점이 아니라 두 바운드 박스가 축 위에서 겹치는지를 본다 — 새의 부리가 선에 닿은
    /// 순간이 통과이지, 몸 한가운데가 선에 닿아야 통과인 것은 아니다.
    ///
    /// <para>돌려주는 값이 <b>음수면 아직</b>이고, <b>0 이상이면 닿았으며 그 값이 넘어간 깊이</b>다.
    /// 같은 틱에 둘이 닿았을 때 더 깊이 넘어간 쪽이 먼저 들어온 것이므로, 이 한 값이 판정과
    /// 등수 가르기를 동시에 해 준다.</para>
    /// </summary>
    public static class FinishLineOverlap
    {
        /// <param name="body">달리는 몸의 월드 바운드(콜라이더 기준).</param>
        /// <param name="line">결승선의 월드 바운드(보이는 판 기준).</param>
        /// <param name="axis">코스가 나아가는 축.</param>
        /// <param name="increasing">그 축의 값이 커지는 방향으로 달리면 true(Flappy=+x), 작아지면 false(Skydive=−y).</param>
        public static float Past(Bounds body, Bounds line, FinishAxis axis, bool increasing)
        {
            return increasing
                ? Component(body.max, axis) - Component(line.min, axis)
                : Component(line.max, axis) - Component(body.min, axis);
        }

        private static float Component(Vector3 value, FinishAxis axis)
        {
            switch (axis)
            {
                case FinishAxis.X: return value.x;
                case FinishAxis.Y: return value.y;
                default: return value.z;
            }
        }
    }
}
