using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 레이저 표시. 맵이 올라올 때 <see cref="LaserField"/>를 주입받아 스스로 등록한다.
    ///
    /// <para><see cref="WindVolume"/>과 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 굽고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가
    /// 씬 주입을 끊는다.</para>
    ///
    /// <para>각도를 <b>도(degree)</b>로 노출하는 것은 씬 인스펙터에서 사람이 읽고 고치기 때문이다.
    /// 라디안 변환은 여기서 한 번만 한다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class LaserVolume : MonoBehaviour
    {
        /// <summary>빔 길이. 이 오브젝트의 위치가 회전 중심(Pivot)이다.</summary>
        public float Length = 30f;

        /// <summary>빔 굵기(반지름).</summary>
        public float Radius = 0.6f;

        public float StartAngleDegrees = 0f;

        /// <summary>도 / 틱. 0이면 고정 빔이다.</summary>
        public float AngularSpeedDegreesPerTick = 0f;

        /// <summary>0보다 크면 전회전 대신 이 폭만큼 왕복한다.</summary>
        public float SweepHalfRangeDegrees = 0f;

        /// <summary>점멸 주기(틱). 0 이하면 늘 켜져 있다.</summary>
        public int Period = 0;
        public int OnTicks = 0;
        public int Phase = 0;

        public Laser ToLaser() => new Laser(
            transform.position.ToNumerics(),
            Length, Radius,
            StartAngleDegrees * Mathf.Deg2Rad,
            AngularSpeedDegreesPerTick * Mathf.Deg2Rad,
            SweepHalfRangeDegrees * Mathf.Deg2Rad,
            Period, OnTicks, Phase);

        private LaserField field;
        private Laser registered;
        private bool hasRegistered;

        [Inject]
        public void Construct(LaserField field)
        {
            this.field = field;
            registered = ToLaser();
            hasRegistered = true;
            field.Add(registered);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 레이저가 두 배가 된다.
            // 등록할 때의 값을 그대로 들고 있다가 뺀다(그 사이 필드가 바뀌어도 짝이 맞게).
            if (hasRegistered && field != null)
            {
                field.Remove(registered);
            }
        }
    }
}
