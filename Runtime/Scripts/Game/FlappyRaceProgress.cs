using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Flappy Race의 완주 판정. 물리도 엔티티도 모르고 <b>x 좌표만</b> 받아 답한다.
    /// 코스가 +x 한 줄이고 새는 뒤로 가지 않아서 한 축이면 충분하다.
    /// (판치기의 진행 규칙 <c>PanchigiTurn</c>은 서버 전용이라 서버(LeagueOfPhysical-Server)로
    /// 돌아갔다 — 이건 반대로, x 좌표 비교뿐인 순수 함수라 클라도 언젠가 같은 답을 로컬로 내야 할
    /// 수 있어 공용에 남긴다.)
    /// </summary>
    public class FlappyRaceProgress
    {
        private readonly float finishX;

        public FlappyRaceProgress(float finishX)
        {
            this.finishX = finishX;
        }

        /// <summary>결승선 위에 정확히 선 것도 통과로 본다 — 선을 밟은 순간이 통과다.</summary>
        public bool HasFinished(float x) => x >= finishX;

        /// <summary>
        /// 남아 있는 새 전원이 통과했나. <b>비어 있으면 false</b> — 아무도 없는 판을
        /// "전원 완주"로 끝내면 스폰 직전(아직 새가 없을 때) 시작하자마자 끝난다.
        /// </summary>
        public bool AllFinished(IReadOnlyList<float> xs)
        {
            if (xs.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < xs.Count; i++)
            {
                if (HasFinished(xs[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
