using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 한 어빌리티 발동(공격)에서 "명중한(닷지 안 된) 대상 id 집합". 히트 정의자(데미지)가 기록하고
    /// on-hit 라이더(넉백 등)가 읽는다 — 효과들이 같은 히트 결과를 공유하게 하는 발동당 컨텍스트.
    /// </summary>
    public sealed class AttackHitContext
    {
        private readonly HashSet<string> _landed = new HashSet<string>();

        public void MarkLanded(string targetId) => _landed.Add(targetId);
        public bool Landed(string targetId) => _landed.Contains(targetId);
        public IReadOnlyCollection<string> LandedTargets => _landed;
    }
}
