using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 지금 떠 있는 과녁 하나. <b>엔티티가 아니다</b> — 화살과 같은 취급으로, 씨앗과 웨이브 번호만
    /// 있으면 양쪽이 각자 계산해 낸다. <see cref="WaveIndex"/>+<see cref="SlotIndex"/>가 이름 노릇을
    /// 해서 "어느 과녁이 먹혔다"를 그 두 숫자로 가리킬 수 있다.
    /// </summary>
    public readonly struct ArcheryTarget
    {
        public readonly int WaveIndex;
        public readonly int SlotIndex;
        public readonly Vector3 Center;
        public readonly float Radius;
        public readonly int Points;

        public ArcheryTarget(int waveIndex, int slotIndex, Vector3 center, float radius, int points)
        {
            WaveIndex = waveIndex;
            SlotIndex = slotIndex;
            Center = center;
            Radius = radius;
            Points = points;
        }
    }
}
