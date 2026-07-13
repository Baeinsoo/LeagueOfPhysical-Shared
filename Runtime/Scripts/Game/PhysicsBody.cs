using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 엔티티의 물리 핸들(Rigidbody·캡슐 콜라이더)을 든 컴포넌트. host 창조자(per-side CharacterCreator)가
    /// 스폰 시 부착하고, 공유 <see cref="KinematicHostSystem"/>이 이 핸들로 겹침해소·rb 반영을 한다.
    /// (LOP-Shared는 UnityEngine 참조 가능 — Rigidbody/Collider는 클·서 공통 Unity 타입이라 여기 둘 수 있다.
    ///  per-side였던 이유는 옛 브릿지가 side별 LOPEntity/PhysicsComponent를 만졌기 때문 — 핸들만 공유로 올려 해소.)
    /// </summary>
    public class PhysicsBody : GameFramework.World.Component
    {
        public Rigidbody Rigidbody { get; }
        public CapsuleCollider Collider { get; }

        public PhysicsBody(Rigidbody rigidbody, CapsuleCollider collider)
        {
            Rigidbody = rigidbody;
            Collider = collider;
        }
    }
}
