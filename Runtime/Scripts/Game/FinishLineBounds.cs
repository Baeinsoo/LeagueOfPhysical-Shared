using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 결승선이 어디 있는지. 맵 씬의 <see cref="FinishLine"/> 마커가 맵 로드 때 스스로 등록한다
    /// (<see cref="WindVolume"/>과 같은 통로).
    ///
    /// <para>시뮬이 첫 틱에 씬을 훑지 않게 하려는 것이다 — 그러면 시뮬이 엔진 씬을 알게 되고,
    /// 되돌리기 재생 중에 무엇을 보는지가 불분명해진다.</para>
    /// </summary>
    public class FinishLineBounds
    {
        private readonly FinishAxis axis;
        private readonly float? fallbackCoordinate;

        private Bounds registered;
        private bool hasRegistered;

        /// <param name="fallbackCoordinate">
        /// 마커가 없는 맵을 위한 대비. 그 좌표에 두께 0인 선을 세운다. 주지 않으면 결승선이
        /// 없는 것으로 보고 아무도 통과하지 않는다.
        /// </param>
        public FinishLineBounds(FinishAxis axis, float? fallbackCoordinate = null)
        {
            this.axis = axis;
            this.fallbackCoordinate = fallbackCoordinate;
        }

        public void Register(Bounds bounds)
        {
            registered = bounds;
            hasRegistered = true;
        }

        /// <summary>맵을 다시 로드하면 옛 마커가 사라진다 — 그때 거둔다.</summary>
        public void Unregister()
        {
            hasRegistered = false;
        }

        public bool TryGet(out Bounds bounds)
        {
            if (hasRegistered)
            {
                bounds = registered;
                return true;
            }
            if (fallbackCoordinate.HasValue)
            {
                bounds = new Bounds(Center(fallbackCoordinate.Value), Vector3.zero);
                return true;
            }
            bounds = default;
            return false;
        }

        private Vector3 Center(float coordinate)
        {
            switch (axis)
            {
                case FinishAxis.X: return new Vector3(coordinate, 0f, 0f);
                case FinishAxis.Y: return new Vector3(0f, coordinate, 0f);
                default: return new Vector3(0f, 0f, coordinate);
            }
        }
    }
}
