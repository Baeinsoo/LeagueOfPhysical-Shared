using System;
using System.Numerics;

namespace LOP
{
    /// <summary>
    /// 한 틱 사이에 캐릭터가 레이저에 닿았는지 판정한다. 시간을 끊어 보면 다이브 속도에서
    /// 얇은 빔을 그냥 통과하므로(터널링), <b>확실히 안전한 만큼만 시간을 앞으로 감는</b>
    /// 방식으로 훑는다.
    ///
    /// <para>업계 표준 매핑: Mirtich의 Conservative Advancement(박사논문 §2.3.2, 3D 강체).
    /// PhysX가 자기 CCD를 <i>best-effort conservative advancement scheme</i>이라 문서에 적어
    /// 두었고, Bullet <c>btContinuousConvexCollision</c>도 같은 알고리즘이다.</para>
    ///
    /// <para>안전 전진 폭은 <c>(거리 − 허용) ÷ 최대 접근 속도</c>다. 최대 접근 속도는 캐릭터의
    /// 이동 거리에 <b>빔 끝이 그리는 호의 길이</b>(각속도 × 길이)를 더해 만든다 — 이는 CA의
    /// 표준 상한식(선속도 + 각속도 × 바운딩 스피어 반지름)을 우리 도형에 대입한 것이다.</para>
    /// </summary>
    public static class LaserSweep
    {
        /// <summary>스치면 수렴이 느려진다. 상한에 닿으면 통과로 본다 — 억울한 죽음이 더 나쁘다.</summary>
        public const int MaxIterations = 16;

        /// <summary>
        /// 이만큼 가까워지면 닿은 것으로 본다. <b>없으면 안 된다</b> — 안전 전진 폭이 남은 거리에
        /// 비례해서, 빔이 가로질러 오는 정상적인 경우에도 <c>d</c>가 허용 거리에 **점점 가까워지기만
        /// 하고 절대 닿지 않아** 상한까지 돌다 통과로 처리된다. Box2D도 같은 이유로 target에
        /// tolerance를 더해 멈춘다.
        /// </summary>
        public const float HitTolerance = 0.01f;

        /// <summary>
        /// 이 틱에 닿았나. <paramref name="timeOfImpact"/>는 틱 안에서의 시각(0~1)이다.
        /// </summary>
        public static bool Hit(in Laser laser, long tick,
                               Vector3 bottomFrom, Vector3 topFrom,
                               Vector3 bottomTo, Vector3 topTo,
                               float capsuleRadius, out float timeOfImpact)
            => Hit(laser, tick, bottomFrom, topFrom, bottomTo, topTo,
                   capsuleRadius, out timeOfImpact, out _);

        /// <param name="iterations">
        /// 돈 횟수. <see cref="MaxIterations"/>와 같으면 상한에 걸려 관대하게 통과시킨 것이다 —
        /// 이게 잦으면 레이저가 조용히 약해지므로 부르는 쪽이 세어 둔다.
        /// </param>
        public static bool Hit(in Laser laser, long tick,
                               Vector3 bottomFrom, Vector3 topFrom,
                               Vector3 bottomTo, Vector3 topTo,
                               float capsuleRadius, out float timeOfImpact, out int iterations)
        {
            timeOfImpact = 0f;
            iterations = 0;
            if (LaserGeometry.Lit(laser, tick) == false)
            {
                return false;
            }

            float allowed = capsuleRadius + laser.Radius;
            float moved = Vector3.Distance(bottomFrom, bottomTo);
            float tipArc = MathF.Abs(laser.AngularSpeed) * laser.Length;
            float closing = moved + tipArc;

            float t = 0f;
            for (int i = 0; i < MaxIterations; i++)
            {
                iterations = i + 1;
                Vector3 bottom = Vector3.Lerp(bottomFrom, bottomTo, t);
                Vector3 top = Vector3.Lerp(topFrom, topTo, t);
                LaserGeometry.SegmentAt(laser, tick + t, out Vector3 a, out Vector3 b);

                float d = SegmentDistance(bottom, top, a, b);
                if (d <= allowed + HitTolerance)
                {
                    timeOfImpact = t;
                    return true;
                }
                if (closing <= 1e-6f)
                {
                    return false;   // 둘 다 안 움직인다 — 지금 안 닿았으면 이 틱엔 안 닿는다
                }

                t += (d - allowed) / closing;
                if (t >= 1f)
                {
                    return false;
                }
            }
            return false;
        }

        /// <summary>
        /// 3D 선분 두 개의 최단거리. 세로 캡슐 축과 가로 빔은 대개 어긋나 있어(skew) 평면 공식으로는
        /// 안 된다. (Ericson, Real-Time Collision Detection §5.1.9)
        /// </summary>
        public static float SegmentDistance(Vector3 p1, Vector3 q1, Vector3 p2, Vector3 q2)
        {
            const float eps = 1e-8f;

            Vector3 d1 = q1 - p1;
            Vector3 d2 = q2 - p2;
            Vector3 r = p1 - p2;
            float a = Vector3.Dot(d1, d1);
            float e = Vector3.Dot(d2, d2);
            float f = Vector3.Dot(d2, r);

            float s, t;
            if (a <= eps && e <= eps)
            {
                return r.Length();
            }
            if (a <= eps)
            {
                s = 0f;
                t = Clamp01(f / e);
            }
            else
            {
                float c = Vector3.Dot(d1, r);
                if (e <= eps)
                {
                    t = 0f;
                    s = Clamp01(-c / a);
                }
                else
                {
                    float b = Vector3.Dot(d1, d2);
                    float denom = a * e - b * b;
                    s = denom > eps ? Clamp01((b * f - c * e) / denom) : 0f;
                    t = (b * s + f) / e;
                    if (t < 0f)
                    {
                        t = 0f;
                        s = Clamp01(-c / a);
                    }
                    else if (t > 1f)
                    {
                        t = 1f;
                        s = Clamp01((b - c) / a);
                    }
                }
            }
            return Vector3.Distance(p1 + d1 * s, p2 + d2 * t);
        }

        private static float Clamp01(float v) => v < 0f ? 0f : (v > 1f ? 1f : v);
    }
}
