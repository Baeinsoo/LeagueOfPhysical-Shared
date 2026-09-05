using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 죽은 고도로부터 되돌아갈 선반을 고른다. <b>저장할 상태가 없다</b> — 코스가 아래 한 방향이라
    /// y 하나로 "어디까지 왔나"가 정해진다 — 지나온 선반을 따로 기록할 필요가 없다.
    /// </summary>
    public static class SkydiveCheckpoints
    {
        /// <summary>
        /// 마지막으로 지나온 선반의 고도. 지나온 선반이 없으면 <paramref name="spawnY"/>.
        ///
        /// <para>선반 고도에 정확히 있는 경우는 <b>아직 안 지난 것</b>으로 본다 — 지났다고 보면
        /// 제자리에 부활해 방금 맞은 레이저에 곧바로 다시 걸린다.</para>
        /// </summary>
        public static float LastPassedShelfY(float deathY, IReadOnlyList<float> shelfYs, float spawnY)
        {
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < shelfYs.Count; i++)
            {
                float y = shelfYs[i];
                if (y > deathY && y < best)
                {
                    best = y;
                    found = true;
                }
            }
            return found ? best : spawnY;
        }
    }
}
