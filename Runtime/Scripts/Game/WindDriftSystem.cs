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

            // 속도의 기준(reference)은 셋 중 가장 큰 것을 쓴다. target만 보면 나갈 때(0) 속도가
            // 0이 되어 영영 안 빠지고, Anchor만 보면 바람에서 더 약한 바람으로 바로 넘어갈 때
            // 기준이 새 목표까지 같이 작아져 전이가 몇 배로 늘어진다(이미 실려 있던 세기를
            // 잊어버리기 때문). 그래서 지금 목표(target) · 마지막으로 있었던 바람(Anchor) ·
            // 지금까지 실린 양(Value) 중 가장 큰 것을 쓴다 — 들어갈 때는 target이, 나갈 때는
            // Anchor가, 센 바람에서 약한 바람으로 넘어갈 때는 아직 안 줄어든 Value가 버텨 준다.
            float reference = Math.Max(target.Length(), Math.Max(drift.Anchor.Length(), drift.Value.Length()));
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
