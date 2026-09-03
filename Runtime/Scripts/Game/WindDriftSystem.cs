using System;
using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 몸을 바람에 실어 준다. 자세가 정하는 것은 <b>얼마나 빨리 실리나</b> 하나다 — 넓게 편
    /// 몸은 공기가 세게 붙잡아 금방 같이 흐르고, 좁힌 몸은 공기를 뚫고 지나가 늦게 실린다.
    /// 그래서 짧은 구간에서는 편 자세만 바람을 다 받는다.
    /// </summary>
    public class WindDriftSystem
    {
        public void Tick(GameFramework.World.Entity entity, float deltaTime,
                         in SkydiveConfig config, WindField field)
        {
            var drift = entity.Get<WindDrift>();
            var transform = entity.Get<GameFramework.World.Transform>();
            if (drift == null || transform == null || field == null)
            {
                return;
            }

            var state = entity.Get<MotionState>()?.Value ?? SkydiveMotionState.Falling;

            // 발을 딛고 있으면 땅이 잡아 준다 — 걷는 몸을 바람이 밀지 않는다.
            Vector3 target = state == SkydiveMotionState.Walking
                ? Vector3.Zero
                : field.SampleAt(transform.Position);

            // 목표가 있는 동안 그 크기를 기록해 둔다. 볼륨을 나가 목표가 0이 되어도
            // 이 기록이 남아 있어야 들어갈 때와 같은 크기를 기준으로 같은 시간 동안
            // 돌아올 수 있다 — 목표 자체(0)에서는 그 크기를 더 알 수 없기 때문이다.
            if (target != Vector3.Zero)
            {
                drift.Anchor = target;
            }

            float lag = LagOf(entity.Get<Posture>(), config);
            if (lag <= 0f)
            {
                drift.Value = target;   // 지연 없음. 나누기 전에 걸러낸다
                return;
            }

            // 들어갈 때도 나올 때도 lag초가 걸리게 한다. 목표만 보고 속도를 정하면 나올 때는
            // 목표가 0이라 속도도 0이 되어 영영 안 빠진다. 그렇다고 매 틱 Value 자신의 크기를
            // 기준으로 삼으면, 그 크기가 줄어드는 만큼 기준도 같이 줄어 버려 기하급수적으로만
            // 다가가고 정확히 0에는 닿지 못한다 — 그래서 줄지 않는 Anchor를 기준으로 삼는다.
            float reference = Math.Max(target.Length(), drift.Anchor.Length());
            drift.Value = MoveTowards(drift.Value, target, reference / lag * deltaTime);
        }

        private static float LagOf(Posture posture, in SkydiveConfig config)
        {
            if (posture == null)
            {
                return config.SpreadWindLag;
            }
            if (posture.Gliding)
            {
                return config.GlideWindLag;
            }

            float axis = posture.Axis < 0f ? 0f : (posture.Axis > 1f ? 1f : posture.Axis);
            return config.SpreadWindLag + (config.DiveWindLag - config.SpreadWindLag) * axis;
        }

        private static Vector3 MoveTowards(Vector3 current, Vector3 target, float maxStep)
        {
            Vector3 diff = target - current;
            float distance = diff.Length();
            if (distance <= maxStep || distance == 0f)
            {
                return target;
            }
            return current + diff * (maxStep / distance);
        }
    }
}
