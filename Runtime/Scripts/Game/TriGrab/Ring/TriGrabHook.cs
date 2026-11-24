using System;
using System.Collections.Generic;
using System.Numerics;

namespace LOP.TriGrab
{
    public enum TriGrabHookState { Out, Back, Done }
    public enum TriGrabHookEnd { None, Card, Mole, Range }

    /// <summary>
    /// 곧게 뻗어 처음 닿은 것에서 멈추고 돌아오는 손. 몸 가운데에서 출발한다.
    /// </summary>
    public sealed class TriGrabHook
    {
        public string Owner;
        public long FireTick;
        public Vector2 Origin;
        public Vector2 Dir;
        public float Length;
        /// <summary>돌아서기 시작한 순간의 길이. 그림이 되돌아오는 길이를 그릴 때 쓴다.</summary>
        public float TurnLength;
        public Vector2 Tip;
        public TriGrabHookState State;
        public TriGrabHookEnd End;
        public int CardId = -1;
        public int MoleId = -1;
        public long TurnTick;

        /// <summary>한 번에 따라가는 조각 길이. 큰 틱에서도 카드를 건너뛰지 않게 한다.</summary>
        public const float PieceLength = 0.04f;
        const float MoleTouch = TriGrabConfig.MoleRadius + 0.14f;
        const float CardTouch = TriGrabConfig.CoinRadius + TriGrabConfig.HookSlack;
        const float ArriveDistance = TriGrabConfig.BodyRadius * 0.6f;

        public static TriGrabHook Fire(string owner, Vector2 bodyPos, float yawDegrees, long tick)
        {
            return new TriGrabHook
            {
                Owner = owner,
                FireTick = tick,
                Origin = bodyPos,
                Dir = TriGrabMath.DirFromYaw(yawDegrees),
                Tip = bodyPos,
            };
        }

        public static void StepOut(TriGrabHook h, IList<TriGrabCard> onDisc, IList<TriGrabMole> moles, long tick, float dt)
        {
            if (h.State != TriGrabHookState.Out) return;
            float move = Math.Min(TriGrabConfig.HookOutSpeed * dt, TriGrabConfig.HookRange - h.Length);
            int pieces = (int)Math.Ceiling(move / PieceLength);
            float left = move;
            for (int i = 0; i < pieces; i++)
            {
                float step = Math.Min(PieceLength, left);
                left -= step;
                h.Length += step;
                h.Tip = h.Origin + h.Dir * h.Length;

                for (int m = 0; m < moles.Count; m++)
                {
                    var mole = moles[m];
                    if (!mole.Blocks(tick)) continue;
                    if (Vector2.Distance(h.Tip, mole.Pos) >= MoleTouch) continue;
                    mole.BonkTick = tick;
                    h.End = TriGrabHookEnd.Mole;
                    h.MoleId = mole.Id;
                    Turn(h, tick);
                    return;
                }

                TriGrabCard best = null;
                float bestD = float.MaxValue;
                for (int c = 0; c < onDisc.Count; c++)
                {
                    var card = onDisc[c];
                    if (TriGrabCards.PopK(card, tick) < 0.5f) continue;
                    float d = Vector2.Distance(h.Tip, card.Pos);
                    if (d >= CardTouch || d >= bestD) continue;
                    bestD = d;
                    best = card;
                }
                if (best != null)
                {
                    best.CarriedBy = h.Owner;
                    h.End = TriGrabHookEnd.Card;
                    h.CardId = best.Id;
                    Turn(h, tick);
                    return;
                }
            }

            if (h.Length >= TriGrabConfig.HookRange - 1e-4f)
            {
                h.Length = TriGrabConfig.HookRange;
                h.Tip = h.Origin + h.Dir * h.Length;
                h.End = TriGrabHookEnd.Range;
                Turn(h, tick);
            }
        }

        static void Turn(TriGrabHook h, long tick)
        {
            h.State = TriGrabHookState.Back;
            h.TurnLength = h.Length;
            h.TurnTick = tick;
        }

        /// <summary>몸이 움직였을 수 있으니 매 틱 몸 자리를 시작점으로 다시 잡는다.</summary>
        public static bool StepBack(TriGrabHook h, Vector2 bodyPos, IDictionary<int, TriGrabCard> cards, float dt)
        {
            if (h.State != TriGrabHookState.Back) return h.State == TriGrabHookState.Done;
            h.Origin = bodyPos;
            h.Length = Math.Max(0f, h.Length - TriGrabConfig.HookBackSpeed * dt);
            h.Tip = h.Origin + h.Dir * h.Length;
            if (h.CardId >= 0 && cards.TryGetValue(h.CardId, out var card))
            {
                card.Pos = h.Tip;
                card.Vel = Vector2.Zero;
            }
            if (h.Length > ArriveDistance) return false;
            h.State = TriGrabHookState.Done;
            return true;
        }
    }
}
