using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 붙잡을 별 조각 하나(왕국의 눈물 엔딩 오마주 — 떨어지는 빛에 다이브로 손을 뻗어 붙잡는다). <b>상태가 없다</b> —
    /// 틱만 넣으면 자리가 나와 클·서·되감기가 같은 답을 낸다(<see cref="Door"/>·<see cref="SpinnerVolume"/>과 같은 성질).
    /// 가운데를 축으로 천천히 돌며 위아래로 흔들린다.
    /// </summary>
    public readonly struct CatchTarget
    {
        public readonly Vector3 Center;
        public readonly float OrbitRadius, DegreesPerTick, StartDegrees, BobAmplitude, CatchRadius;
        public readonly int BobPeriod;

        public CatchTarget(Vector3 center, float orbitRadius, float degreesPerTick, float startDegrees, float bobAmplitude, int bobPeriod, float catchRadius)
        {
            Center = center;
            OrbitRadius = orbitRadius;
            DegreesPerTick = degreesPerTick;
            StartDegrees = startDegrees;
            BobAmplitude = bobAmplitude;
            BobPeriod = bobPeriod;
            CatchRadius = catchRadius;
        }
    }

    public static class CatchTargetGeometry
    {
        /// <summary>이 틱의 별 자리. 그림은 소수 틱을 넣는다. 큰 틱에서도 오차가 쌓이지 않게 double로 각을 접는다.</summary>
        public static Vector3 PositionAt(in CatchTarget t, double tick)
        {
            double deg = t.StartDegrees + t.DegreesPerTick * tick;
            deg -= System.Math.Floor(deg / 360.0) * 360.0;
            double rad = deg * System.Math.PI / 180.0;
            double bob = 0.0;
            if (t.BobPeriod > 0)
            {
                double phase = tick / t.BobPeriod;
                phase -= System.Math.Floor(phase);
                bob = System.Math.Sin(phase * 2.0 * System.Math.PI) * t.BobAmplitude;
            }
            return new Vector3(t.Center.x + (float)(System.Math.Cos(rad) * t.OrbitRadius),
                               t.Center.y + (float)bob,
                               t.Center.z + (float)(System.Math.Sin(rad) * t.OrbitRadius));
        }

        /// <summary>몸(선 캡슐)에서 별까지 — 잡는 반지름 + 몸 반지름 안이면 닿은 것. 음수면 그만큼 모자란다.</summary>
        public static float Reach(in CatchTarget t, long tick, Vector3 feet, float bodyRadius, float bodyHeight)
        {
            Vector3 star = PositionAt(t, tick);
            float lo = feet.y + bodyRadius, hi = feet.y + bodyHeight - bodyRadius;
            Vector3 nearest = new Vector3(feet.x, Mathf.Clamp(star.y, lo, hi), feet.z);
            return t.CatchRadius + bodyRadius - Vector3.Distance(star, nearest);
        }

        public static bool Catches(in CatchTarget t, long tick, Vector3 feet, float bodyRadius, float bodyHeight)
            => Reach(t, tick, feet, bodyRadius, bodyHeight) >= 0f;
    }

    /// <summary>이 판의 별 조각 전부. 맵 씬의 <see cref="StarVolume"/>이 로드될 때 스스로 들어온다.</summary>
    public class CatchTargetField
    {
        private readonly System.Collections.Generic.List<CatchTarget> all = new System.Collections.Generic.List<CatchTarget>();
        public System.Collections.Generic.IReadOnlyList<CatchTarget> All => all;
        public void Add(CatchTarget t) => all.Add(t);
        public bool Remove(CatchTarget t) => all.Remove(t);
    }

    /// <summary>별 붙잡기 결승 — 클·서가 같은 코드로 SkydiveWorld.Detection에서 부른다.</summary>
    public static class SkydiveCatch
    {
        /// <summary>
        /// 닿았으면 그 틱을 결승으로 적는다. 깊이 = 얼마나 깊이 들어왔나(같은 틱이면 더 깊은 쪽이 먼저 — 결승선과 같은 규칙).
        /// 공중에서 잡는다 — 결승선(땅에 내려앉아야)과 달리 접지를 묻지 않는다.
        /// </summary>
        public static bool TryCatch(GameFramework.World.Entity diver, CatchTargetField field, long tick, float bodyRadius, float bodyHeight)
        {
            var state = diver.Get<FinishState>();
            if (state == null || state.Finished || field == null || field.All.Count == 0)
            {
                return false;
            }
            Vector3 feet = GameFramework.World.EntityMotionExtensions.GetPosition(diver);
            float best = -1f;
            for (int i = 0; i < field.All.Count; i++)
            {
                best = Mathf.Max(best, CatchTargetGeometry.Reach(field.All[i], tick, feet, bodyRadius, bodyHeight));
            }
            if (best < 0f)
            {
                return false;
            }
            state.FinishedTick = tick;
            state.Depth = best;
            return true;
        }

        /// <summary>붙잡은 뒤에는 패러세일이 펴진 채 천천히 내려앉는다(엔딩처럼) — 스태미나도 안 마르게 해 착지 사망을 막는다.</summary>
        public static void SettleAfterFinish(GameFramework.World.Entity diver, float staminaMax)
        {
            var state = diver.Get<FinishState>();
            if (state == null || state.Finished == false)
            {
                return;
            }
            var posture = diver.Get<Posture>();
            if (posture != null)
            {
                posture.Gliding = true;
            }
            var stamina = diver.Get<Stamina>();
            if (stamina != null)
            {
                stamina.Current = staminaMax;
                stamina.EmergencyUsed = false;
                stamina.EmergencyRemaining = 0f;
            }
        }
    }
}
