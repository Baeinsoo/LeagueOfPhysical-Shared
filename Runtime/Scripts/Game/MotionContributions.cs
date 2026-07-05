using System.Collections.Generic;
using GameFramework.World;

namespace LOP
{
    /// <summary>엔티티에 얹힌 외부 이동 기여(넉백·외력) 컬렉션(데이터 컴포넌트). 프루닝/해소는 <see cref="MotionContributionSystem"/>.</summary>
    public class MotionContributions : Component
    {
        public List<MotionContribution> Items { get; } = new List<MotionContribution>();
    }
}
