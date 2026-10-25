using UnityEngine;

namespace LOP
{
    /// <summary>탈것의 머리 방향(도)과 앞으로 가는 속도(음수면 후진).</summary>
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
    /// 탈것 이동 커널. 스틱은 말 기준이다 — 미는 세기가 가속, 좌우가 방향 돌리기, 곧바로 아래는 브레이크 뒤 후진.
    /// 카메라가 어디를 보든 "위 = 말 앞"이라, 뒤를 보고 쏘면서도 말은 똑바로 달릴 수 있다.
    /// 속도는 부호가 있다(음수 = 후진).
    /// </summary>
    public static class MountMotor
    {
        /// <summary>이보다 약한 스틱은 놓은 것으로 본다.</summary>
        private const float Deadzone = 0.08f;

        /// <summary>이 속도 이하로 느려지면 멈춘 것으로 보고 아래 스틱을 후진으로 바꾼다.</summary>
        private const float StopSpeed = 0.1f;

        /// <param name="stick">말 기준 스틱 값. x = 좌우 돌리기, 스틱을 민 세기 = 가속, 아래쪽 원뿔 = 브레이크·후진.</param>
        public static MountState Step(MountState current, Vector2 stick, bool drawing,
                                      in MountSettings s, float dt)
        {
            float heading = current.HeadingDeg;
            float mag = Mathf.Clamp01(stick.magnitude);
            bool released = stick.magnitude < Deadzone;
            float turn = released ? 0f : Mathf.Clamp(stick.x, -1f, 1f);

            float rate = Mathf.Lerp(s.TurnRateStoppedDeg, s.TurnRateAtMaxDeg,
                                    Mathf.Clamp01(Mathf.Abs(current.Speed) / s.MaxSpeed));
            heading += turn * rate * dt;

            //  브레이크·후진은 스틱이 곧바로 아래 쪽(아래 ±45도)일 때만이다. 옆으로 밀거나 옆으로 살짝
            //  흔들리는 건 돌기일 뿐 서는 게 아니다 — 돌면서도 달려야 달리며 옆·뒤로 쏠 수 있다.
            bool downward = released == false && stick.y < 0f && Mathf.Abs(stick.x) <= -stick.y;

            float target;
            if (released)
            {
                target = 0f;
            }
            else if (downward)
            {
                //  앞으로 달리는 중이면 먼저 0까지 줄이고, 멈춘 뒤에야 후진한다.
                target = current.Speed > StopSpeed ? 0f : -mag * s.ReverseSpeed;
            }
            else
            {
                target = mag * s.MaxSpeed;
            }

            if (drawing)
            {
                target *= s.DrawSpeedFactor;
            }

            //  0에서 멀어지는 쪽, 그리고 0을 지나 반대쪽으로 가는 쪽은 가속. 0 쪽으로 줄이는 건 감속.
            bool speedingUp = target * current.Speed < 0f || Mathf.Abs(target) > Mathf.Abs(current.Speed);
            float change = speedingUp ? s.Acceleration : s.Deceleration;
            float speed = Mathf.MoveTowards(current.Speed, target, change * dt);

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
