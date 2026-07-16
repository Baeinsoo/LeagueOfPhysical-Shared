using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// World 모션을 Unity 물리 바디에 반영하는 공유 브릿지(포트 구현 1개 — UnityCollisionQuery와 동형).
    /// 겹침 해소는 2패스: Depenetrate(지형) + Separate(캐릭터). 둘 다 full로 밀어냄 — 캐릭터는 서로 통과
    /// 못 하는 단단한 벽이고, 클·서가 동일해야 예측이 맞아 recon이 작다(soft 분리는 넷코드상 불가로 폐기).
    /// (배율 param은 seam으로 남겨둠 — 현재 클·서 모두 1.0.)
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
            // 캐릭터 겹침에서 캡슐을 밖으로 — 캐릭터는 벽이라 full로 빠져나옴(양쪽 1.0). 클·서 동일 = 예측 일치.
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
