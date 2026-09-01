using System.Collections.Generic;

namespace LOP
{
    /// <summary>한 명이 결승선에 닿은 기록.</summary>
    public readonly struct FinishRecord
    {
        public string EntityId { get; }

        /// <summary>처음 닿은 틱. 작을수록 앞선다.</summary>
        public long Tick { get; }

        /// <summary>그 틱에 결승선을 넘어간 깊이. 같은 틱이면 <b>깊을수록</b> 먼저 들어온 것이다.</summary>
        public float Past { get; }

        public FinishRecord(string entityId, long tick, float past)
        {
            EntityId = entityId;
            Tick = tick;
            Past = past;
        }

        /// <summary>등수가 같은가 — 틱도 깊이도 같으면 진짜 동점이다.</summary>
        public bool SameRankAs(FinishRecord other) => Tick == other.Tick && Past == other.Past;
    }

    /// <summary>
    /// 결승선에 닿은 순서를 재는 추적기. 물리도 엔티티도 좌표축도 모르고, 매 틱
    /// "결승선을 얼마나 넘었나"(<see cref="FinishLineOverlap.Past"/>, 음수 = 아직)만 받아 답한다.
    ///
    /// <para><b>같은 틱에 닿은 둘을 깊이로 가른다.</b> 틱만 세면 앞뒤를 못 가려 목록 순서
    /// (사실상 스폰 순서)가 등수를 정하는데, Flappy는 모든 새가 같은 속도로 달려 같은 틱 통과가
    /// <b>기본값</b>이라 판마다 그 일이 벌어진다. 더 깊이 넘어가 있다는 것은 그만큼 먼저 닿았다는 뜻이다.</para>
    ///
    /// <para>틱도 깊이도 같으면 <b>진짜 동점</b>이므로 억지로 가르지 않는다 — 등수를 매기는 쪽이
    /// <see cref="FinishRecord.SameRankAs"/>로 공동 순위를 낸다.</para>
    /// </summary>
    public class FinishOrderTracker
    {
        private readonly Dictionary<string, FinishRecord> finished = new Dictionary<string, FinishRecord>();
        private readonly List<FinishRecord> ordered = new List<FinishRecord>();
        private bool orderDirty;

        /// <summary>
        /// 이번 틱의 상태를 알린다. <paramref name="past"/>는 결승선을 지난 정도로,
        /// 음수면 아직 전이고 0 이상이면 지난 것이다.
        /// </summary>
        public void Observe(string entityId, long tick, float past)
        {
            if (past < 0f || finished.ContainsKey(entityId))
            {
                return;   // 아직 안 닿았거나, 이미 기록됐다 — 등수는 처음 닿은 순간이 정답이다
            }

            var record = new FinishRecord(entityId, tick, past);
            finished.Add(entityId, record);
            ordered.Add(record);
            orderDirty = true;
        }

        public bool HasFinished(string entityId) => finished.ContainsKey(entityId);

        public int FinishedCount => finished.Count;

        /// <summary>먼저 닿은 순. 같은 틱이면 깊이 넘은 쪽이 앞. 둘 다 같으면 앞뒤를 정하지 않는다.</summary>
        public IReadOnlyList<FinishRecord> Ordered
        {
            get
            {
                if (orderDirty)
                {
                    //  틱이 먼저, 같으면 깊이 넘은 쪽이 먼저. 둘 다 같으면 순서를 만들지 않는다 —
                    //  등수를 매기는 쪽이 SameRankAs로 훑어 공동 순위를 낸다.
                    ordered.Sort((a, b) =>
                    {
                        int byTick = a.Tick.CompareTo(b.Tick);
                        return byTick != 0 ? byTick : b.Past.CompareTo(a.Past);
                    });
                    orderDirty = false;
                }
                return ordered;
            }
        }

        public void Reset()
        {
            finished.Clear();
            ordered.Clear();
            orderDirty = false;
        }
    }
}
