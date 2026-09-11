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

        /// <summary>화살이 떠나는 높이 — 발밑이 아니라 눈높이에서 나가야 겨눈 대로 간다.</summary>
        public const float EyeHeight = 1.4f;

        /// <summary>당긴 정도 0~1. 오래 당겨도 1을 넘지 않는다.</summary>
        public static float DrawRatio(long drawStartTick, long currentTick, float tickInterval)
        {
            float seconds = (currentTick - drawStartTick) * tickInterval;
            return Mathf.Clamp01(seconds / FullDrawSeconds);
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

            float speed = SpeedFor(DrawRatio(aim.DrawStartTick, tick, tickInterval));
            Vector3 origin = entity.Get<GameFramework.World.Transform>().Position.ToUnity()
                           + new Vector3(0f, EyeHeight, 0f);
            Vector3 velocity = ArcheryTrajectory.DirectionFrom(aim.Yaw, aim.Pitch) * speed;

            aim.Drawing = false;
            return new ArcheryShot(entity.Id, tick, origin, velocity);
        }
    }
}
