using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 엔티티의 몸에 붙는 신원표. 물리 쿼리가 콜라이더를 돌려줬을 때 "이게 어느 엔티티냐"를
    /// 되찾는 실마리다. 클·서가 각자 더 붙일 것이 있어(클라는 뷰) 이 타입을 상속해서 쓴다.
    /// </summary>
    public class EntityActor : MonoBehaviour
    {
        public string entityId { get; private set; }

        public void SetEntityId(string entityId)
        {
            this.entityId = entityId;
        }
    }
}
