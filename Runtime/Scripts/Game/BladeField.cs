using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 맵에 놓인 도는 날개들의 목록. <see cref="LaserField"/>와 같은 자리·같은 수명이다.
    ///
    /// <para>⚠️ <b>실험용(스파이크)</b> — 곡면 날개가 사람을 미는지 보려고 만든 것이다.</para>
    /// </summary>
    public class BladeField
    {
        private readonly List<SpinningBlade> blades = new List<SpinningBlade>();

        public IReadOnlyList<SpinningBlade> All => blades;

        public void Add(SpinningBlade blade)
        {
            if (blade != null && blades.Contains(blade) == false)
            {
                blades.Add(blade);
            }
        }

        public void Remove(SpinningBlade blade) => blades.Remove(blade);
    }
}
