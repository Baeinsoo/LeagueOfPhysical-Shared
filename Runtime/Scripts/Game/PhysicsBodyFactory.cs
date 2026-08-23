using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 엔티티의 물리 몸(Rigidbody + 캡슐 콜라이더)을 만들어 <see cref="UnityPhysicsBody"/>로 감싼다.
    ///
    /// 클·서가 **같은 몸**을 써야 예측과 권위가 어긋나지 않는다 — 그래서 값이 한 곳에만 있다.
    /// 예전엔 클·서에 `PhysicsFollower`라는 MonoBehaviour가 한 벌씩 있었지만, 붙이자마자 rb·콜라이더만
    /// 뽑아 쓰고 아무도 다시 찾지 않는 껍데기였다(이름과 달리 따라가는 일은 MotionBridge가 한다).
    /// </summary>
    public static class PhysicsBodyFactory
    {
        //  캡슐 규격. 클·서가 다르면 같은 입력에도 충돌 결과가 갈린다.
        private const float Radius = 0.35f;
        private const float Height = 1.5f;

        public static UnityPhysicsBody Create(GameObject root, GameFramework.World.Entity worldEntity, bool isKinematic, bool isTrigger)
        {
            var worldTransform = worldEntity.Get<GameFramework.World.Transform>();
            var worldVelocity = worldEntity.Get<GameFramework.World.Velocity>();

            root.layer = LayerMask.NameToLayer("Character");

            //  루트(시뮬 바디)를 스폰 위치에 즉시 놓는다. kinematic rb의 rb.position은 다음 물리 스텝에야
            //  트랜스폼에 반영돼, 루트가 한 틱 원점에 머물다 점프하면 자식 모델이 끌려가 첫 틱에 순간이동한다.
            root.transform.SetPositionAndRotation(worldTransform.Position.ToUnity(), worldTransform.Rotation.ToUnity());

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.linearDamping = 0f;   //  수평 정지는 이동 모터가 0으로 제동한다. 수직은 순수 중력.
            rigidbody.angularDamping = 0.05f;
            rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.position = worldTransform.Position.ToUnity();
            rigidbody.rotation = worldTransform.Rotation.ToUnity();
            rigidbody.linearVelocity = worldVelocity.Linear.ToUnity();
            rigidbody.isKinematic = isKinematic;

            var collider = root.AddComponent<CapsuleCollider>();
            collider.radius = Radius;
            collider.height = Height;
            collider.center = new Vector3(0, Height * 0.5f, 0);
            collider.isTrigger = isTrigger;

            return new UnityPhysicsBody(rigidbody, collider);
        }
    }
}
