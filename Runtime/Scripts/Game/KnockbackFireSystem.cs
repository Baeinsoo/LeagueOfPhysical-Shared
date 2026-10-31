using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 쏠 수 있는지 정하고 탄 수·연사 간격·반동을 처리한다. 탄을 장부에 넣고 사건을 내는 일은 월드가 맡는다.
    /// </summary>
    public sealed class KnockbackFireSystem
    {
        // 탄이 몸 안에서 시작하지 않게 몸 앞으로 이만큼 내민다.
        const float MuzzleOffset = 0.7f;

        readonly KnockbackConfig config;

        public KnockbackFireSystem(KnockbackConfig config)
        {
            this.config = config;
        }

        public bool TryFire(GameFramework.World.Entity body, long tick, out KnockbackShot shot)
        {
            shot = default;
            var gun = body.Get<KnockbackGun>();
            var input = body.Get<InputBuffer>()?.Current;
            var transform = body.Get<GameFramework.World.Transform>();
            var velocity = body.Get<GameFramework.World.Velocity>();
            if (body.Get<GameFramework.World.Simulated>() == null || gun == null || input == null
                || transform == null || velocity == null)
            {
                return false;
            }
            if (!input.Fire || gun.Ammo < 1 || tick < gun.CooldownEndTick)
            {
                return false;
            }

            float yaw = input.AimYaw * Mathf.Deg2Rad;
            var direction = new Vector2(Mathf.Sin(yaw), Mathf.Cos(yaw));
            var origin = new Vector2(transform.Position.X, transform.Position.Z) + direction * MuzzleOffset;
            shot = new KnockbackShot(body.Id, tick, origin, direction);

            // 가득 차 있다가 처음 줄어든 순간에만 충전 시계를 시작한다(이미 돌고 있으면 그대로).
            if (gun.Ammo == config.AmmoMax)
            {
                gun.NextReloadTick = tick + config.Ticks(config.ReloadSeconds);
            }
            gun.Ammo--;
            gun.CooldownEndTick = tick + config.Ticks(config.FireCooldownSeconds);

            var linear = velocity.Linear;
            linear.X -= direction.x * config.Recoil;
            linear.Z -= direction.y * config.Recoil;
            velocity.Linear = linear;
            return true;
        }

        public void Reload(GameFramework.World.Entity body, long tick)
        {
            var gun = body.Get<KnockbackGun>();
            if (gun == null || tick < gun.NextReloadTick)
            {
                return;
            }
            gun.Ammo++;
            gun.NextReloadTick = gun.Ammo >= config.AmmoMax
                ? long.MaxValue
                : gun.NextReloadTick + config.Ticks(config.ReloadSeconds);
        }
    }
}
