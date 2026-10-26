using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 넉백 생존전의 몸 하나 움직이기. 수평은 기존 충돌 커널(다른 몸·장애물)에 맡기고,
    /// 수직은 "발밑에 칸이 있나"를 계산으로 정한다 — 칸이 없으면 중력으로 떨어진다.
    /// </summary>
    public sealed class KnockbackMoveSystem
    {
        const float Gravity = -9.81f * 2f;   // KinematicMoveSystem과 같은 값
        // 이만큼보다 아래로 내려간 몸은 칸 위를 지나가도 다시 서지 않는다(떨어지는 중).
        const float StandMinY = -0.05f;
        // 이보다 느리게 미끄러질 땐 몸을 돌리지 않는다(멈추는 중에 방향이 흔들리지 않게).
        const float FaceMinSpeed = 0.8f;

        readonly KnockbackFloor floor;
        readonly ICollisionQuery query;
        readonly int layerMask;
        readonly float moveSpeed;
        readonly float moveGain;

        public KnockbackMoveSystem(KnockbackFloor floor, ICollisionQuery query, int layerMask, float moveSpeed, float moveGain)
        {
            this.floor = floor;
            this.query = query;
            this.layerMask = layerMask;
            this.moveSpeed = moveSpeed;
            this.moveGain = moveGain;
        }

        public void Tick(GameFramework.World.Entity entity, long tick, float dt)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var body = entity.Get<GameFramework.World.CapsuleShape>();
            var ground = entity.Get<GameFramework.World.GroundState>();
            if (transform == null || velocity == null || body == null || ground == null)
            {
                return;
            }

            Vector3 pos = transform.Position.ToUnity();
            Vector3 vel = velocity.Linear.ToUnity();

            if (ground.IsGrounded)
            {
                var input = entity.Get<InputBuffer>()?.Current;
                var horizontal = new Vector2(input?.Horizontal ?? 0f, input?.Vertical ?? 0f);
                var next = KnockbackMotor.Step(new Vector2(vel.x, vel.z), horizontal, moveSpeed, moveGain, dt);
                vel.x = next.x;
                vel.z = next.y;
            }

            var moved = KinematicMover.Move(new KinematicMoveInput(
                pos, new Vector3(vel.x, 0f, vel.z), body.Radius, body.Height, dt, layerMask,
                stepOffset: 0f, groundProbe: 0f), query);

            float x = moved.position.x;
            float z = moved.position.z;
            float y = pos.y;
            float vy = vel.y;

            bool standing = y >= StandMinY && floor.IsSolid(x, z, tick) && vy <= 0f;
            if (standing)
            {
                y = 0f;
                vy = 0f;
            }
            else
            {
                vy += Gravity * dt;
                y += vy * dt;
            }

            ground.IsGrounded = standing;
            float hx = moved.velocity.x;
            float hz = moved.velocity.z;
            if (hx * hx + hz * hz > FaceMinSpeed * FaceMinSpeed)
            {
                float yaw = Mathf.Atan2(hx, hz) * Mathf.Rad2Deg;
                transform.Rotation = Quaternion.Euler(0f, yaw, 0f).ToNumerics();
            }
            transform.Position = new Vector3(x, y, z).ToNumerics();
            velocity.Linear = new Vector3(moved.velocity.x, vy, moved.velocity.z).ToNumerics();
        }
    }
}
