namespace LOP
{
    /// <summary>
    /// 시전 진행도 환산 커널(순수). 페이즈 경계가 절대 틱이므로 "지금 몇 틱"만 알면 산수로 풀린다 —
    /// 클라는 원격 어빌리티를 시뮬하지 않고 이 함수로 그림만 맞춘다.
    /// 컨텍스트 없는 순수 계산이라 static이며 *System 이름을 붙이지 않는다.
    /// </summary>
    public static class AbilityPlayback
    {
        /// <summary>
        /// <paramref name="currentTick"/> 시점의 페이즈와 전체 진행도(0~1)를 구한다.
        /// 시전 중이 아니면 false(출력은 Ready/0).
        /// </summary>
        /// <param name="totalTicks">startup+active+recovery 합 — 발동 틱을 역산하는 데 쓴다.</param>
        public static bool Solve(in ActiveAbility active, long currentTick, long totalTicks,
                                 out AbilityPhase phase, out float normalizedTime)
        {
            phase = AbilityPhase.Ready;
            normalizedTime = 0f;

            if (totalTicks <= 0)
            {
                return false;
            }

            long activationTick = active.RecoveryEndTick - totalTicks;
            if (currentTick < activationTick || currentTick >= active.RecoveryEndTick)
            {
                return false;
            }

            normalizedTime = (float)(currentTick - activationTick) / totalTicks;

            // 경계 틱은 다음 페이즈에 속한다 — AbilitySystem.Tick의 `currentTick >= 경계` 전진과 같은 규칙.
            if (currentTick < active.StartupEndTick)
            {
                phase = AbilityPhase.Startup;
            }
            else if (currentTick < active.ActiveEndTick)
            {
                phase = AbilityPhase.Active;
            }
            else
            {
                phase = AbilityPhase.Recovery;
            }
            return true;
        }
    }
}
