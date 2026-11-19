using UnityEngine;

namespace LOP
{
    public static class TriGrabMath
    {
        /// <summary>갈고리 끝 반지름 안의 카드 중 쏜 몸에 가장 가까운 슬롯 번호. 없으면 -1.</summary>
        public static int PickHit(Vector2 tip, Vector2 origin, Vector2[] slotPositions, bool[] occupied)
        {
            int hit = -1;
            float best = float.MaxValue;
            for (int i = 0; i < slotPositions.Length; i++)
            {
                if (!occupied[i]) continue;
                if ((slotPositions[i] - tip).magnitude >= TriGrabConfig.HitRadius) continue;
                float d = (slotPositions[i] - origin).magnitude;
                if (d < best) { best = d; hit = i; }
            }
            return hit;
        }

        /// <summary>yaw(도) → 바닥 방향. 넉백과 같은 규약 (sin, cos).</summary>
        public static Vector2 DirFromYaw(float yawDegrees)
        {
            float r = yawDegrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Sin(r), Mathf.Cos(r));
        }

        public static float YawFromDir(Vector2 d) => Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
    }
}
