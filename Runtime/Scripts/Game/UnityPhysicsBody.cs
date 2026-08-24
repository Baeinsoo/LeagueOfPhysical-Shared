using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// <see cref="GameFramework.World.PhysicsBody"/> 포트의 Unity 어댑터 — 엔티티의 물리 몸(Rigidbody·캡슐 콜라이더)을 감싼다.
    /// 코어는 추상 PhysicsBody만 알고, 실제 rb/collider 조작은 여기서 한다. 뷰 스포너(EntityBinder)가 스폰 시 부착.
    /// </summary>
    public class UnityPhysicsBody : GameFramework.World.PhysicsBody
    {
        private readonly Rigidbody _rigidbody;
        private readonly Collider _collider;

        public UnityPhysicsBody(Rigidbody rigidbody, Collider collider)
        {
            _rigidbody = rigidbody;
            _collider = collider;
        }

        public override bool IsKinematic => _rigidbody != null && _rigidbody.isKinematic;

        public override void SetPosition(System.Numerics.Vector3 position)
        {
            if (_rigidbody != null)
            {
                _rigidbody.position = position.ToUnity();
            }
        }

        public override void SetRotation(System.Numerics.Quaternion rotation)
        {
            if (_rigidbody != null)
            {
                _rigidbody.rotation = rotation.ToUnity();
            }
        }

        public override void SetVelocity(System.Numerics.Vector3 linear)
        {
            if (_rigidbody != null)
            {
                _rigidbody.linearVelocity = linear.ToUnity();
            }
        }

        public override System.Numerics.Vector3 GetPosition()
        {
            return _rigidbody == null ? System.Numerics.Vector3.Zero : _rigidbody.position.ToNumerics();
        }

        public override System.Numerics.Quaternion GetRotation()
        {
            return _rigidbody == null ? System.Numerics.Quaternion.Identity : _rigidbody.rotation.ToNumerics();
        }

        public override System.Numerics.Vector3 GetVelocity()
        {
            return _rigidbody == null ? System.Numerics.Vector3.Zero : _rigidbody.linearVelocity.ToNumerics();
        }

        public override System.Numerics.Vector3 ComputePushOut(int layerMask)
        {
            // 밀어내기는 캡슐 몸(우리 키네마틱 컨트롤러) 전용 — 원반 등 다른 콜라이더는 대상이 아니다.
            if (_collider is not CapsuleCollider capsule)
            {
                return System.Numerics.Vector3.Zero;
            }
            return KinematicDepenetration.ComputePushOut(capsule, layerMask).ToNumerics();
        }
    }
}
