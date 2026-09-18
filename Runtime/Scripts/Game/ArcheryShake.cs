using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 오래 당기고 있으면 조준이 흔들린다. 미리 당겨놓고 무한정 기다리지 못하게 하는 대가다.
    ///
    /// <para><b>난수가 아니라 사인파</b>로 만든다. 틱마다 난수를 뽑으면 값이 매끄럽지 않아 지직거려서
    /// "손이 떨린다"가 아니라 화면이 고장 난 것처럼 보인다. 주기가 서로 안 맞는 사인파 둘을 더하면
    /// 규칙이 눈에 안 보이면서도 매끄럽다. 난수를 안 쓰므로 클·서 난수 소비 계약도 안 건드린다.</para>
    /// </summary>
    public static class ArcheryShake
    {
        //  두 주기가 서로 나누어떨어지지 않아야 같은 모양이 반복되지 않는다(초당 진동 수).
        private const float SlowHz = 0.7f;
        private const float FastHz = 1.7f;

        //  위아래는 좌우와 다른 주기로 움직여야 원을 그리지 않고 불규칙해 보인다.
        private const float SlowHzPitch = 0.9f;
        private const float FastHzPitch = 2.1f;

        /// <summary>빠른 쪽이 전체에서 차지하는 몫. 느린 흔들림 위에 잔떨림이 얹힌 모양이 된다.</summary>
        private const float FastWeight = 0.35f;

        /// <summary>
        /// 이만큼 당기고 있었을 때의 조준 흔들림(도). x는 좌우, y는 위아래이며 <b>위가 양수</b>다
        /// (조준 각도와 같은 좌표계 — 유니티 카메라의 x 회전은 반대다).
        /// </summary>
        public static Vector2 Offset(float heldSeconds, int phaseSeed, ArcheryConfig config)
        {
            float ramped = heldSeconds - config.ShakeFreeSeconds;
            if (ramped <= 0f || config.ShakeMaxDegrees <= 0f)
            {
                return Vector2.zero;
            }

            float grow = config.ShakeRampSeconds > 0f
                ? Mathf.Clamp01(ramped / config.ShakeRampSeconds)
                : 1f;
            float amplitude = config.ShakeMaxDegrees * grow;

            //  위상을 사람마다 다르게 준다 — 안 그러면 모두가 똑같이 흔들린다.
            //  0~1로 접어 넣는 것이 중요하다: 큰 수를 그대로 쓰면 사인에 넣는 값이 백만 단위가 되어
            //  float의 정밀도가 그 자리에서 1 근처로 떨어진다. 즉 흔들림이 뭉개진다.
            float phase = (phaseSeed & 0xFFFF) / 65536f;

            float yaw = Wave(heldSeconds, phase, SlowHz, FastHz);
            float pitch = Wave(heldSeconds, phase + 1.7f, SlowHzPitch, FastHzPitch);

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

        //  느린 파 + 빠른 파. 합이 -1~1을 넘지 않도록 몫을 나눠 둔다.
        private static float Wave(float seconds, float phase, float slowHz, float fastHz)
        {
            float slow = Mathf.Sin((seconds * slowHz + phase) * 2f * Mathf.PI);
            float fast = Mathf.Sin((seconds * fastHz + phase * 2f) * 2f * Mathf.PI);
            return slow * (1f - FastWeight) + fast * FastWeight;
        }
    }
}
