using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 캡슐의 두 끝점(p1·p2)을 발밑 기준 위치에서 뽑아낸다. <see cref="KinematicMover"/>의 sweep과
    /// <see cref="FlappyMoverOverlap"/>의 overlap이 같은 식을 쓰게 여기 한 곳에 모은다 — 따로 베껴 두면
    /// 누운 캡슐 도입 때처럼 한쪽만 고치고 다른 쪽을 잊는 일이 생긴다.
    /// </summary>
    public static class CapsuleEnds
    {
        /// <param name="feet">발밑 기준 위치.</param>
        /// <param name="lyingLength">
        /// 누운 캡슐의 축 방향 전체 길이. 0 이하면 세운(Y) 캡슐로 본다 — 지금까지의 식
        /// (<c>feet+up·r</c>, <c>feet+up·(h-r)</c>) 그대로다. 0보다 크면 중심을 발밑에서
        /// <c>up·r</c>만큼 올린 자리로 잡고, 거기서 <c>right</c> 축으로 반쪽씩 벌린다.
        /// </param>
        public static void Of(Vector3 feet, float radius, float height, float lyingLength,
            out Vector3 p1, out Vector3 p2)
        {
            if (lyingLength <= 0f)
            {
                p1 = feet + Vector3.up * radius;
                p2 = feet + Vector3.up * (height - radius);
                return;
            }

            Vector3 center = feet + Vector3.up * radius;
            float half = lyingLength * 0.5f - radius;
            p1 = center - Vector3.right * half;
            p2 = center + Vector3.right * half;
        }
    }
}
