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

                buffer.LastReceived = command;
                buffer.PredictedTicks = 0;
                return command;
            }

            buffer.Current = null;
            return null;
        }

        /// <summary>
        /// 유실로 비어 있는 틱을 마지막으로 받은 커맨드로 메운다(입력 예측). 확정된 커맨드를 반환.
        ///
        /// <para>빈 칸은 비워둘 수 없다 — 서버는 그 틱을 어차피 굴려야 한다. 0으로 메우면 "제동하라"는
        /// *능동적으로 틀린* 지시가 된다. 한 틱은 20ms라, 그 사이 손을 뗐을 확률보다 계속 누르고 있을
        /// 확률이 압도적이므로 직전 이동을 이어 쓴다.</para>
        ///
        /// <para>이동만 이어 쓴다 — 점프·어빌리티는 1회성이라 반복하면 두 번 발동한다.
        /// 연속 <paramref name="maxTicks"/>를 넘으면 중립으로 떨어뜨린다(연결이 끊긴 캐릭터가
        /// 영영 달리면 안 된다).</para>
        /// </summary>
        public InputCommand PredictMissing(InputBuffer buffer, int maxTicks)
        {
            if (buffer.LastReceived == null || buffer.PredictedTicks >= maxTicks)
            {
                buffer.Current = new InputCommand();
                return buffer.Current;
            }

            buffer.PredictedTicks++;

            // 예측값은 받은 커맨드가 아니므로 시퀀스를 물려주지 않는다(dedup·seqGap 기준을 흐린다).
            buffer.Current = new InputCommand
            {
                Horizontal = buffer.LastReceived.Horizontal,
                Vertical = buffer.LastReceived.Vertical,
            };
            return buffer.Current;
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
