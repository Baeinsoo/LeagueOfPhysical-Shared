using System;

namespace LOP
{
    /// <summary>
    /// 틱 → 그 틱에 적용된 입력 커맨드. 클라 롤백 재생(replay)용 입력 로그.
    /// 슬롯 = tick % capacity. 같은 슬롯의 오래된 틱은 덮여 자동 eviction. (slice② SnapshotHistory 링과 동형 —
    /// InputCommand는 참조형이라 tick 판별용 병렬 배열 + sentinel을 둔다.)
    /// </summary>
    public class InputHistory
    {
        private const long EmptyTick = long.MinValue;

        private readonly long[] _ticks;
        private readonly InputCommand[] _commands;
        private readonly int _capacity;
        private long _latestTick;
        private bool _hasAny;

        public InputHistory(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity));
            }

            _capacity = capacity;
            _ticks = new long[capacity];
            _commands = new InputCommand[capacity];
            for (int i = 0; i < capacity; i++)
            {
                _ticks[i] = EmptyTick;
            }
        }

        /// <summary>틱에 적용된 입력을 기록한다(무입력 틱은 0-커맨드를 넣는다).</summary>
        public void Record(long tick, InputCommand command)
        {
            int slot = Slot(tick);
            _ticks[slot] = tick;
            _commands[slot] = command;
            if (!_hasAny || tick > _latestTick)
            {
                _latestTick = tick;
            }
            _hasAny = true;
        }

        /// <summary>틱의 입력을 조회한다. 최근 capacity틱 윈도우 밖이거나 미기록이면 false.</summary>
        public bool TryGet(long tick, out InputCommand command)
        {
            if (_hasAny && tick <= _latestTick && tick > _latestTick - _capacity)
            {
                int slot = Slot(tick);
                if (_ticks[slot] == tick)
                {
                    command = _commands[slot];
                    return true;
                }
            }

            command = null;
            return false;
        }

        private int Slot(long tick) => (int)(((tick % _capacity) + _capacity) % _capacity);
    }
}
