using System;

namespace LOP
{
    /// <summary>
    /// 틱 → 그 틱의 <see cref="PredictedAbilityState"/>. 클라 롤백 재생용 어빌리티 상태 로그.
    /// 슬롯 = tick % capacity. 같은 슬롯의 오래된 틱은 덮여 자동 eviction. (InputHistory 링과 동형 —
    /// 참조형 페이로드라 tick 판별용 병렬 배열 + sentinel.)
    /// </summary>
    public class PredictedAbilityStateHistory
    {
        private const long EmptyTick = long.MinValue;

        private readonly long[] _ticks;
        private readonly PredictedAbilityState[] _states;
        private readonly int _capacity;
        private long _latestTick;
        private bool _hasAny;

        public PredictedAbilityStateHistory(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _ticks = new long[capacity];
            _states = new PredictedAbilityState[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _ticks[i] = EmptyTick;
            }
        }

        public void Record(long tick, PredictedAbilityState state)
        {
            int slot = Slot(tick);
            _ticks[slot] = tick;
            _states[slot] = state;
            if (!_hasAny || tick > _latestTick)
            {
                _latestTick = tick;
            }
            _hasAny = true;
        }

        public bool TryGet(long tick, out PredictedAbilityState state)
        {
            if (_hasAny && tick <= _latestTick && tick > _latestTick - _capacity)
            {
                int slot = Slot(tick);
                if (_ticks[slot] == tick)
                {
                    state = _states[slot];
                    return true;
                }
            }

            state = null;
            return false;
        }

        private int Slot(long tick) => (int)(((tick % _capacity) + _capacity) % _capacity);
    }
}
