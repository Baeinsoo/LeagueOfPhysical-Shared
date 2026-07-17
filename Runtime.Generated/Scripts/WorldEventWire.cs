using GameFramework.World;

// hand-written(생성물 아님). generate_protos.sh는 Protobuf/·MessageInitializer만 지우므로 이 파일은 보존됨.
namespace LOP
{
    /// <summary>
    /// 연출 WorldEvent ↔ 와이어(WorldEventToC oneof) 순수 변환. 클·서 공유 — 같은 매핑이라 drift 없음.
    /// 데미지/발동 같은 transient 연출만 다룬다(durable HP 등은 스냅샷 소관, 여기 없음).
    /// </summary>
    public static class WorldEventWire
    {
        /// <summary>연출 WorldEvent를 oneof 와이어 레코드로. 매핑 없는 타입은 null(서버가 무시).</summary>
        public static WorldEventToC ToWire(WorldEvent e)
        {
            switch (e)
            {
                case DamageDealtEvent d:
                    return new WorldEventToC
                    {
                        Damage = new DamageEventToC
                        {
                            AttackerId = d.attackerId,
                            TargetId   = d.targetId,
                            ActionCode = "attack",
                            DamageType = "physical",
                            Damage     = d.amount,
                            IsCritical = d.isCritical,
                            IsDodged   = d.isDodged,
                            IsBlocked  = false,
                        }
                    };
                case AbilityActivatedEvent a:
                    return new WorldEventToC
                    {
                        AbilityActivated = new AbilityActivatedToC
                        {
                            EntityId  = a.entityId,
                            AbilityId = a.abilityId,
                        }
                    };
                default:
                    return null;
            }
        }

        /// <summary>oneof 와이어 레코드를 연출 WorldEvent로. 미인식 case는 null(클라가 무시).</summary>
        public static WorldEvent FromWire(WorldEventToC rec)
        {
            switch (rec.EventCase)
            {
                case WorldEventToC.EventOneofCase.Damage:
                    return new DamageDealtEvent(
                        targetId:   rec.Damage.TargetId,
                        attackerId: rec.Damage.AttackerId,
                        amount:     (int)rec.Damage.Damage,
                        isCritical: rec.Damage.IsCritical,
                        isDodged:   rec.Damage.IsDodged);
                case WorldEventToC.EventOneofCase.AbilityActivated:
                    return new AbilityActivatedEvent(
                        rec.AbilityActivated.EntityId,
                        rec.AbilityActivated.AbilityId);
                default:
                    return null;
            }
        }
    }
}
