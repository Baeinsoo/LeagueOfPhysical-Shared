using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 지금 솟아오르는 과녁 하나. <b>엔티티가 아니다</b> — 화살과 같은 취급으로, 씨앗과 웨이브 번호,
    /// 판이 시작한 틱(<c>gameplayStartTick</c>)만 있으면 양쪽이 각자 계산해 낸다.
    /// <see cref="WaveIndex"/>+<see cref="SlotIndex"/>가 이름 노릇을 해서 "어느 과녁이 먹혔다"를
    /// 그 두 숫자로 가리킬 수 있다.
    /// </summary>
    public readonly struct ArcheryTarget
    {
        public readonly int WaveIndex;
        public readonly int SlotIndex;

        /// <summary>솟기 시작하는 자리(가운데 무대 언저리). 좌우로는 안 움직이므로 x·z는 내내 이 값이다.</summary>
        public readonly Vector3 Origin;

        /// <summary>솟기 시작하는 속도(m/s). 높이와 수명에서 역산한다 — <see cref="ArcheryTargetMotion.RiseSpeedFor"/>.</summary>
        public readonly float RiseSpeed;

        /// <summary>솟기 시작하는 절대 틱. 묶음 안에서 슬롯마다 다르다(연달아 솟는다).</summary>
        public readonly long SpawnTick;

        public readonly float Radius;
        public readonly int Points;

        /// <summary>맞히면 안 되는 과녁인가.</summary>
        public readonly bool IsTrap;

        /// <summary>공인가 판인가. 판정하는 법이 여기서 갈린다.</summary>
        public readonly ArcheryTargetShape Shape;

        /// <summary>
        /// 중심에서 바깥으로 가는 띠 목록. 비어 있으면 <see cref="Points"/>짜리 띠 하나로 친다.
        /// </summary>
        public readonly IReadOnlyList<ArcheryRingBand> Bands;

        /// <summary>
        /// 판이 바라보는 쪽(단위 벡터). 공은 이 값을 안 쓴다.
        /// 이쪽에서 오는 화살만 맞는다 — 뒤에서 온 것은 통과한다.
        /// </summary>
        public readonly Vector3 Facing;

        /// <summary>
        /// 솟았다 떨어지기까지의 시간, 또는 서 있다 사라지기까지의 시간(초).
        /// <b>파생값이 아니라 값이다</b> — 사거리 과녁은 솟지 않으므로(속도 0) 속도에서 수명을
        /// 유도하면 서자마자 사라진다. 웨이브 과녁은 <see cref="ArcheryTargetMotion.LifetimeFor"/>가
        /// 예전과 같은 값을 채운다.
        /// </summary>
        public readonly float LifetimeSeconds;

        /// <summary>
        /// 이 과녁의 주인(userId). <b>빈 문자열이면 주인이 없다</b> — 먼저 맞힌 사람이 먹는다(원형 맵).
        /// 주인이 있으면 주인이 맞혔을 때만 점수가 나고 그때만 사라진다.
        /// </summary>
        public readonly string OwnerUserId;

        public ArcheryTarget(int waveIndex, int slotIndex, Vector3 origin, float riseSpeed, long spawnTick,
                             float radius, int points, bool isTrap,
                             ArcheryTargetShape shape, IReadOnlyList<ArcheryRingBand> bands, Vector3 facing,
                             float lifetimeSeconds, string ownerUserId)
        {
            WaveIndex = waveIndex;
            SlotIndex = slotIndex;
            Origin = origin;
            RiseSpeed = riseSpeed;
            SpawnTick = spawnTick;
            Radius = radius;
            Points = points;
            IsTrap = isTrap;
            Shape = shape;
            Bands = bands;
            Facing = facing;
            LifetimeSeconds = lifetimeSeconds;
            OwnerUserId = ownerUserId ?? string.Empty;
        }
    }
}
