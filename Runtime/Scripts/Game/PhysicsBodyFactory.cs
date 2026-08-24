using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 엔티티의 물리 몸(Rigidbody + 캡슐 콜라이더)을 만들어 <see cref="UnityPhysicsBody"/>로 감싼다.
    ///
    /// 클·서가 **같은 몸**을 써야 예측과 권위가 어긋나지 않는다 — 그래서 캡슐 치수는 엔티티의
    /// <see cref="GameFramework.World.CapsuleShape"/>가 갖고 있고, 이 팩토리는 그 값을 읽어 쓸 뿐 다시
    /// 정하지 않는다. 예전엔 클·서에 `PhysicsFollower`라는 MonoBehaviour가 한 벌씩 있었지만, 붙이자마자
    /// rb·콜라이더만 뽑아 쓰고 아무도 다시 찾지 않는 껍데기였다(이름과 달리 따라가는 일은 MotionBridge가 한다).
    /// </summary>
    public static class PhysicsBodyFactory
    {
        public static UnityPhysicsBody Create(GameObject root, GameFramework.World.Entity worldEntity)
        {
            var config = worldEntity.Get<GameFramework.World.PhysicsConfig>();
            if (config == null)
            {
                // 몸을 어떻게 세울지는 엔티티를 만드는 쪽(게임)이 정한다 — 여기서 기본값을
                // 지어내면 시뮬이 쓰는 몸과 다시 어긋난다(CapsuleShape과 같은 이유).
                throw new System.InvalidOperationException(
                    $"[PhysicsBodyFactory] {worldEntity.Id}에 PhysicsConfig가 없다 — 크리에이터가 붙여야 한다.");
            }

            var capsule = worldEntity.Get<GameFramework.World.CapsuleShape>();
            var disc = worldEntity.Get<GameFramework.World.DiscShape>();
            if (capsule == null && disc == null)
            {
                throw new System.InvalidOperationException(
                    $"[PhysicsBodyFactory] {worldEntity.Id}에 몸 모양이 없다 — CapsuleShape이나 DiscShape을 붙여야 한다.");
            }

            var worldTransform = worldEntity.Get<GameFramework.World.Transform>();
            var worldVelocity = worldEntity.Get<GameFramework.World.Velocity>();

            root.layer = LayerMask.NameToLayer("Character");

            //  루트(시뮬 바디)를 스폰 위치에 즉시 놓는다. kinematic rb의 rb.position은 다음 물리 스텝에야
            //  트랜스폼에 반영돼, 루트가 한 틱 원점에 머물다 점프하면 자식 모델이 끌려가 첫 틱에 순간이동한다.
            root.transform.SetPositionAndRotation(worldTransform.Position.ToUnity(), worldTransform.Rotation.ToUnity());

            var rigidbody = root.AddComponent<Rigidbody>();
            rigidbody.linearDamping = 0f;   //  수평 정지는 이동 모터가 0으로 제동한다. 수직은 순수 중력.
            rigidbody.angularDamping = 0.05f;
            rigidbody.constraints = config.FreezeRotation
                ? RigidbodyConstraints.FreezeRotation
                : RigidbodyConstraints.None;
            rigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
            rigidbody.position = worldTransform.Position.ToUnity();
            rigidbody.rotation = worldTransform.Rotation.ToUnity();
            rigidbody.linearVelocity = worldVelocity.Linear.ToUnity();
            rigidbody.isKinematic = config.Kind != GameFramework.World.BodyKind.Dynamic;

            Collider collider;
            if (disc != null)
            {
                var cylinder = root.AddComponent<CapsuleCollider>();
                cylinder.direction = 1;              // Y축 — 원반은 눕힌 캡슐이 아니라 납작한 기둥이다
                cylinder.radius = disc.Radius;
                cylinder.height = disc.Thickness;
                cylinder.center = Vector3.zero;
                collider = cylinder;
            }
            else
            {
                var capsuleCollider = root.AddComponent<CapsuleCollider>();
                capsuleCollider.radius = capsule.Radius;
                capsuleCollider.height = capsule.Height;
                capsuleCollider.center = new Vector3(0, capsule.Height * 0.5f, 0);
                collider = capsuleCollider;
            }
            collider.isTrigger = config.IsTrigger;

            return new UnityPhysicsBody(rigidbody, collider);
        }
    }
}
