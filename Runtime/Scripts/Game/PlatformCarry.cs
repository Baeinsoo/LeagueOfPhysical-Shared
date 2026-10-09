using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 움직이는 판 위에 선 몸을 판과 같이 옮기고 돌린다(사용자 10-09 "회전하는 장애물 위에 있으면 같이").
    /// 판이 지난 틱 → 이번 틱에 움직인 만큼(행렬 차이) 발 자리를 옮기고, 판이 수평으로 돈 만큼 몸 방향도 돌린다.
    /// </summary>
    public static class PlatformCarry
    {
        public static void Apply(GameFramework.World.Transform body, Matrix4x4 before, Matrix4x4 after)
        {
            //  판 기준 자리는 그대로 두고 판만 옮긴다 — 지난 틱 판 좌표로 바꿨다가 이번 틱 판 좌표에서 되돌린다.
            Vector3 local = before.inverse.MultiplyPoint3x4(body.Position.ToUnity());
            body.Position = after.MultiplyPoint3x4(local).ToNumerics();

            //  몸은 서 있으니 수평으로 돈 만큼만(기울기는 안 옮긴다).
            Quaternion turn = after.rotation * Quaternion.Inverse(before.rotation);
            float yaw = turn.eulerAngles.y;
            if (Mathf.Abs(Mathf.DeltaAngle(0f, yaw)) > 1e-4f)
            {
                //  오래 서 있으면 곱셈 오차가 쌓여 길이가 1에서 벗어난다 — 매번 다시 맞춘다.
                Quaternion turned = Quaternion.Euler(0f, yaw, 0f) * body.Rotation.ToUnity();
                body.Rotation = Quaternion.Normalize(turned).ToNumerics();
            }
        }
    }
}
