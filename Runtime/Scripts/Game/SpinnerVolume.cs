using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 수평으로 도는 대형 장애물(틈 난 원판·풍차 날개). 자식의 메시 콜라이더가 판정이고, 이 오브젝트를 Y축으로 돌린다.
    /// <para>틱만 넣으면 각도가 나온다 — 시뮬(정수 틱)·그림(소수 틱)·되감기가 같은 식을 쓴다(<see cref="DoorVolume"/>과 같은 성질).
    /// 위에 서 있는 사람을 태워 돌리지는 않는다 — 원판이 발밑에서 돌아 틈이 오면 빠진다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class SpinnerVolume : MonoBehaviour, IPosedObstacle
    {
        public float StartDegrees;

        /// <summary>도 / 틱. 음수면 반대로 돈다.</summary>
        public float DegreesPerTick;

        /// <summary>0~360. 틱이 커져도 오차가 쌓이지 않게 double로 한 번에 계산한다.</summary>
        public static float AngleAt(float start, float degreesPerTick, double tick)
        {
            double a = start + degreesPerTick * tick;
            a -= System.Math.Floor(a / 360.0) * 360.0;
            return (float)a;
        }

        public void Pose(double tick)
        {
            transform.localRotation = Quaternion.Euler(0f, AngleAt(StartDegrees, DegreesPerTick, tick), 0f);
        }

        private ObstacleField field;

        [Inject]
        public void Construct(ObstacleField field)
        {
            this.field = field;
            field.Add(this);
        }

        // 라운드마다 맵을 다시 로드한다 — 안 빼면 다음 판에 두 배가 된다.
        private void OnDestroy() => field?.Remove(this);
    }
}
