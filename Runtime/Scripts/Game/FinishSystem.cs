using GameFramework;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 결승선을 넘었는지 보고 <see cref="FinishState"/>에 적는다. 판정식은 서버가 등수를 매길 때
    /// 쓰던 <see cref="FinishLineOverlap"/> 그대로다.
    ///
    /// <para>몸 바운드를 콜라이더가 아니라 <b>진실원본 + <see cref="GameFramework.World.CapsuleShape"/></b>로
    /// 조립한다. 콜라이더가 원래 그 둘로 만들어지므로(<see cref="PhysicsBodyFactory"/>) 값은 같고,
    /// 되돌리기 재생 중에도 얼지 않는다 — 재생 중엔 물리를 안 돌려 엔진 트랜스폼이 한 틱 전에
    /// 멈춰 있다.</para>
    /// </summary>
    public class FinishSystem
    {
        private readonly FinishLineBounds line;
        private readonly FinishAxis axis;
        private readonly bool increasing;

        //  결승선을 모르면 아무도 통과하지 못한다 — 판이 시간 상한까지 안 끝나는데 로그는 조용하다.
        //  한 번만 크게 알린다(매 틱 찍으면 다른 로그를 전부 밀어낸다).
        private bool warnedNoLine;

        public FinishSystem(FinishLineBounds line, FinishAxis axis, bool increasing)
        {
            this.line = line;
            this.axis = axis;
            this.increasing = increasing;
        }

        public void Tick(GameFramework.World.Entity entity, long tick)
        {
            var state = entity.Get<FinishState>();
            if (state == null || state.Finished)
            {
                return;   // 등수는 처음 닿은 순간이 정답이다 — 덮어쓰지 않는다
            }

            var transform = entity.Get<GameFramework.World.Transform>();
            var shape = entity.Get<GameFramework.World.CapsuleShape>();
            if (transform == null || shape == null)
            {
                return;
            }

            if (line.TryGet(out Bounds lineBounds) == false)
            {
                if (warnedNoLine == false)
                {
                    warnedNoLine = true;
                    Debug.LogError("[Finish] 결승선을 모른다 — 맵 마커가 등록되지 않았다. " +
                        "아무도 통과하지 못하고 판이 시간 상한까지 간다.");
                }
                return;
            }

            float past = FinishLineOverlap.Past(BodyBounds(transform, shape), lineBounds, axis, increasing);
            if (past < 0f)
            {
                return;
            }

            state.FinishedTick = tick;
            state.Depth = past;

            //  [진단용 임시] 누가 언제 얼마나 깊이 닿았는지. 등수가 이 세 값으로만 정해진다.
            Debug.Log($"[Finish] {entity.Id} tick={tick} 넘은깊이={past:F3}m");
        }

        //  콜라이더와 같은 모양으로 맞춘다 — PhysicsBodyFactory가 center를 (0, height/2, 0)에 둔다.
        private static Bounds BodyBounds(GameFramework.World.Transform transform,
                                         GameFramework.World.CapsuleShape shape)
        {
            Vector3 center = transform.Position.ToUnity() + new Vector3(0f, shape.Height * 0.5f, 0f);
            return new Bounds(center, new Vector3(shape.Radius * 2f, shape.Height, shape.Radius * 2f));
        }
    }
}
