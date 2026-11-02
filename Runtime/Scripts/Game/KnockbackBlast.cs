using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 충격파 폭탄이 터졌을 때 누가 어느 쪽으로 밀리는지 고르는 순수 식.
    /// 밀기 자체(위험도 +, 날림)는 탄에 맞을 때와 같은 <see cref="KnockbackRules.ApplyHit"/>를 쓴다.
    /// </summary>
    public static class KnockbackBlast
    {
        /// <summary>
        /// 반경 안(경계 포함)의 상대를 id 순으로 돌려준다. 쏜 사람 자신과 같은 팀은 뺀다.
        /// 방향은 터진 자리에서 몸 쪽이고, 터진 자리와 겹쳐 방향이 없으면 탄 방향을 쓴다.
        /// </summary>
        public static List<(string id, Vector2 direction)> Resolve(
            Vector2 center, Vector2 shotDirection, float radius,
            IReadOnlyList<(string id, Vector2 position, int team)> bodies,
            string shooterId, int shooterTeam)
        {
            var pushes = new List<(string, Vector2)>();
            var fallback = shotDirection.sqrMagnitude > 0f ? shotDirection.normalized : Vector2.right;
            for (int i = 0; i < bodies.Count; i++)
            {
                var body = bodies[i];
                if (body.id == shooterId || KnockbackTeams.SameTeam(shooterTeam, body.team))
                {
                    continue;
                }

                var offset = body.position - center;
                if (offset.sqrMagnitude > radius * radius)
                {
                    continue;
                }

                pushes.Add((body.id, offset.sqrMagnitude > 1e-8f ? offset.normalized : fallback));
            }

            pushes.Sort((a, b) => string.CompareOrdinal(a.Item1, b.Item1));
            return pushes;
        }
    }
}
