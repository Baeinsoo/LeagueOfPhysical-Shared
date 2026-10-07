using UnityEngine;

namespace LOP
{
    /// <summary>이 높이 아래로 빠지면 이 자리에서 다시 떨어진다(별만 결승인 맵 — 놓치면 구름 아래로).</summary>
    public readonly struct RetryZone
    {
        public readonly float BelowY;
        public readonly Vector3 Point;
        public RetryZone(float belowY, Vector3 point) { BelowY = belowY; Point = point; }
    }

    /// <summary>이 판의 "다시 떨어지기" 자리. 맵 씬의 <see cref="RetryVolume"/>이 로드될 때 스스로 들어온다.</summary>
    public class RetryField
    {
        private readonly System.Collections.Generic.List<RetryZone> all = new System.Collections.Generic.List<RetryZone>();
        public System.Collections.Generic.IReadOnlyList<RetryZone> All => all;
        public void Add(RetryZone z) => all.Add(z);
        public bool Remove(RetryZone z) => all.Remove(z);
    }

    /// <summary>별만 결승(사용자 10-07) — 놓쳐도 처음으로 가지 않고 출구 아래에서 다시 시도한다("아깝다, 한 번 더").</summary>
    public static class SkydiveRetry
    {
        /// <summary>아직 별을 못 잡았고 구름 아래로 빠졌으면 다시 떨어질 자리를 준다. 잡은 사람은 그냥 내려앉는다.</summary>
        public static bool ShouldRetry(GameFramework.World.Entity diver, RetryField field, out Vector3 point)
        {
            point = default;
            if (field == null || field.All.Count == 0 || (diver.Get<FinishState>()?.Finished ?? false))
            {
                return false;
            }
            float y = GameFramework.World.EntityMotionExtensions.GetPosition(diver).y;
            for (int i = 0; i < field.All.Count; i++)
            {
                if (y < field.All[i].BelowY)
                {
                    point = field.All[i].Point;
                    return true;
                }
            }
            return false;
        }
    }
}
