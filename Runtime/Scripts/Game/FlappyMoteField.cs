using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 빛 알갱이 무더기 하나. 씬 마커(<see cref="FlappyMoteCluster"/>)가 만들어 필드에 넣고, 사라질 때 같은 것을 뺀다.
    /// 알갱이는 맵 위의 점이다 — 움직이지 않고, 먹어도 줄지 않는다.
    /// </summary>
    public class FlappyMoteSet
    {
        public readonly float[] Xs;
        public readonly float[] Ys;

        /// <summary>금빛 큰 무더기인가. 판정에는 안 쓰고 그림만 다르게 그린다.</summary>
        public readonly bool Big;

        /// <summary>
        /// 붙은 관문의 틈 중심(x, y)과 틈의 반 높이. 그림만 쓴다 — 알갱이가 어느 끝(위·아래)에 붙었는지와
        /// 그 끝의 높이를 알아야 빛 띠를 벽에 붙여 그릴 수 있다. 반 높이 0이면 모름(띠를 안 그린다).
        /// </summary>
        public readonly float OriginX, OriginY, GapHalf;

        /// <summary>
        /// 움직이는 관문(승강기) 위 무더기면 그 승강기의 진폭·주기·위상. 판정 높이 = 원래 높이 +
        /// <see cref="FlappyLiftCurve.OffsetAt"/> — 승강기가 그 틱에 쓰는 바로 그 식이라 클·서·되감기가 같은 높이를 본다.
        /// 주기 0이면 고정 무더기.
        /// </summary>
        public readonly float MoveAmplitude, MovePeriod, MovePhase;

        /// <summary>그림용 지금 오프셋(승강기 케이지의 지금 높이). 판정에는 안 쓴다. 없으면 0.</summary>
        public System.Func<float> VisualOffset;

        public bool Moving => MovePeriod > 0f;

        public FlappyMoteSet(float[] xs, float[] ys, bool big, float originX = 0f, float originY = 0f, float gapHalf = 0f,
                             float moveAmplitude = 0f, float movePeriod = 0f, float movePhase = 0f)
        {
            Xs = xs;
            Ys = ys;
            Big = big;
            OriginX = originX;
            OriginY = originY;
            GapHalf = gapHalf;
            MoveAmplitude = moveAmplitude;
            MovePeriod = movePeriod;
            MovePhase = movePhase;
        }

        /// <summary>틱 <paramref name="tick"/>에서 이 무더기가 원래 자리에서 오르내린 높이(m).</summary>
        public float OffsetAt(long tick, float tickSeconds)
            => Moving ? FlappyLiftCurve.OffsetAt(MoveAmplitude, MovePeriod, MovePhase, tick, tickSeconds) : 0f;

        public float CurrentVisualOffset => Moving && VisualOffset != null ? VisualOffset() : 0f;

        public int Count => Xs.Length;
    }

    /// <summary>
    /// 이 판에 놓인 빛 알갱이 전부(<see cref="FlappyBoostPadField"/>와 같은 모양).
    ///
    /// <para><b>먹는 판정은 "알갱이의 세로줄을 지나는 순간"이다.</b> 새가 한 틱에 x₀ → x₁로 갔고 알갱이의 x가
    /// 그 사이(x₀ &lt; x ≤ x₁)면, 그 순간의 새 높이와 알갱이 높이 차를 본다. 새는 뒤로 가지 않으므로 같은
    /// 알갱이를 두 번 지나는 일이 없다 — "누가 무엇을 먹었나"를 기억하지 않아도 한 번만 먹고, 그래서
    /// 되감아 다시 돌려도 같은 답이 나온다.</para>
    ///
    /// <para>물리 질의를 쓰지 않는다. 산술로만 센다(패드와 같은 이유 — 롤백 재생에서 물리를 안 돌린다).</para>
    /// </summary>
    public class FlappyMoteField
    {
        private readonly List<FlappyMoteSet> _sets = new List<FlappyMoteSet>();

        // 모든 무더기의 알갱이를 x 순으로 합친 것. 무더기가 들고 날 때만 다시 만든다.
        private float[] _xs = System.Array.Empty<float>();
        private float[] _ys = System.Array.Empty<float>();
        private FlappyMoteSet[] _owner = System.Array.Empty<FlappyMoteSet>();   // 알갱이마다 속한 무더기(움직임을 묻는다)
        private bool _dirty;

        public IReadOnlyList<FlappyMoteSet> Sets => _sets;

        public int Count
        {
            get
            {
                Rebuild();
                return _xs.Length;
            }
        }

        public void Add(FlappyMoteSet set)
        {
            if (set == null || _sets.Contains(set))
            {
                return;
            }
            _sets.Add(set);
            _dirty = true;
        }

        public bool Remove(FlappyMoteSet set)
        {
            bool removed = _sets.Remove(set);
            _dirty |= removed;
            return removed;
        }

        /// <summary>
        /// (x0, y0) → (x1, y1)로 움직이는 동안 세로줄을 지난 알갱이 중, 그 순간 새 중심과의 높이 차가
        /// <paramref name="reachY"/> 이하인 것의 수. <paramref name="hits"/>를 주면 먹은 알갱이 자리를 담는다.
        /// </summary>
        public int CountCrossed(float x0, float y0, float x1, float y1, float reachY, List<(float x, float y)> hits = null)
            => CountWith(x0, y0, x1, y1, reachY, hits, set => 0f);

        /// <summary>
        /// 시뮬용: 움직이는 무더기는 틱 <paramref name="tick"/>의 높이로 본다. <paramref name="hits"/>에는 원래 자리를 담는다
        /// (그림이 그 알갱이를 찾을 수 있게).
        /// </summary>
        public int CountCrossed(float x0, float y0, float x1, float y1, float reachY, long tick, float tickSeconds,
                                List<(float x, float y)> hits = null)
            => CountWith(x0, y0, x1, y1, reachY, hits, set => set.OffsetAt(tick, tickSeconds));

        /// <summary>화면용: 움직이는 무더기는 지금 그려진 오프셋(<see cref="FlappyMoteSet.VisualOffset"/>)으로 본다.</summary>
        public int CountCrossedVisual(float x0, float y0, float x1, float y1, float reachY, List<(float x, float y)> hits = null)
            => CountWith(x0, y0, x1, y1, reachY, hits, set => set.CurrentVisualOffset);

        private int CountWith(float x0, float y0, float x1, float y1, float reachY, List<(float x, float y)> hits,
                          System.Func<FlappyMoteSet, float> offsetOf)
        {
            if (x1 <= x0)
            {
                return 0;
            }
            Rebuild();
            int count = 0;
            float span = x1 - x0;
            for (int i = FirstAfter(x0); i < _xs.Length && _xs[i] <= x1; i++)
            {
                float y = y0 + (y1 - y0) * ((_xs[i] - x0) / span);
                var owner = _owner[i];
                float moteY = _ys[i] + (owner.Moving ? offsetOf(owner) : 0f);
                if (System.Math.Abs(y - moteY) <= reachY)
                {
                    count++;
                    hits?.Add((_xs[i], _ys[i]));
                }
            }
            return count;
        }

        // x가 처음으로 x0보다 큰 자리(이진 탐색).
        private int FirstAfter(float x0)
        {
            int lo = 0, hi = _xs.Length;
            while (lo < hi)
            {
                int mid = (lo + hi) >> 1;
                if (_xs[mid] <= x0)
                {
                    lo = mid + 1;
                }
                else
                {
                    hi = mid;
                }
            }
            return lo;
        }

        private void Rebuild()
        {
            if (_dirty == false)
            {
                return;
            }
            _dirty = false;
            var all = new List<(float x, float y, FlappyMoteSet set)>();
            for (int s = 0; s < _sets.Count; s++)
            {
                var set = _sets[s];
                for (int i = 0; i < set.Count; i++)
                {
                    all.Add((set.Xs[i], set.Ys[i], set));
                }
            }
            //  x가 같으면 y로 — 등록 순서가 바뀌어도 같은 배열이 되게.
            all.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            _xs = new float[all.Count];
            _ys = new float[all.Count];
            _owner = new FlappyMoteSet[all.Count];
            for (int i = 0; i < all.Count; i++)
            {
                _xs[i] = all[i].x;
                _ys[i] = all[i].y;
                _owner[i] = all[i].set;
            }
        }
    }
}
