using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 키네마틱 캐릭터 이동: 중력(수직 가속)을 속도에 더한 뒤 collide-and-slide 커널로 위치를 낸다.
    /// World.Transform/Velocity(진실원본)에 쓴다. 클·서 공유 — 호스트가 캐릭터 엔티티에 대해 호출한다.
    /// 물리 쿼리는 ICollisionQuery 포트 뒤. mover 커널은 중력을 모른다 — 중력은 여기서만(분리된 수직 스텝).
    /// </summary>
    public class KinematicMoveSystem
    {
        const float Gravity = -9.81f * 2f;   // 서버 Physics.gravity.y와 같은 값 유지(낙하 가속)
        const float Radius = 0.35f;          // PhysicsComponent 캡슐과 일치
        const float Height = 1.5f;

        private readonly ICollisionQuery _query;
        private readonly int _layerMask;

        public KinematicMoveSystem(ICollisionQuery query, int layerMask)
        {
            _query = query;
            _layerMask = layerMask;
        }

        public void Tick(GameFramework.World.Entity entity, float deltaTime)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            if (transform == null || velocity == null)
            {
                return;
            }

            Vector3 vel = velocity.Linear.ToUnity();
            vel.y += Gravity * deltaTime;   // 중력 = 분리된 수직 스텝(컨트롤러 레이어). mover는 이걸 모름.

            var result = KinematicMover.Move(new KinematicMoveInput(
                transform.Position.ToUnity(), vel, Radius, Height, deltaTime, _layerMask), _query);

            transform.Position = result.position.ToNumerics();
            velocity.Linear = result.velocity.ToNumerics();
        }
    }
}
