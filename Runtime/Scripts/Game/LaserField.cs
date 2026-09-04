using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 이 판에 놓인 레이저 전부. 맵 씬의 <see cref="LaserVolume"/> 마커가 로드될 때 스스로 들어온다.
    ///
    /// <para><see cref="WindField"/>와 달리 정렬하지 않는다 — 바람은 겹친 볼륨의 합을 구해야 해서
    /// 순서가 부동소수 합에 새어 들어갔지만, 레이저는 각각 독립으로 판정하므로 순서가 결과를
    /// 바꾸지 않는다.</para>
    /// </summary>
    public class LaserField
    {
        private readonly List<Laser> _lasers = new List<Laser>();

        public IReadOnlyList<Laser> All => _lasers;

        public void Add(Laser laser) => _lasers.Add(laser);

        /// <summary>
        /// 등록했던 레이저 하나를 뺀다. <see cref="Laser"/>가 값이라 참조가 아니라 <b>값으로</b>
        /// 찾는데, 완전히 같은 레이저가 둘이면 어느 쪽을 빼도 결과가 같아 문제되지 않는다.
        /// </summary>
        public bool Remove(in Laser laser) => _lasers.Remove(laser);

        public void Clear() => _lasers.Clear();
    }
}
