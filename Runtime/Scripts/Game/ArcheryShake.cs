using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 시위를 잡고 있는 동안 조준이 흔들린다 — 실제 손이 활을 당기고 있을 때처럼, 잡은 그
    /// 순간부터 떨리고 오래 버틸수록(피로) 더 떨리고 많이 당길수록(부하) 더 떨린다.
    ///
    /// <para><b>난수가 아니라 사인파</b>로 만든다. 틱마다 난수를 뽑으면 값이 매끄럽지 않아 지직거려서
    /// "손이 떨린다"가 아니라 화면이 고장 난 것처럼 보인다. 주기가 서로 안 맞는 사인파 여러 개를
    /// 더하면 규칙이 눈에 안 보이면서도 매끄럽다. 난수를 안 쓰므로 클·서 난수 소비 계약도 안 건드린다.</para>
    /// </summary>
    public static class ArcheryShake
    {
        //  두 주기가 서로 나누어떨어지지 않아야 같은 모양이 반복되지 않는다(초당 진동 수).
        private const float SlowHz = 0.7f;
        private const float FastHz = 1.7f;

        //  위아래는 좌우와 다른 주기로 움직여야 원을 그리지 않고 불규칙해 보인다.
        private const float SlowHzPitch = 0.9f;
        private const float FastHzPitch = 2.1f;

        //  생리학적으로 사람 손은 느린 드리프트 위에 8~10Hz대의 아주 작은 잔떨림이 항상 얹혀
        //  있다(생리적 손떨림, physiological tremor). 그게 없으면 "팔이 크게 흔들리는" 것처럼만
        //  보이고 "손으로 쥐고 있다"는 느낌이 안 난다. 느린/빠른 성분과도 정수배로 안 맞게
        //  8.7/9.4로 어긋나게 잡아 겹침이 안 생기게 한다.
        private const float TremorHz = 8.7f;
        private const float TremorHzPitch = 9.4f;

        /// <summary>빠른(1.7Hz대) 성분이 전체에서 차지하는 몫.</summary>
        private const float FastWeight = 0.30f;

        /// <summary>
        /// 손떨림(8~10Hz) 성분의 몫. 나머지 둘보다 <b>훨씬 작게</b> 잡는다 — 이건 "팔이
        /// 흔들리는 큰 움직임"이 아니라 "쥔 손이 미세하게 떠는" 결이라, 크게 넣으면 오히려
        /// 지직거리는 잡음처럼 보인다.
        /// </summary>
        private const float TremorWeight = 0.08f;

        //  세 몫을 합쳐 1을 넘지 않게 한다 — 그래야 사인이 전부 같은 부호로 겹쳐도 진폭이
        //  config.ShakeMaxDegrees를 넘지 않는다는 보장이 선다.
        private const float SlowWeight = 1f - FastWeight - TremorWeight;

        /// <summary>
        /// 이만큼 당기고 있었을 때의 조준 흔들림(도). x는 좌우, y는 위아래이며 <b>위가 양수</b>다
        /// (조준 각도와 같은 좌표계 — 유니티 카메라의 x 회전은 반대다).
        /// </summary>
        /// <param name="heldSeconds">시위를 잡고 있었던 시간(초). 잡은 순간부터 흐른다 — 완전히
        /// 가만있는 구간은 없다(0초에는 자연히 진폭도 0이라 매끄럽게 이어질 뿐이다).</param>
        /// <param name="drawRatio">지금 얼마나 당겼나(0~1). 절반만 당긴 활은 절반만 떤다 —
        /// 실제 활시위를 버티는 힘이 절반이기 때문이다. 시뮬 상태(<see cref="ArcheryAim.DrawRatio"/>)에서
        /// 읽은 값만 넣을 것 — 클라이언트 전용 값(카메라 FOV 등)을 넣으면 클·서가 서로 다른
        /// 화살을 쏘게 된다.</param>
        public static Vector2 Offset(float heldSeconds, float drawRatio, int phaseSeed, ArcheryConfig config)
        {
            //  free=0이면 ramped는 heldSeconds와 같아진다 — 0으로 나누지 않고, heldSeconds가
            //  0에 가까워질수록 grow도 0에 가까워지므로 여기서 끊기는 값이 없다.
            float ramped = heldSeconds - config.ShakeFreeSeconds;
            if (ramped <= 0f || config.ShakeMaxDegrees <= 0f)
            {
                return Vector2.zero;
            }

            float grow = config.ShakeRampSeconds > 0f
                ? Mathf.Clamp01(ramped / config.ShakeRampSeconds)
                : 1f;
            //  피로(시간)와 부하(당긴 정도)를 각각의 몫으로 곱한다 — 둘 중 하나만 있어도
            //  실제 손떨림이 안 된다. 살짝 당겨 잠깐 버티는 건 거의 안 떨리고, 완전히 당겨
            //  오래 버티면 두 배로 심해진다.
            float amplitude = config.ShakeMaxDegrees * grow * Mathf.Clamp01(drawRatio);

            //  위상을 사람마다 다르게 준다 — 안 그러면 모두가 똑같이 흔들린다.
            //  0~1로 접어 넣는 것이 중요하다: 큰 수를 그대로 쓰면 사인에 넣는 값이 백만 단위가 되어
            //  float의 정밀도가 그 자리에서 1 근처로 떨어진다. 즉 흔들림이 뭉개진다.
            float phase = (phaseSeed & 0xFFFF) / 65536f;

            float yaw = Wave(heldSeconds, phase, SlowHz, FastHz, TremorHz);
            float pitch = Wave(heldSeconds, phase + 1.7f, SlowHzPitch, FastHzPitch, TremorHzPitch);

            return new Vector2(yaw * amplitude, pitch * amplitude);
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

        //  느린 파 + 빠른 파 + 손떨림 파. 합이 -1~1을 넘지 않도록 몫을 나눠 둔다.
        private static float Wave(float seconds, float phase, float slowHz, float fastHz, float tremorHz)
        {
            float slow = Mathf.Sin((seconds * slowHz + phase) * 2f * Mathf.PI);
            float fast = Mathf.Sin((seconds * fastHz + phase * 2f) * 2f * Mathf.PI);
            float tremor = Mathf.Sin((seconds * tremorHz + phase * 3f) * 2f * Mathf.PI);
            return slow * SlowWeight + fast * FastWeight + tremor * TremorWeight;
        }
    }
}
