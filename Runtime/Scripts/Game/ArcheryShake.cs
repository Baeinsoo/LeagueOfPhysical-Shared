using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 시위를 잡고 버티는 동안 조준점이 멈추지 않고 작은 <b>8자</b>를 그리며 떠다닌다. 양궁에서
    /// 이걸 <b>플로트(float)</b>라 부르고, 코치들은 <i>없애려 하지 말고 그 안에서 놓으라</i>고
    /// 가르친다. 저격 게임의 sway도 같은 물건이다.
    ///
    /// <para><b>업계 표준 모양</b>: 호흡 주기(분당 15회 = 0.25Hz)로 도는 리사주 2:1 곡선.
    /// 가로가 세로의 두 배 속도라 누운 8자(∞)가 된다. 오래 버틸수록(피로) 커지고 많이
    /// 당길수록(부하) 커진다.</para>
    ///
    /// <para><b>왜 8자인가</b> — 닫힌 곡선이라 <b>외워서 탈 수 있으면서</b> 한자리에 멈추지
    /// 않는다. 원은 한 방향으로만 도는 게 티가 나고, 서로 안 맞는 주기를 섞으면 불규칙해서
    /// 아예 못 배운다(그게 이 파일의 이전 버전이었고, 실물에서 "운"으로 읽혔다).</para>
    ///
    /// <para><b>난수가 아니라 사인파</b>다. 틱마다 난수를 뽑으면 매끄럽지 않아 "손이 떨린다"가
    /// 아니라 화면이 고장 난 것처럼 보인다. 난수를 안 쓰므로 클·서 난수 소비 계약도 안 건드린다.</para>
    /// </summary>
    public static class ArcheryShake
    {
        /// <summary>
        /// 플로트가 한 바퀴 도는 빠르기(초당). <b>쉴 때의 호흡수(분당 15회)</b>에 맞춘 값이다 —
        /// 사람이 활을 버틸 때 조준점을 가장 크게 움직이는 것이 호흡이기 때문이다.
        ///
        /// <para>⚠️ <b>이 값이 이 파일의 핵심이다.</b> 전에는 0.7·1.7·8.7Hz 사인 셋을 더했는데
        /// 실물에서 "보정이 사실상 불가능"했다. 크기가 아니라 <b>속도</b>가 문제였다 — 사람이 보고
        /// 반응하는 데 0.2초가 걸리는데 1.7Hz(주기 0.59초)짜리는 반응했을 때 이미 반대로 가 있다.
        /// 사람이 눈으로 보고 따라갈 수 있는 건 대략 0.5Hz 아래뿐이다. 올릴 거면 그 한계를
        /// 먼저 떠올릴 것.</para>
        /// </summary>
        private const float FloatHz = 0.25f;

        /// <summary>
        /// 이만큼 당기고 있었을 때의 조준 흔들림(도). x는 좌우, y는 위아래이며 <b>위가 양수</b>다
        /// (조준 각도와 같은 좌표계 — 유니티 카메라의 x 회전은 반대다).
        /// </summary>
        /// <param name="heldSeconds">시위를 잡고 있었던 시간(초). 잡은 순간부터 흐른다 — 완전히
        /// 가만있는 구간은 없다. 0초에도 <see cref="ArcheryConfig.ShakeBaseRatio"/>만큼은 이미
        /// 실린다(당기고 있다면) — "0초=0"이 아니라 "0초=기준선"이다.</param>
        /// <param name="drawRatio">지금 얼마나 당겼나(0~1). 절반만 당긴 활은 절반만 떤다 —
        /// 실제 활시위를 버티는 힘이 절반이기 때문이다. 시뮬 상태(<see cref="ArcheryAim.DrawRatio"/>)에서
        /// 읽은 값만 넣을 것 — 클라이언트 전용 값(카메라 FOV 등)을 넣으면 클·서가 서로 다른
        /// 화살을 쏘게 된다.</param>
        public static Vector2 Offset(float heldSeconds, float drawRatio, int phaseSeed, ArcheryConfig config)
        {
            if (config.ShakeMaxDegrees <= 0f)
            {
                return Vector2.zero;
            }

            //  크기는 한 곳에서만 정한다 — 파형(아래 8자)과 크기를 같은 자리에서 섞으면
            //  시험이 크기만 따로 재지 못한다.
            float amplitude = AmplitudeDegrees(heldSeconds, drawRatio, config);
            if (amplitude <= 0f)
            {
                return Vector2.zero;
            }

            //  위상을 사람마다 다르게 준다 — 안 그러면 모두가 똑같이 흔들린다.
            //  0~1로 접어 넣는 것이 중요하다: 큰 수를 그대로 쓰면 사인에 넣는 값이 백만 단위가 되어
            //  float의 정밀도가 그 자리에서 1 근처로 떨어진다. 즉 흔들림이 뭉개진다.
            float phase = (phaseSeed & 0xFFFF) / 65536f;

            //  가로가 세로의 **두 배** 속도로 돌면 조준점이 누운 8자(∞)를 그린다. 이 2:1이
            //  업계에서 쓰는 모양이고 — 원을 그리면 한 방향으로만 도는 게 티가 나고, 서로
            //  안 맞는 주기를 섞으면 불규칙해서 못 배운다 — 8자는 닫힌 곡선이라 **외워서
            //  탈 수 있으면서** 같은 자리에 멈추지 않는다. 세로가 느린 쪽인 것도 이유가 있다:
            //  활을 버틸 때 조준점을 위아래로 미는 것이 호흡이다.
            float turns = heldSeconds * FloatHz + phase;
            float yaw = Mathf.Sin(2f * Mathf.PI * 2f * turns);
            float pitch = Mathf.Sin(2f * Mathf.PI * turns);

            return new Vector2(yaw * amplitude, pitch * amplitude);
        }

        /// <summary>
        /// 지금 플로트가 얼마나 큰가(도). <see cref="Offset"/>가 그리는 8자의 반지름이다 —
        /// 8자의 어디쯤인지(위상)와 무관한 <b>크기</b>만 준다.
        ///
        /// <para>시험이 이 값을 쓴다. 한 시점의 <see cref="Offset"/>는 8자 위 어디냐에 따라
        /// 0일 수도 있어서, "오래 잡으면 더 흔들린다" 같은 것을 재려면 시간을 훑어 최댓값을
        /// 찾아야 했다 — 주기가 길어지면 그 방식이 통째로 무너진다.</para>
        /// </summary>
        public static float AmplitudeDegrees(float heldSeconds, float drawRatio, ArcheryConfig config)
        {
            if (config.ShakeMaxDegrees <= 0f)
            {
                return 0f;
            }

            float ramped = heldSeconds - config.ShakeFreeSeconds;
            float fatigue = ramped <= 0f
                ? 0f
                : (config.ShakeRampSeconds > 0f ? Mathf.Clamp01(ramped / config.ShakeRampSeconds) : 1f);

            float baseRatio = Mathf.Clamp01(config.ShakeBaseRatio);
            return config.ShakeMaxDegrees
                 * (baseRatio + (1f - baseRatio) * fatigue)
                 * Mathf.Clamp01(drawRatio);
        }

        /// <summary>사람마다 다른 위상을 준다. 같은 사람이면 언제 물어도 같다.</summary>
        public static int PhaseSeedOf(string entityId)
        {
            //  프레임워크 공용 해시(GameFramework.Rng.Hashing.Fnv1a64)를 쓴다.
            //
            //  ⚠️ 이 안정성은 지금 **클·서 합의의 토대**다 — 예전엔 서버가 흔들림을 계산하지
            //  않아 상관없었지만(클라가 조준 각도에 흔들림을 이미 녹여 보내기만 했다), 지금은
            //  클라(예측)와 서버(권위) 둘 다 ArcheryAimSystem.Tick → DirectionFor를 불러 이
            //  함수를 *각자* 계산한다 — 와이어로 위상 값을 주고받지 않는다. 그래서 같은
            //  entityId를 넣으면 두 프로세스가 반드시 같은 위상을 내놔야 발사 방향이 일치한다.
            //  string.GetHashCode()로 바꾸면 런타임/버전마다(그리고 클·서가 서로 다른 실행이므로
            //  사실상 항상) 값이 달라질 수 있어 그 즉시 클·서가 서로 다른 화살을 쏘게 된다 —
            //  화면은 멀쩡해 보이고 reconciliation만 매 발마다 어긋난다.
            return (int)(GameFramework.Rng.Hashing.Fnv1a64(entityId) & 0x7FFFFFFF);
        }

    }
}
