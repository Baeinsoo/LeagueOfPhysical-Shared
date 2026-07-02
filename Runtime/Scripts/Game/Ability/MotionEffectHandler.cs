using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// <see cref="MotionEffect"/> 핸들러(코어). Active 동안 매 틱 시전자가 바라보는 방향으로 그 속도로 민다(대시).
    /// World.Velocity(진실원본)에 직접 쓴다 → 호스트 velocity 브릿지가 Rigidbody에 반영.
    /// 엔진(Rigidbody)·LOPEntity·EntityManager 비참조 = 클·서 공유 1벌.
    /// </summary>
    public class MotionEffectHandler : AbilityEffectHandler<MotionEffect>
    {
        protected override void OnActiveTick(AbilityEffectContext ctx, MotionEffect effect)
        {
            var worldTransform = ctx.Caster.Get<GameFramework.World.Transform>();
            var worldVelocity = ctx.Caster.Get<GameFramework.World.Velocity>();
            if (worldTransform == null || worldVelocity == null)
            {
                return;
            }

            // 바라보는 방향(수평)으로 speed만큼 가도록 World.velocity를 맞춘다(Y 보존).
            Vector3 forward = worldTransform.Rotation.ToUnity() * Vector3.forward;
            Vector3 target = new Vector3(forward.x, 0f, forward.z).normalized * effect.Speed;

            Vector3 v = worldVelocity.Linear.ToUnity();
            v.x = target.x;
            v.z = target.z;
            worldVelocity.Linear = v.ToNumerics();
        }
    }
}
