using System.Globalization;
using System.Text;

namespace LOP
{
    /// <summary>
    /// 연속 날갯짓 고비와 그때 주는 대시 게이지(spec 2026-10-27-flappy-combo-dash-reward). 맵 마커(<see cref="FlappyMapRules"/>)가 맵마다 바꾼다.
    /// <para>고비는 "연속 수가 딱 그 값이 되는 날갯짓"이다. 수는 한 번에 1씩만 오르니 한 연속에서 고비마다 꼭 한 번 걸리고,
    /// 되감아 다시 굴려도 수와 게이지가 짝으로 되돌아가 한 번이다(<see cref="FlappySavedState"/>).</para>
    /// <para>한 번 만들면 안 바뀐다 — 판 도중에 규칙이 바뀌면 클·서가 갈린다.</para>
    /// </summary>
    public sealed class FlappyFlapComboRules
    {
        public static readonly FlappyFlapComboRules Default = new FlappyFlapComboRules(DefaultMilestones(), DefaultGains());

        /// <summary>보상 없는 맵 — 마커에서 두 배열을 다 비웠다. 세기와 화면은 그대로다.</summary>
        public static readonly FlappyFlapComboRules Off = new FlappyFlapComboRules(new int[0], new float[0]);

        private readonly int[] _milestones;
        private readonly float[] _gains;

        private FlappyFlapComboRules(int[] milestones, float[] gains)
        {
            _milestones = milestones;
            _gains = gains;
        }

        /// <summary>기본 고비(사용자 결정 10-27). 마커 필드 초기값으로도 쓰여 부를 때마다 새 배열이다.</summary>
        public static int[] DefaultMilestones() => new[] { 10, 25, 50, 100 };

        /// <summary>기본 보상(칸). <see cref="DefaultMilestones"/>와 짝(사용자 결정 10-28 — 게이지가 너무 잘 차 10은 말만, 25/50/100을 내렸다.
        /// 10-30 — 10도 조금은 준다, 0.1칸).</summary>
        public static float[] DefaultGains() => new[] { 0.1f, 0.25f, 0.5f, 0.5f };

        /// <summary>마지막 고비 뒤로 이만큼마다 마지막 보상을 다시 준다(사용자 결정 11-02 — 끊지 않는 쪽이 박고 다시 쌓는 쪽보다 늘 낫게).
        /// 맵 마커 칸이 아니다 — 이미 구운 맵을 다시 굽지 않아도 모든 맵에 같다.</summary>
        public const int RepeatEvery = 50;

        public int Count => _milestones.Length;

        /// <summary>
        /// 마커 값으로 규칙을 만든다. 짝이 안 맞음·오름차순 아님·1 미만 고비·0~2칸 밖 보상이면 false와 <see cref="Default"/>.
        /// 둘 다 비었으면 <see cref="Off"/>.
        /// </summary>
        public static bool TryCreate(int[] milestones, float[] gains, out FlappyFlapComboRules rules)
        {
            rules = Default;
            if (milestones == null || gains == null || milestones.Length != gains.Length)
            {
                return false;
            }
            for (int i = 0; i < milestones.Length; i++)
            {
                bool rising = i == 0 || milestones[i] > milestones[i - 1];
                //  NaN은 어떤 비교에도 거짓이라 "0 이상이고 2칸 이하인가"로 물어야 걸러진다.
                bool gainOk = gains[i] >= 0f && gains[i] <= FlappyDash.MaxCharge;
                if (milestones[i] <= 0 || rising == false || gainOk == false)
                {
                    return false;
                }
            }
            rules = milestones.Length == 0
                ? Off
                : new FlappyFlapComboRules((int[])milestones.Clone(), (float[])gains.Clone());
            return true;
        }

        /// <summary>연속 수 <paramref name="count"/>가 몇 번째 고비인가(0부터). 정확한 고비만 — 되풀이 자리(150…)는 −1이다(<see cref="RewardIndex"/>).</summary>
        public int MilestoneIndex(int count)
        {
            for (int i = 0; i < _milestones.Length; i++)
            {
                if (_milestones[i] == count)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 보상 자리 — 정확한 고비면 그 번호, 마지막 고비 뒤 <see cref="RepeatEvery"/>마다(기본 150·200·250…)는 마지막 고비 번호, 아니면 −1.
        /// 게이지(<see cref="Gain"/>)와 연출(한마디·고리·화음)이 이것을 본다. 코인·서버 통계는 이것을 안 본다(<c>FlappyComboStatKeys</c>).
        /// </summary>
        public int RewardIndex(int count)
        {
            int i = MilestoneIndex(count);
            if (i >= 0 || _milestones.Length == 0)
            {
                return i;
            }
            int last = _milestones[_milestones.Length - 1];
            return count > last && (count - last) % RepeatEvery == 0 ? _milestones.Length - 1 : -1;
        }

        /// <summary>연속 수가 <paramref name="count"/>가 되는 날갯짓에 주는 게이지(칸). 보상 자리(<see cref="RewardIndex"/>)가 아니면 0.
        /// 수 하나에 묶인 값이라 되감아 다시 지나도 그 수에 한 번이다.</summary>
        public float Gain(int count)
        {
            int i = RewardIndex(count);
            return i < 0 ? 0f : _gains[i];
        }

        /// <summary>로그 한 줄. 예: "10:+0.1 25:+0.25 50:+0.5 100:+0.5 그뒤50마다:+0.5". 보상이 없으면 "없음".</summary>
        public string Describe()
        {
            if (_milestones.Length == 0)
            {
                return "없음";
            }
            var text = new StringBuilder();
            for (int i = 0; i < _milestones.Length; i++)
            {
                if (i > 0)
                {
                    text.Append(' ');
                }
                text.Append(_milestones[i].ToString(CultureInfo.InvariantCulture))
                    .Append(":+")
                    .Append(_gains[i].ToString("0.###", CultureInfo.InvariantCulture));
            }
            text.Append(" 그뒤")
                .Append(RepeatEvery.ToString(CultureInfo.InvariantCulture))
                .Append("마다:+")
                .Append(_gains[_gains.Length - 1].ToString("0.###", CultureInfo.InvariantCulture));
            return text.ToString();
        }
    }
}
