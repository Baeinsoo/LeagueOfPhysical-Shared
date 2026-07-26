using System;
using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 예측 상태이상 목록과 서버 권위 목록이 같은 시점에서 다른지 판정한다(순수, 무할당).
    /// <see cref="GameFramework.Netcode.ReconcileGate.ShouldReconcile"/>의 상태이상 짝 — 위치는
    /// 거리 임계값, 이쪽은 목록 비교로 롤백 필요 여부를 판정한다.
    /// <para>id 집합 차이뿐 아니라 만료틱·스택수 차이도 봐야 한다 — 몬스터가 쿨다운 없이 계속 때리면
    /// 서버가 같은 효과를 "재적용"해 만료틱만 계속 밀리는데, id 집합은 그대로라 id만 비교하면 이
    /// 발산을 못 잡는다(원래 버그).</para>
    /// </summary>
    public static class StatusEffectReconcileGate
    {
        public static bool ShouldReconcile(
            List<ActiveEffect> predictedEffects,
            List<ActiveEffect> serverEffects,
            Func<int, StatusEffectData?> resolver)
        {
            int predictedCount = predictedEffects != null ? predictedEffects.Count : 0;
            int serverCount = serverEffects != null ? serverEffects.Count : 0;

            for (int i = 0; i < serverCount; i++)
            {
                var serverEffect = serverEffects[i];
                int foundIndex = -1;
                for (int j = 0; j < predictedCount; j++)
                {
                    if (predictedEffects[j].EffectId == serverEffect.EffectId)
                    {
                        foundIndex = j;
                        break;
                    }
                }

                if (foundIndex < 0)
                {
                    // 클라가 모르는 서버 효과 id는 StatusEffectSystem.ApplyAuthoritativeState도 애초에
                    // 걸지 않으므로(구버전 데이터 등) 불일치로 치지 않는다 — 안 그러면 영원히 해소되지
                    // 않는 롤백 루프가 된다.
                    if (resolver(serverEffect.EffectId) == null)
                    {
                        continue;
                    }
                    return true;
                }

                var predictedEffect = predictedEffects[foundIndex];
                if (predictedEffect.ExpireTick != serverEffect.ExpireTick ||
                    predictedEffect.StackCount != serverEffect.StackCount)
                {
                    return true;
                }
            }

            // 예측 쪽에만 남은 효과(서버가 이미 뗀 것 등)는 불일치로 본다.
            for (int i = 0; i < predictedCount; i++)
            {
                int predictedEffectId = predictedEffects[i].EffectId;
                bool foundInServer = false;
                for (int j = 0; j < serverCount; j++)
                {
                    if (serverEffects[j].EffectId == predictedEffectId)
                    {
                        foundInServer = true;
                        break;
                    }
                }
                if (!foundInServer)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
