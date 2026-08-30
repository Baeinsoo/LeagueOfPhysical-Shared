using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// Skydive의 완주 판정. 물리도 엔티티도 모르고 <b>y 좌표만</b> 받아 답한다.
    /// 코스가 아래 한 방향이고 떨어진 사람은 다시 올라가지 않아서 한 축이면 충분하다.
    /// (<see cref="FlappyRaceProgress"/>와 같은 자리, 반대 축이다.)
    /// </summary>
    public class SkydiveProgress
    {
        private readonly float finishY;

        public SkydiveProgress(float finishY)
        {
            this.finishY = finishY;
        }

        /// <summary>결승 고도에 정확히 있는 것도 통과로 본다 — 선을 밟은 순간이 통과다.</summary>
        public bool HasFinished(float y) => y <= finishY;

        /// <summary>
        /// 남아 있는 사람 전원이 내려왔나. <b>비어 있으면 false</b> — 아무도 없는 판을
        /// "전원 완주"로 끝내면 스폰 직전에 판이 끝난다.
        /// </summary>
        public bool AllFinished(IReadOnlyList<float> ys)
        {
            if (ys.Count == 0)
            {
                return false;
            }
            for (int i = 0; i < ys.Count; i++)
            {
                if (HasFinished(ys[i]) == false)
                {
                    return false;
                }
            }
            return true;
        }
    }
}
