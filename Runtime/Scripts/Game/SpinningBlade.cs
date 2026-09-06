using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 도는 날개. 매 틱 시뮬이 <see cref="Pose"/>로 이 틱의 자세를 잡아 주고, 그
    /// 자세의 콜라이더에 사람이 겹치면 밀려난다.
    ///
    /// <para><b>시뮬이 자세를 잡는 이유</b>: 되감기 재생 중에는 "지금"이 아니라 "그 틱"의 날개와
    /// 부딪혀야 라이브와 같은 답이 나온다. 뷰가 프레임마다 돌리면 재생 때 엉뚱한 각도를 보게 된다.</para>
    ///
    /// <para><see cref="LaserVolume"/>과 같은 이유로 <b>공용 패키지</b>에 있다 — 맵 씬은 클라에서
    /// 굽고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되어 씬 주입이 끊긴다.</para>
    ///
    /// <para>⚠️ <b>실험용(스파이크)</b> — 결과에 따라 통째로 버릴 수 있다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class SpinningBlade : MonoBehaviour
    {
        public float StartAngleDegrees = 0f;

        /// <summary>도 / 틱. 50틱/초이므로 3이면 초당 150도다.</summary>
        public float AngularSpeedDegreesPerTick = 3f;

        private BladeField field;
        private bool hasRegistered;

        [Inject]
        public void Construct(BladeField field)
        {
            this.field = field;
            hasRegistered = true;
            field.Add(this);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 날개가 두 배가 된다.
            if (hasRegistered && field != null)
            {
                field.Remove(this);
            }
        }

        /// <summary>
        /// 그 시각의 자세로 돌려놓는다. <b>시뮬은 정수 틱</b>으로 부른다(재생 중에도) — 콜라이더가
        /// 그 틱 자리에 있어야 되감기가 라이브와 같아진다. <b>뷰는 소수 틱</b>으로 부른다 —
        /// 시뮬은 초당 50번인데 화면은 60번 이상이라, 틱 자세만 쓰면 여섯 프레임 중 하나가
        /// 제자리라 계단처럼 떤다. 뷰가 덮어써도 다음 틱에 시뮬이 다시 정확한 자세로 되돌린다.
        /// </summary>
        public void Pose(double tick)
        {
            transform.localRotation = Quaternion.Euler(
                0f, BladeGeometry.AngleDegreesAt(StartAngleDegrees, AngularSpeedDegreesPerTick, tick), 0f);
        }

        // 씬 뷰에서 어디를 쓸고 가는지 보이게 — 배치가 곧 판정이다(LaserVolume과 같은 이유).
        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.6f, 0.1f, 0.5f);
            Gizmos.DrawWireSphere(transform.position, 0.4f);
            Gizmos.DrawLine(transform.position, transform.position + transform.up * 2f);
        }
    }
}
