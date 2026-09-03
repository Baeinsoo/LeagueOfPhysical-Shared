using System.Collections.Generic;
using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 이 판의 바람 전부. 위치를 넣으면 그 지점의 바람이 나온다.
    ///
    /// 시간을 타지 않는다 — 클라가 과거 틱으로 되감아 다시 달려도 같은 답이 나온다.
    /// (움직이는 강체를 이번에 안 만드는 이유가 정확히 이 성질의 반대다.)
    /// </summary>
    public class WindField
    {
        private readonly List<WindCylinder> cylinders = new List<WindCylinder>();

        public int Count => cylinders.Count;

        public void Add(WindCylinder cylinder)
        {
            if (cylinder == null || cylinders.Contains(cylinder))
            {
                return;
            }

            cylinders.Add(cylinder);

            // 겹친 바람을 더하는 순서를 고정한다. 씬에서 들어오는 순서는 정해져 있지 않은데,
            // 부동소수 덧셈은 순서가 바뀌면 마지막 자릿수가 바뀌어 클·서가 갈린다.
            // List.Sort는 불안정 정렬이라 비교가 동률(0)로 남으면 그 자리에 삽입 순서가 그대로
            // 새어 들어온다 — 그래서 모든 필드(바람 값까지)를 비교해 동률이면 완전히 같은
            // 볼륨이게 만든다(같은 값끼리 더하는 건 순서를 타지 않는다).
            cylinders.Sort(CompareForStableSum);
        }

        public bool Remove(WindCylinder cylinder) => cylinders.Remove(cylinder);

        public Vector3 SampleAt(Vector3 position)
        {
            var total = Vector3.Zero;
            for (int i = 0; i < cylinders.Count; i++)
            {
                if (cylinders[i].Contains(position))
                {
                    total += cylinders[i].Wind;
                }
            }
            return total;
        }

        private static int CompareForStableSum(WindCylinder left, WindCylinder right)
        {
            int result = left.Center.Y.CompareTo(right.Center.Y);
            if (result != 0) return result;
            result = left.Center.X.CompareTo(right.Center.X);
            if (result != 0) return result;
            result = left.Center.Z.CompareTo(right.Center.Z);
            if (result != 0) return result;
            result = left.Radius.CompareTo(right.Radius);
            if (result != 0) return result;
            result = left.Height.CompareTo(right.Height);
            if (result != 0) return result;
            result = left.Wind.X.CompareTo(right.Wind.X);
            if (result != 0) return result;
            result = left.Wind.Y.CompareTo(right.Wind.Y);
            if (result != 0) return result;
            return left.Wind.Z.CompareTo(right.Wind.Z);
        }
    }
}
