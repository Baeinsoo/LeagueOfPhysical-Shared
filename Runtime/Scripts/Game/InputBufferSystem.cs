using System.Collections.Generic;
using System.Linq;

namespace LOP
{
    /// <summary>
    /// <see cref="InputBuffer"/>를 채우고(Enqueue) 소비하고(Consume) 비우는(Prune/Trim) 로직.
    /// 컴포넌트는 순수 데이터라 상태 변경은 전부 여기(System)에서 한다.
    /// 클라·서버가 각자 표준 넷코드 방식대로 호출한다(클=로컬 캡처+redundancy 윈도우, 서=지터버퍼+지각 prune).
    /// </summary>
    public class InputBufferSystem
    {
        /// <summary>커맨드를 버퍼에 넣는다. 이미 처리된 시퀀스나 같은 틱은 무시(redundancy dedup). 새로 들어갔으면 true.</summary>
        public bool Enqueue(InputBuffer buffer, long tick, InputCommand command)
        {
            if (command.SequenceNumber <= buffer.LastProcessedSequence)
            {
                return false;
            }
            if (buffer.Commands.ContainsKey(tick))
            {
                return false;
            }

            buffer.Commands.Add(tick, command);
            buffer.ExpectedNextSequence = command.SequenceNumber + 1;
            return true;
        }

        /// <summary>
        /// 지정 틱의 커맨드를 이번 틱 소비분(<see cref="InputBuffer.Current"/>)으로 확정한다.
        /// 있으면 꺼내 제거하고 처리 시퀀스를 갱신, 없으면 Current=null(무입력). 확정된 커맨드(또는 null)를 반환.
        /// </summary>
        public InputCommand Consume(InputBuffer buffer, long tick)
        {
            if (buffer.Commands.TryGetValue(tick, out var command))
            {
                buffer.Commands.Remove(tick);
                buffer.LastProcessedSequence = command.SequenceNumber;
                buffer.Current = command;
                return command;
            }

            buffer.Current = null;
            return null;
        }

        /// <summary>이번 틱 커맨드를 직접 확정한다(클라 로컬 예측 — 방금 캡처한 커맨드 또는 무입력 0).</summary>
        public void SetCurrent(InputBuffer buffer, InputCommand command)
        {
            buffer.Current = command;
        }

        /// <summary>지정 틱보다 오래된 커맨드를 버린다(서버 jitter buffer — 지각/처리불가). 버린 개수를 반환.</summary>
        public int PruneBefore(InputBuffer buffer, long tick)
        {
            var stale = buffer.Commands.Keys.Where(k => k < tick).ToList();
            foreach (var key in stale)
            {
                buffer.Commands.Remove(key);
            }
            return stale.Count;
        }

        /// <summary>최근 N틱만 남긴다(클라 redundancy 윈도우 유지 — 유실 대비 재전송분).</summary>
        public void TrimToWindow(InputBuffer buffer, int window)
        {
            while (buffer.Commands.Count > window)
            {
                buffer.Commands.Remove(buffer.Commands.Keys.First());
            }
        }
    }
}
