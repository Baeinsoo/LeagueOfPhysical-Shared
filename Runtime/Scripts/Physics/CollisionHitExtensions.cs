using UnityEngine;

namespace LOP
{
    /// <summary>물리 히트에서 게임 쪽 신원을 되짚는다. 물리 계층은 엔티티를 알지 않는다.</summary>
    public static class CollisionHitExtensions
    {
        /// <summary>맞은 몸의 엔티티 id. 엔티티가 아닌 것(판·지형)을 맞았으면 null.</summary>
        public static string GetEntityId(this GameFramework.Physics.CollisionHit hit)
        {
            if (hit.Collider == null)
            {
                return null;
            }

            EntityActor actor = hit.Collider.GetComponentInParent<EntityActor>();
            return actor != null ? actor.entityId : null;
        }
    }
}
