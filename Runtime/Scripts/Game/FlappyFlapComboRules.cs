using System.Globalization;
using System.Text;

namespace LOP
{
    /// <summary>
    /// 연속 날갯짓 보상(spec 2026-10-27-flappy-combo-dash-reward, 11-02 개정). 맵 마커(<see cref="FlappyMapRules"/>)가 맵마다 바꾼다.
    /// <para><b>게이지:</b> 연속 수가 <see cref="Step"/>(기본 10)의 배수가 되는 날갯짓마다 표의 k번째 칸을 준다(k = 수/Step).
    /// 표가 끝나면 꼬리 값(<see cref="TailGain"/>)을 계속 준다. 표는 고정 숫자다 — 실행 중에 거듭제곱을 계산하지 않아
    /// 클·서·IL2CPP가 비트까지 같다. 칸이 뒤로 갈수록 줄지 않아(TryCreate가 지킨다) 끊지 않고 잇는 쪽이 박고 다시 쌓는 쪽보다 늘 많다.</para>
    /// <para><b>고비(화면):</b> 10·30·50·100, 그 뒤 50마다(150·200…)에만 한마디·고리·화음. 게이지와 따로다 — 화면 연출만 쓴다.
    /// 코인·서버 통계는 둘 다 안 본다(서버 <c>FlappyComboStatKeys</c>).</para>
    /// <para>보상은 수 하나에 묶인 값이라 되감아 다시 굴려도 수와 게이지가 짝으로 되돌아가 한 번이다(<see cref="FlappySavedState"/>).</para>
    /// <para>한 번 만들면 안 바뀐다 — 판 도중에 규칙이 바뀌면 클·서가 갈린다.</para>
    /// </summary>
    public sealed class FlappyFlapComboRules
    {
        /// <summary>기본 보상 간격(사용자 결정 11-02).</summary>
        public const int DefaultStep = 10;

        /// <summary>표가 끝난 뒤(기본 160부터) 간격마다 주는 칸.</summary>
        public const float DefaultTailGain = 0.25f;

        /// <summary>화면 고비 — 마지막(100) 뒤로는 <see cref="MilestoneRepeatEvery"/>마다 마지막 고비와 같은 연출.</summary>
        private static readonly int[] Milestones = { 10, 30, 50, 100 };

        public const int MilestoneRepeatEvery = 50;

        public static readonly FlappyFlapComboRules Default = new FlappyFlapComboRules(DefaultStep, DefaultGains(), DefaultTailGain);

        /// <summary>보상 없는 맵 — 마커 간격이 0. 세기와 "+N"은 그대로, 고비 연출은 없다.</summary>
        public static readonly FlappyFlapComboRules Off = new FlappyFlapComboRules(0, new float[0], 0f);

        private readonly int _step;
        private readonly float[] _gains;
        private readonly float _tail;

        private FlappyFlapComboRules(int step, float[] gains, float tail)
        {
            _step = step;
            _gains = gains;
            _tail = tail;
        }

        /// <summary>
        /// 기본 표(칸) — 10·20·…·150번째 날갯짓. 대략 걸음마다 ×1.1, 소수 둘째 자리 고정(사용자 결정 11-02 — 초반엔 옛 10/25/50/100보다 덜 준다).
        /// 누적 50→0.37, 100→0.97, 150→1.93, 200→3.18. 마커 필드 초기값으로도 쓰여 부를 때마다 새 배열이다.
        /// </summary>
        public static float[] DefaultGains() => new[]
        {
            0.06f, 0.07f, 0.07f, 0.08f, 0.09f,
            0.10f, 0.11f, 0.12f, 0.13f, 0.14f,
            0.16f, 0.17f, 0.19f, 0.21f, 0.23f,
        };

        public int Step => _step;

        public float TailGain => _tail;

        public bool Rewards => _step > 0;

        /// <summary>
        /// 마커 값으로 규칙을 만든다. 간격 0이면 <see cref="Off"/>. 간격 음수·표 없음(null이나 빈 표 — 인스펙터에서 비우면 []다)·0~2칸 밖 칸·뒤로 가며 줄어드는 칸·
        /// 표 끝 칸보다 작은 꼬리면 false와 <see cref="Default"/> — 줄어드는 칸은 "박고 다시 쌓기"가 나아지는 틈이다.
        /// </summary>
        public static bool TryCreate(int step, float[] gains, float tailGain, out FlappyFlapComboRules rules)
        {
            rules = Default;
            if (step == 0)
            {
                rules = Off;
                return true;
            }
            if (step < 0 || gains == null || gains.Length == 0)
            {
                return false;
            }
            float previous = 0f;
            for (int i = 0; i < gains.Length; i++)
            {
                //  NaN은 어떤 비교에도 거짓이라 "앞 칸 이상이고 2칸 이하인가"로 물어야 걸러진다.
                if ((gains[i] >= previous && gains[i] <= FlappyDash.MaxCharge) == false)
                {
                    return false;
                }
                previous = gains[i];
            }
            if ((tailGain >= previous && tailGain <= FlappyDash.MaxCharge) == false)
            {
                return false;
            }
            rules = new FlappyFlapComboRules(step, (float[])gains.Clone(), tailGain);
            return true;
        }

        /// <summary>연속 수가 <paramref name="count"/>가 되는 날갯짓에 주는 게이지(칸). 간격의 배수가 아니면 0.</summary>
        public float Gain(int count)
        {
            if (_step <= 0 || count <= 0 || count % _step != 0)
            {
                return 0f;
            }
            int k = count / _step;
            return k <= _gains.Length ? _gains[k - 1] : _tail;
        }

        /// <summary>
        /// 화면 고비 번호(0부터) — 10·30·50·100이 0·1·2·3, 100 뒤 50마다도 3. 아니면 −1. 보상 없는 맵은 늘 −1.
        /// 한마디·고리·화음만 이것을 본다(게이지는 <see cref="Gain"/>).
        /// </summary>
        public int MilestoneIndex(int count)
        {
            if (Rewards == false)
            {
                return -1;
            }
            for (int i = 0; i < Milestones.Length; i++)
            {
                if (Milestones[i] == count)
                {
                    return i;
                }
            }
            int last = Milestones[Milestones.Length - 1];
            return count > last && (count - last) % MilestoneRepeatEvery == 0 ? Milestones.Length - 1 : -1;
        }

        /// <summary>로그 한 줄. 예: "10마다 +0.06 +0.07 … +0.23 그뒤 +0.25". 보상이 없으면 "없음".</summary>
        public string Describe()
        {
            if (Rewards == false)
            {
                return "없음";
            }
            var text = new StringBuilder();
            text.Append(_step.ToString(CultureInfo.InvariantCulture)).Append("마다");
            for (int i = 0; i < _gains.Length; i++)
            {
                text.Append(" +").Append(_gains[i].ToString("0.###", CultureInfo.InvariantCulture));
            }
            text.Append(" 그뒤 +").Append(_tail.ToString("0.###", CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }
}
