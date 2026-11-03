namespace LOP
{
    public static class CardRushProgress
    {
        /// <summary>못 들어온 사람 순위용 진행 값: 칸이 많을수록, 같은 칸이면 먼저 도착할수록 크다.</summary>
        //  나눗셈은 double로 한다 — 틱이 커지면 float 나눗셈이 이웃 틱을 같은 값으로 뭉갠다. 다만 결과가 float라
        //  칸 39 근처(최소 간격 약 3.8e-6)에서 이웃 틱이 갈리려면 horizonTick이 대략 13만 틱(50Hz로 43분) 안이어야 한다.
        //  지금 시험 값(7500)에선 float로 바꿔도 안 뭉개진다(사보타주로 확인) — double은 방어용이다.
        public static float ForRanking(int step, long moveStartTick, long horizonTick)
            => (float)(step + 0.5 * (1.0 - moveStartTick / (double)(horizonTick + 1)));
    }
}
