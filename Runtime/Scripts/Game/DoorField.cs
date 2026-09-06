using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 이 판에 놓인 문 전부. 맵 씬의 <see cref="DoorVolume"/> 마커가 로드될 때 스스로 들어온다.
    ///
    /// <para><see cref="Laser"/>/<see cref="LaserField"/>와 달리 <see cref="DoorVolume"/>은 값이
    /// 아니라 MonoBehaviour <b>참조</b>로 담는다 — 뒤 태스크에서 시뮬이 매 틱 그 패널 트랜스폼을
    /// 직접 옮겨야 하고(레이저는 그럴 필요가 없다), 서버는 <see cref="DoorVolume.ToDoor"/>로
    /// 판정 데이터만 뽑아 쓴다.</para>
    /// </summary>
    public class DoorField
    {
        private readonly List<DoorVolume> _doors = new List<DoorVolume>();

        public IReadOnlyList<DoorVolume> All => _doors;

        /// <summary>같은 문을 두 번 넣어도 하나만 남는다(재등록 방지 — WindField와 같은 이유).</summary>
        public void Add(DoorVolume door)
        {
            if (door == null || _doors.Contains(door))
            {
                return;
            }

            _doors.Add(door);
        }

        public void Remove(DoorVolume door) => _doors.Remove(door);

        public void Clear() => _doors.Clear();
    }
}
