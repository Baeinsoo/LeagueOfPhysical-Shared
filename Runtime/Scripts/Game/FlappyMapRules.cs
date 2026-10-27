using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 추격자·수동 대시 스위치. 맵이 올라올 때 <see cref="FlappyMapRulesField"/>를
    /// 주입받아 스스로 값을 넣는다.
    ///
    /// <para><see cref="FlappyBoostPad"/>와 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서
    /// 만들고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈
    /// 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para>이 마커가 없는 맵(지금의 라이브 맵)에서는 필드가 기본값(둘 다 꺼짐)으로 남는다 —
    /// 추격자는 아무도 잡지 않고, 대시는 다이브로 못 차고 부스트 패드로만 받는다.</para>
    /// </summary>
    // 이 표시가 없으면 유니티는 플레이 중이 아닐 때 Awake/OnDestroy 같은 생명주기 함수를
    // 아예 불러 주지 않는다 — 그러면 씬 편집 중에 마커를 지워도 등록 해제가 안 걸린다.
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyMapRules : MonoBehaviour
    {
        /// <summary>추격자(뒤에서 오는 벽)가 잡는 판정을 켜나.</summary>
        public bool Chaser;

        /// <summary>다이브로 대시 게이지를 직접 채우나. 꺼져 있으면 부스트 패드로만 대시를 받는다.</summary>
        public bool ManualDash;

        /// <summary>대시 게이지를 빛 알갱이(<see cref="FlappyMoteCluster"/>)로 채우나. 켜면 다이브 충전은 쉰다.</summary>
        public bool MoteCharge;

        /// <summary>알갱이 하나가 채우는 게이지(칸).</summary>
        public float MoteValue = FlappyMapRulesField.DefaultMoteValue;

        /// <summary>몸 표면에서 이만큼 안의 알갱이를 먹는다(m).</summary>
        public float MoteReach = FlappyMapRulesField.DefaultMoteReach;

        /// <summary>날갯짓을 참고 빠르게 떨어지는 동안 대시 게이지가 차나(새 다이브 충전).</summary>
        public bool DiveCharge;

        /// <summary>다이브 충전 문턱 = 최고 낙하 × 이 비율.</summary>
        public float DiveMinFallRatio = FlappyMapRulesField.DefaultDiveMinFallRatio;

        /// <summary>최고 낙하로 떨어질 때 초당 차는 칸.</summary>
        public float DiveRate = FlappyMapRulesField.DefaultDiveRate;

        /// <summary>박아서(스턴) 잃는 게이지(칸). 이미 찬 칸은 지킨다 — 채우던 몫이 이보다 적으면 그만큼만.
        /// 알갱이·다이브로 채우는 맵에서만 쓴다.</summary>
        public float StunLoss = FlappyMapRulesField.DefaultStunLoss;

        /// <summary>연속 날갯짓 고비 — 연속 수가 이 값이 되는 날갯짓에 <see cref="ComboDashGains"/>의 같은 자리 칸을 준다(1 이상, 오름차순).
        /// 둘 다 비우면 이 맵은 보상이 없다. 이미 구운 맵엔 이 칸이 없어 초기값(기본)이 돈다 — 그래서 다시 굽지 않아도 된다.</summary>
        public int[] ComboMilestones = FlappyFlapComboRules.DefaultMilestones();

        /// <summary>고비마다 주는 대시 게이지(칸, 0~2). <see cref="ComboMilestones"/>와 같은 개수.</summary>
        public float[] ComboDashGains = FlappyFlapComboRules.DefaultGains();

        private FlappyMapRulesField field;

        [Inject]
        public void Construct(FlappyMapRulesField field)
        {
            this.field = field;
            if (FlappyFlapComboRules.TryCreate(ComboMilestones, ComboDashGains, out var flapCombo) == false)
            {
                Debug.LogWarning("[FlappyMapRules] 연속 날갯짓 고비 값이 잘못돼 기본값을 쓴다 — 고비와 보상은 같은 개수, 고비는 1 이상 오름차순, 보상은 0~2칸");
            }
            //  어느 값이 돌았는지 서버 파드 로그에서도 보이게 한 줄 — 이미 구운 맵은 이 칸이 없어 기본값이어야 한다.
            Debug.Log($"[FlappyMapRules] 연속 날갯짓 고비 {flapCombo.Describe()}");
            field.Set(Chaser, ManualDash, MoteCharge, MoteValue, MoteReach, DiveCharge, DiveMinFallRatio, DiveRate, StunLoss, flapCombo);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 거두면 지난 맵의 룰이 새 맵에 남는다.
            if (field != null)
            {
                field.Clear();
            }
        }
    }
}
