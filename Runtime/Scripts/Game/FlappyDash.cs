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

        /// <summary>
        /// 게이지를 몇 칸까지 쌓아 둘 수 있나. 2인 이유는 <b>다이브의 보상이 끊기지 않게</b>
        /// 하기 위해서다 — 상한이 1이면 게이지가 차는 순간 낮게 날 이유가 사라져서, 이 게임의
        /// 리스크·리워드가 그 자리에서 꺼진다.
        /// <para><see cref="InitialCharge"/>와 같은 이유로 컨피그에 빼지 않았다 — 이 값을 바꾸면
        /// 버튼 UI(칸 수를 숫자로 그린다)도 같이 바뀌므로 튜닝 손잡이가 아니다.</para>
        /// </summary>
        public const float MaxCharge = 2f;

        /// <summary>0~<see cref="MaxCharge"/>. 1 이상이면 한 칸을 써서 발동할 수 있다.</summary>
        public float Charge = InitialCharge;

        /// <summary>대시가 끝나기까지 남은 시간(초). 0이면 대시 중이 아니다.</summary>
        public float DashRemaining;
    }
}
