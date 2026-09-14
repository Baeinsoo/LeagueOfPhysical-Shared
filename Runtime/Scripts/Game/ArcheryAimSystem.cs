using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 입력을 읽어 조준 상태를 갱신하고, 손을 뗀 틱에 화살 한 발을 만든다.
    /// 무상태다 — 상태는 <see cref="ArcheryAim"/>에 있다.
    /// </summary>
    public class ArcheryAimSystem
    {
        /// <summary>살짝 당겼을 때의 화살 속도(m/s). 느리고 크게 휜다.</summary>
        public const float MinSpeed = 25f;

        /// <summary>끝까지 당겼을 때의 화살 속도(m/s). 빠르고 곧게 간다.</summary>
        public const float MaxSpeed = 65f;

        /// <summary>이만큼 당기면 최대다. 더 당겨도 세지지 않는다.</summary>
        public const float FullDrawSeconds = 0.8f;

        /// <summary>
        /// 이만큼은 끌어야 시위가 걸린다(0~1). 못 미치면 쏘지 않고 취소다 — 화면을 스치기만 해도
        /// 화살이 나가면 조준하다 실수로 쏘게 된다.
        /// </summary>
        public const float DrawThreshold = 0.15f;

        /// <summary>화살이 떠나는 높이 — 발밑이 아니라 눈높이에서 나가야 겨눈 대로 간다.</summary>
        public const float EyeHeight = 1.4f;

        /// <summary>이만큼 당기고 있었다(초). 화면은 정수 틱 사이도 물어보므로 소수 틱을 받는다.</summary>
        public static float HeldSeconds(long drawStartTick, double currentTick, float tickInterval)
        {
            return (float)((currentTick - drawStartTick) * tickInterval);
        }

        /// <summary>당긴 정도 0~1. 오래 당겨도 1을 넘지 않는다.</summary>
        public static float DrawRatio(long drawStartTick, long currentTick, float tickInterval)
        {
            return Mathf.Clamp01(HeldSeconds(drawStartTick, currentTick, tickInterval) / FullDrawSeconds);
        }

        public static float SpeedFor(float drawRatio)
        {
            return Mathf.Lerp(MinSpeed, MaxSpeed, Mathf.Clamp01(drawRatio));
        }

        /// <summary>떼는 틱에만 화살을 돌려준다. 나머지 틱은 null이다.</summary>
        public ArcheryShot? Tick(GameFramework.World.Entity entity, long tick, float tickInterval)
        {
            var aim = entity.Get<ArcheryAim>();
            var command = entity.Get<InputBuffer>()?.Current;
            if (aim == null || command == null)
            {
                return null;
            }

            aim.Yaw = command.AimYaw;
            aim.Pitch = command.AimPitch;
            //  당기는 동안에만 갱신한다. 떼는 틱은 Drawing=false로 오므로 여기서 0으로 덮으면
            //  아래에서 "얼마나 당겼는지"를 잃는다 — 마지막으로 당긴 값이 곧 쏘는 힘이다.
            if (command.Drawing)
            {
                aim.DrawRatio = command.DrawRatio;
            }

            // 당기기 시작한 틱은 "안 당기다가 당기기 시작한" 그 틱에만 새로 찍는다.
            if (command.Drawing && aim.Drawing == false)
            {
                aim.DrawStartTick = tick;
            }

            if (command.Release == false)
            {
                aim.Drawing = command.Drawing;
                return null;
            }

            // 당긴 적 없이 뗀 것은 발사가 아니다(손가락이 스친 경우).
            if (aim.Drawing == false)
            {
                return null;
            }

            //  임계치를 못 넘고 뗐으면 취소다. 시위가 걸린 적이 없으니 화살도 없다.
            if (aim.DrawRatio < DrawThreshold)
            {
                aim.Drawing = false;
                aim.DrawRatio = 0f;
                return null;
            }

            float speed = SpeedFor(aim.DrawRatio);
            Vector3 origin = entity.Get<GameFramework.World.Transform>().Position.ToUnity()
                           + new Vector3(0f, EyeHeight, 0f);
            Vector3 velocity = ArcheryTrajectory.DirectionFrom(aim.Yaw, aim.Pitch) * speed;

            aim.Drawing = false;
            aim.DrawRatio = 0f;
            return new ArcheryShot(entity.Id, tick, origin, velocity);
        }
    }
}
