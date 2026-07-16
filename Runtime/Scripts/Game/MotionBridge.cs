using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// World 모션을 Unity 물리 바디에 반영하는 공유 브릿지(포트 구현 1개 — UnityCollisionQuery와 동형).
    /// 겹침 해소는 2패스: Depenetrate(지형 full) + Separate(캐릭터 reciprocal, per-side 배율).
    /// World.Transform이 진실원본, Rigidbody는 팔로워(kinematic이면 위치·회전 직접 밀어넣음).
    /// </summary>
    public class MotionBridge : GameFramework.World.IMotionBridge
    {
        private readonly int _envMask;
        private readonly int _charMask;
        private readonly float _separationScale;

        public MotionBridge(int envMask, int charMask, float separationScale)
        {
            _envMask = envMask;
            _charMask = charMask;
            _separationScale = separationScale;
        }

        public void SyncTransforms() => Physics.SyncTransforms();

        public void Depenetrate(GameFramework.World.Entity entity)
        {
            // 지형 겹침(스폰 flush 등)에서 캡슐을 밖으로 — 지면은 안 움직이니 전부 해소.
            ApplyPushOut(entity, _envMask, 1f);
        }

        public void Separate(GameFramework.World.Entity entity)
        {
            // 캐릭터끼리 부드럽게 밀어냄. 배율=per-side(서버 0.5 상호분리 / 클라 1.0 내가 다 빠짐).
            ApplyPushOut(entity, _charMask, _separationScale);
        }

        private void ApplyPushOut(GameFramework.World.Entity entity, int layerMask, float scale)
        {
            var body = entity.Get<PhysicsBody>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (body == null || body.Collider == null || transform == null)
            {
                return;
            }
            Vector3 push = KinematicDepenetration.ComputePushOut(body.Collider, layerMask);
            if (push.sqrMagnitude > 0f)
            {
                transform.Position = (transform.Position.ToUnity() + push * scale).ToNumerics();
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
