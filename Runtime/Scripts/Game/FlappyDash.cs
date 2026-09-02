namespace LOP
{
    /// <summary>
    /// 대시 게이지와, 대시가 끝나기까지 남은 시간. 데이터만 갖는다 — 충전·발동·소진은
    /// <see cref="FlappyDashSystem"/>이 한다(<see cref="FlappyStun"/>과 같은 짝).
    /// </summary>
    public class FlappyDash : GameFramework.World.Component
    {
        /// <summary>
        /// 출발할 때 이미 차 있는 양. 0에서 시작하면 첫 대시까지 한참을 기다려야 해서 둔 값이다.
        /// 튜닝 대상이 아니라 컨피그로 빼지 않았다 — 필요해지면 그때 뺀다.
        /// </summary>
        public const float InitialCharge = 0.6f;

        /// <summary>0~1. 1이면 발동할 수 있다.</summary>
        public float Charge = InitialCharge;

        /// <summary>대시가 끝나기까지 남은 시간(초). 0이면 대시 중이 아니다.</summary>
        public float DashRemaining;
    }
}
