using UnityEngine;

namespace LOP
{
    /// <summary>탈것의 머리 방향(도)과 앞으로 가는 속도.</summary>
    public readonly struct MountState
    {
        public readonly float HeadingDeg;
        public readonly float Speed;

        public MountState(float headingDeg, float speed)
        {
            HeadingDeg = headingDeg;
            Speed = speed;
        }
    }

    /// <summary>
    /// 탈것 이동 커널. 스틱은 "가고 싶은 방향"일 뿐, 몸은 돌기 빠르기 한도 안에서 따라가고
    /// 속도는 가속/감속 한도 안에서 바뀐다. 앞으로만 간다.
    /// </summary>
    public static class MountMotor
    {
        /// <summary>이보다 약한 스틱은 놓은 것으로 본다.</summary>
        private const float Deadzone = 0.08f;

        /// <param name="desired">세계 x,z 기준 스틱 값(패드가 카메라 기준으로 이미 돌린 것).</param>
        public static MountState Step(MountState current, Vector2 desired, bool drawing,
                                      in MountSettings s, float dt)
        {
            float heading = current.HeadingDeg;
            float mag = Mathf.Clamp01(desired.magnitude);
            float target;

            if (mag < Deadzone)
            {
                target = 0f;
            }
            else
            {
                float want = Mathf.Atan2(desired.x, desired.y) * Mathf.Rad2Deg;
                //  급회전 판정은 돌기 전 차이로 한다 — 돈 뒤로 재면 한 틱 돌고 나서야 감속이 걸린다.
                float diff = Mathf.DeltaAngle(heading, want);
                float rate = Mathf.Lerp(s.TurnRateStoppedDeg, s.TurnRateAtMaxDeg,
                                        Mathf.Clamp01(current.Speed / s.MaxSpeed));
                heading += Mathf.Clamp(diff, -rate * dt, rate * dt);
                target = mag * s.MaxSpeed * (Mathf.Abs(diff) > s.SharpTurnDeg ? s.SharpTurnSpeedFactor : 1f);
            }

            if (drawing)
            {
                target *= s.DrawSpeedFactor;
            }

            float change = target > current.Speed ? s.Acceleration : s.Deceleration;
            float speed = Mathf.Max(0f, Mathf.MoveTowards(current.Speed, target, change * dt));

            return new MountState(Normalize(heading), speed);
        }

        /// <summary>속도를 세계 수평 벡터로 바꾼다(y=0).</summary>
        public static Vector3 VelocityOf(MountState state)
        {
            float rad = state.HeadingDeg * Mathf.Deg2Rad;
            return new Vector3(Mathf.Sin(rad), 0f, Mathf.Cos(rad)) * state.Speed;
        }

        //  (-180, 180]로 정리한다.
        private static float Normalize(float deg)
        {
            deg = Mathf.Repeat(deg + 180f, 360f) - 180f;
            return deg == -180f ? 180f : deg;
        }
    }
}
