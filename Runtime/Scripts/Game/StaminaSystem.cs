namespace LOP
{
    /// <summary>
    /// 활공 자원의 소모·회복. 젤다 규칙 그대로 — 자유낙하는 공짜, 패러세일만 먹고,
    /// 회복은 발 딛고 있을 때만. 잔고가 0이어도 "마지막 한 번"은 펼 수 있다(착지 직전 구제).
    /// </summary>
    public class StaminaSystem
    {
        public void Tick(GameFramework.World.Entity entity, float deltaTime,
                         in SkydiveConfig config, bool grounded)
        {
            var stamina = entity.Get<Stamina>();
            var posture = entity.Get<Posture>();
            if (stamina == null || posture == null)
            {
                return;
            }

            // 비상 펼침 중이면 잔고가 아니라 남은 시간이 줄고, 다 되면 접힌다.
            if (stamina.EmergencyRemaining > 0f)
            {
                stamina.EmergencyRemaining -= deltaTime;
                if (stamina.EmergencyRemaining <= 0f)
                {
                    stamina.EmergencyRemaining = 0f;
                    posture.Gliding = false;
                }
                return;
            }

            if (posture.Gliding)
            {
                stamina.Current -= config.GlideDrain * deltaTime;
                if (stamina.Current <= 0f)
                {
                    stamina.Current = 0f;
                    posture.Gliding = false;   // 손에서 놓아진다
                }
                return;
            }

            if (grounded)
            {
                stamina.Current += config.GroundRecover * deltaTime;
                if (stamina.Current > config.StaminaMax)
                {
                    stamina.Current = config.StaminaMax;
                }
            }
            // 공중에서 안 펴고 있으면 아무 일도 없다 — 젤다도 공중에선 안 찬다.
        }

        /// <summary>
        /// 패러세일을 펴려는 시도. 잔고가 있으면 그냥 펴고, 0이면 "마지막 한 번"을 쓴다.
        /// 이미 그 한 번을 썼으면 거절한다.
        /// </summary>
        public bool TryStartGlide(GameFramework.World.Entity entity, in SkydiveConfig config)
        {
            var stamina = entity.Get<Stamina>();
            var posture = entity.Get<Posture>();
            if (stamina == null || posture == null)
            {
                return false;
            }

            if (stamina.Current > 0f)
            {
                posture.Gliding = true;
                return true;
            }

            if (stamina.EmergencyUsed)
            {
                return false;
            }

            stamina.EmergencyUsed = true;
            stamina.EmergencyRemaining = config.EmergencyGlideTime;
            posture.Gliding = true;
            return true;
        }
    }
}
