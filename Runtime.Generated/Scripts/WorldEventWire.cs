using GameFramework.World;
using UnityEngine;

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
                case ArcheryShotFiredEvent s:
                    return new WorldEventToC
                    {
                        ArcheryShot = new ArcheryShotToC
                        {
                            ShooterId = s.shooterId,
                            FireTick  = s.fireTick,
                            Origin    = new ProtoVector3 { X = s.origin.x, Y = s.origin.y, Z = s.origin.z },
                            Velocity  = new ProtoVector3 { X = s.velocity.x, Y = s.velocity.y, Z = s.velocity.z },
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
                case WorldEventToC.EventOneofCase.ArcheryShot:
                    return new ArcheryShotFiredEvent(
                        shooterId: rec.ArcheryShot.ShooterId,
                        fireTick:  rec.ArcheryShot.FireTick,
                        origin:    new Vector3(rec.ArcheryShot.Origin.X, rec.ArcheryShot.Origin.Y, rec.ArcheryShot.Origin.Z),
                        velocity:  new Vector3(rec.ArcheryShot.Velocity.X, rec.ArcheryShot.Velocity.Y, rec.ArcheryShot.Velocity.Z));
                default:
                    return null;
            }
        }
    }
}
