using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 입력과 당김을 읽어 탈것에 탄 몸의 머리 방향·속도를 갱신한다. 무상태다.
    /// 머리는 Transform 회전, 속도는 Velocity에서 읽고 거기에 다시 쓴다 — 되감기가 이 둘만
    /// 복원하므로 따로 들고 있는 값이 있으면 되감을 때 어긋난다.
    /// </summary>
    public class MountSystem
    {
        private readonly MountSettings settings;

        public MountSystem(MountSettings settings)
        {
            this.settings = settings;
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var mount = entity.Get<Mount>();
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var command = entity.Get<InputBuffer>()?.Current;
            if (mount == null || transform == null || velocity == null || command == null)
            {
                return;
            }

            float heading = Mathf.DeltaAngle(0f, transform.Rotation.ToUnity().eulerAngles.y);
            var linear = velocity.Linear.ToUnity();
            //  머리 방향으로의 성분을 그대로 쓴다 — 크기만 쓰면 후진 중인 걸 앞으로 착각한다.
            float rad = heading * Mathf.Deg2Rad;
            float speed = linear.x * Mathf.Sin(rad) + linear.z * Mathf.Cos(rad);

            var next = MountMotor.Step(new MountState(heading, speed),
                new Vector2(command.Horizontal, command.Vertical), command.Drawing, settings, deltaTime);

            transform.Rotation = Quaternion.Euler(0f, next.HeadingDeg, 0f).ToNumerics();
            var v = MountMotor.VelocityOf(next);
            //  세로 속도는 중력·점프 쪽 몫이라 건드리지 않는다.
            velocity.Linear = new Vector3(v.x, linear.y, v.z).ToNumerics();
        }
    }
}
