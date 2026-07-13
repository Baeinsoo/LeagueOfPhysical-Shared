using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// World 모션을 Unity 물리 바디에 반영하는 공유 브릿지(포트 구현 1개 — UnityCollisionQuery와 동형).
    /// 엔티티의 공유 <see cref="PhysicsBody"/> 핸들로 겹침해소·rb 반영을 해서 per-side LOPEntity를 안 만진다.
    /// World.Transform이 진실원본, Rigidbody는 팔로워(kinematic이면 위치·회전 직접 밀어넣음).
    /// </summary>
    public class MotionBridge : GameFramework.World.IMotionBridge
    {
        private readonly int _layerMask = LayerMask.GetMask("Default");

        public void SyncTransforms() => Physics.SyncTransforms();

        public void Depenetrate(GameFramework.World.Entity entity)
        {
            var body = entity.Get<PhysicsBody>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (body == null || body.Collider == null || transform == null)
            {
                return;
            }
            // 겹친 지오메트리(스폰 flush 등)에서 캡슐을 밀어냄 → World.Transform에 반영(PushMotion이 rb로 동기).
            Vector3 push = KinematicDepenetration.ComputePushOut(body.Collider, _layerMask);
            if (push.sqrMagnitude > 0f)
            {
                transform.Position = (transform.Position.ToUnity() + push).ToNumerics();
            }
        }

        public void PushMotion(GameFramework.World.Entity entity)
        {
            var body = entity.Get<PhysicsBody>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (body == null || body.Rigidbody == null || transform == null)
            {
                return;
            }
            Rigidbody rb = body.Rigidbody;
            // kinematic 바디(캐릭터)는 velocity를 못 받는다 → World 위치·회전을 rb에 직접 밀어넣는다.
            if (rb.isKinematic)
            {
                rb.position = transform.Position.ToUnity();
                rb.rotation = transform.Rotation.ToUnity();
                return;
            }
            var velocity = entity.Get<GameFramework.World.Velocity>();
            if (velocity != null)
            {
                rb.linearVelocity = velocity.Linear.ToUnity();
            }
            rb.rotation = transform.Rotation.ToUnity();
        }
    }
}
