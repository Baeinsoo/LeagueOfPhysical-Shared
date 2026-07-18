using GameFramework.World;

namespace LOP
{
    /// <summary>엔티티 종류(Character/Item 등). 스폰 데이터 디스패치·"캐릭터냐" 판별용.</summary>
    public class EntityKind : Component
    {
        public EntityType Kind { get; }

        public EntityKind(EntityType kind)
        {
            Kind = kind;
        }
    }
}
