using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 과녁이 솟았다 떨어지는 길 + 좌우로 왕복하는 길. 상태가 없는 순수 계산이라 클·서·뷰가 같은
    /// 시각을 넣으면 같은 답을 얻는다 — <b>그래서 과녁을 통신으로 보낼 필요가 없다</b>.
    ///
    /// <para>화살(<see cref="ArcheryTrajectory"/>)과 같은 모양이다: 출발점·초기속도·출발시각만 있으면
    /// 어느 시각의 위치든 나온다.</para>
    ///
    /// <para><b>사거리 과녁은 좌우로도 움직인다</b>(<see cref="ArcheryTarget.LateralSpan"/>) —
    /// 화살이 과녁에 닿기까지 걸리는 시간(사거리가 멀수록 길다)만큼 과녁이 미리 가 있을 자리를
    /// 읽어야 맞힐 수 있게 만든 것이다. 흔드는 움직임이라 폭을 벗어나지 않고, 씨앗 없이 시각만으로
    /// 계산되므로(=예측 가능) "운"이 아니라 "읽는 연습"이 된다.</para>
    ///
    /// <para><b>투석기 표적은 옆으로 쭉 난다</b>(<see cref="ArcheryTarget.Drift"/>) — 한 틱에 0.2 m(성배 10 m/s)라 판정의 "한 틱 동안 멈춘 것으로 본다" 전제(반지름 0.5 m)를 지킨다.</para>
    ///
    /// <para><b>2차 표적은 출렁·솟음·회전을 더한다</b> — 모두 시간의 순수 함수라 클·서가 같은 답을 낸다. 도는 접시의 판정 크기는 <see cref="RadiusAt"/>.</para>
    /// </summary>
    public static class ArcheryTargetMotion
    {
        /// <summary>
        /// 과녁에 걸리는 중력. <b>화살과 같은 값이다</b> — 중력은 세계의 성질이지 물체의 설정이
        /// 아니다. 다르게 두면 같은 화면에서 화살과 과녁이 서로 다른 속도로 떨어져,
        /// 화살로 과녁을 따라가는 이 게임에서는 바로 눈에 띈다.
        /// </summary>
        public const float Gravity = ArcheryTrajectory.Gravity;

        /// <summary>
        /// 그 높이까지 솟으려면 얼마로 출발해야 하나(m/s). 중력이 고정이라 높이 하나가 속도도
        /// 수명도 정한다 — 따로 적을 값이 없다.
        /// </summary>
        public static float RiseSpeedFor(float riseHeight)
        {
            if (riseHeight <= 0f)
            {
                return 0f;
            }
            //  정점 높이 H = v0²/(2g)를 v0에 대해 풀면 v0 = sqrt(2gH)다.
            return Mathf.Sqrt(2f * Gravity * riseHeight);
        }

        /// <summary>
        /// 그 속도로 솟은 과녁이 떠 있는 시간(초). 올라갔다 내려오므로 정점까지의 두 배다.
        /// <b>웨이브 과녁의 수명은 여기서만 나온다</b> — 식을 두 군데 적으면 한쪽만 고쳐진다.
        /// </summary>
        public static float LifetimeFor(float riseSpeed)
        {
            return 2f * riseSpeed / Gravity;
        }

        /// <summary>
        /// 그 시각의 과녁 자리. 솟기 전(또는 좌우로 흔들기 전)에는 출발점에 가만히 있다.
        /// <b>안 솟는 과녁(솟는 속도 0 — 사거리 맵의 서 있는 과녁)은 위아래로는 내내 제자리다</b> —
        /// 포물선 식은 "떠올랐다 떨어진다"는 전제라, 속도 0을 그대로 넣으면 중력만 작용해 서 있던
        /// 과녁이 바닥으로 꺼진다. <b>단 좌우로는 <see cref="ArcheryTarget.LateralSpan"/>이 있으면
        /// 그만큼 흔든다</b> — 위아래(솟기)와 좌우(흔들기)는 서로 다른 축이라 하나가 꺼져 있어도
        /// 다른 하나는 그대로 동작한다.
        /// </summary>
        public static Vector3 PositionAt(in ArcheryTarget target, double tick, float tickInterval)
        {
            float t = (float)((tick - target.SpawnTick) * tickInterval);
            if (t <= 0f)
            {
                return target.Origin;
            }

            float y = 0f;
            if (target.RiseSpeed > 0f)
            {
                y = target.RiseSpeed * t - 0.5f * Gravity * t * t;
            }

            Vector3 lateral = Vector3.zero;
            if (target.LateralSpan > 0f && target.LateralPeriod > 0f)
            {
                //  삼각파의 값 범위는 [-1,1]이므로 절반 폭(진폭)을 곱해야 끝에서 끝까지가 LateralSpan이 된다.
                float offset = TriangleWave(t, target.LateralPeriod) * (target.LateralSpan * 0.5f);
                lateral = LateralAxis(target.Facing) * offset;
            }

            //  투석기 표적: 수명 동안 위로 한 번 호를 그린다. 0~1로 자른 s에서 4h·s(1−s)는
            //  가운데(s=0.5)에서 h, 양 끝에서 0이다. 중력 포물선을 안 쓰는 이유 — 수명(2.6초)에
            //  맞추면 정점이 8 m로 고정돼 높이를 고를 수 없다.
            if (target.ArcHeight > 0f && target.LifetimeSeconds > 0f)
            {
                float s = Mathf.Clamp01(t / target.LifetimeSeconds);
                y += 4f * target.ArcHeight * s * (1f - s);
            }

            //  출렁(물풍선·종·피냐타): sin에서 시작 위상의 값을 빼 발사 순간 제자리에서 시작한다 —
            //  안 빼면 투석기 바구니가 놓은 자리에서 최대 진폭만큼 순간이동한다. 위아래 폭(2A)은 그대로다.
            if (target.BobAmplitude > 0f && target.BobHz > 0f)
            {
                float phase = 2f * Mathf.PI * target.MotionPhase;
                y += target.BobAmplitude * (Mathf.Sin(2f * Mathf.PI * target.BobHz * t + phase) - Mathf.Sin(phase));
            }

            //  솟음(폭죽 로켓): 중력 없이 곧게 오른다. 수명(2.6초) 끝까지 올라가는 것이 "먼 데다 계속 도망간다"는 어려움이다.
            if (target.Climb > 0f)
            {
                y += target.Climb * t;
            }

            return new Vector3(target.Origin.x, target.Origin.y + y, target.Origin.z) + lateral + target.Drift * t;
        }

        /// <summary>
        /// 좌우로 오가는 삼각파. 값의 범위는 [-1, 1]이고 <c>t=0</c>에서 0(제자리)이다.
        ///
        /// <para><b>사인파가 아니라 삼각파를 쓰는 이유</b> — 사인파는 중앙을 지날 때 가장 빠르고
        /// 양 끝에서 느려지는데, 그러면 "지금 과녁이 얼마나 빠른가"가 자리마다 달라서 리드(조준을
        /// 얼마나 앞에 줘야 하는가)를 배워도 다음 판에 못 써먹는다. 삼각파는 방향을 바꾸는 순간
        /// (끝점)만 빼면 속도가 늘 일정해서, 한 번 감(느낌)을 잡으면 그 감이 계속 맞는다.
        /// 끝점에서 방향이 꺾이는 순간만 빠르게 다시 읽어야 하는데, 그 어긋남은 "운"이 아니라
        /// "이번엔 타이밍을 놓쳤다"는 자기 오독으로 느껴지는 게 의도다.</para>
        /// </summary>
        private static float TriangleWave(float t, float period)
        {
            float phase = Mathf.Repeat(t, period) / period;   // 0..1, 한 주기 안에서 지금 어디인지
            if (phase < 0.25f) { return phase * 4f; }          //  0 →  1
            if (phase < 0.75f) { return 2f - phase * 4f; }      //  1 → -1
            return phase * 4f - 4f;                             // -1 →  0
        }

        /// <summary>
        /// 과녁이 좌우로 흔들리는 축(단위 벡터) — 사수 쪽을 보는 방향(<paramref name="facing"/>)의
        /// 수평 수직선이다. 레인 폭 방향으로만 움직이게 하려는 것이다(사수 쪽으로 다가오거나
        /// 멀어지면 다른 레인을 침범할 수 있다).
        /// </summary>
        private static Vector3 LateralAxis(Vector3 facing)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, facing);
            float sqrMagnitude = axis.sqrMagnitude;
            if (sqrMagnitude < 1e-8f)
            {
                //  facing이 거의 수직(과녁이 없거나 값이 비정상)이면 방향이 안 정해진다 — 흔들
                //  폭이 0인 과녁(웨이브 맵)에서만 벌어지는데 그때는 어차피 곱해서 0이 되므로 아무
                //  방향이나 안전하다.
                return Vector3.right;
            }
            return axis / Mathf.Sqrt(sqrMagnitude);
        }

        /// <summary>사수가 보는 기준 오른쪽(수평 단위벡터). <paramref name="facing"/>은 과녁이 사수를 보는 쪽이다.</summary>
        public static Vector3 ShooterRightAxis(Vector3 facing)
        {
            Vector3 axis = Vector3.Cross(Vector3.up, -facing);
            float sqr = axis.sqrMagnitude;
            return sqr < 1e-8f ? Vector3.right : axis / Mathf.Sqrt(sqr);
        }

        /// <summary>도는 접시가 옆으로 누워도 이 비율 밑으로는 안 작아진다(spec 2차 §2 — 완전히 0이면 아예 못 맞힌다).</summary>
        public const float MinSpinRadiusRatio = 0.2f;

        /// <summary>
        /// 그 시각의 회전각(라디안). 0이면 앞면이 사수를 본다. 축은 사수 기준 가로축이다 — 화면이 이 각으로 그림을 돌린다.
        /// 발사 전에는 시작 위상의 각에 머문다. 안 도는 표적은 0.
        /// </summary>
        public static float SpinAngleAt(in ArcheryTarget target, double tick, float tickInterval)
        {
            if (target.SpinHz <= 0f)
            {
                return 0f;
            }
            float t = Mathf.Max(0f, (float)((tick - target.SpawnTick) * tickInterval));
            return 2f * Mathf.PI * (target.MotionPhase + target.SpinHz * t);
        }

        /// <summary>
        /// 그 시각의 판정 반지름(m). 도는 접시는 옆으로 누울수록 작아진다 — r·max(0.2, |cos θ|).
        /// <b>서버 판정·클라 꽂힘·화면 회전이 모두 이 함수와 <see cref="SpinAngleAt"/>을 같은 시각으로 부른다</b> —
        /// 한 곳만 옛 반지름을 쓰면 "꽂혔는데 점수가 없다"가 된다.
        /// </summary>
        public static float RadiusAt(in ArcheryTarget target, double tick, float tickInterval)
        {
            if (target.SpinHz <= 0f)
            {
                return target.Radius;
            }
            float cos = Mathf.Abs(Mathf.Cos(SpinAngleAt(target, tick, tickInterval)));
            return target.Radius * Mathf.Max(MinSpinRadiusRatio, cos);
        }

        /// <summary>아직 공중에 있나. 솟기 전과 떨어진 뒤에는 거짓이다.</summary>
        public static bool IsAlive(in ArcheryTarget target, double tick, float tickInterval)
        {
            double elapsed = (tick - target.SpawnTick) * tickInterval;
            return elapsed >= 0d && elapsed <= target.LifetimeSeconds;
        }
    }
}
