using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 수평으로 도는 대형 장애물(틈 난 원판·풍차 날개). 자식의 메시 콜라이더가 판정이고, 이 오브젝트를 Y축으로 돌린다.
    /// <para>틱만 넣으면 각도가 나온다 — 시뮬(정수 틱)·그림(소수 틱)·되감기가 같은 식을 쓴다(<see cref="DoorVolume"/>과 같은 성질).
    /// 위에 서 있는 사람은 같이 돈다(<see cref="SkydiveWorld"/>의 실어 나르기, 사용자 10-09) — 틈까지 걸어가 빠진다.</para>
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

        /// <summary>
        /// 판 위 한 점이 지금 움직이는 수평 속도(초당 m). 위에서 보면 DegreesPerTick가 양수일 때 시계 방향으로 돈다.
        /// 각속도가 일정해서 틱이 필요 없다 — 되감기 재생에서도 같은 답.
        /// </summary>
        public static Vector3 PointVelocity(Vector3 axisPoint, float degreesPerTick, Vector3 point, float deltaTime)
        {
            if (deltaTime <= 0f)
            {
                return Vector3.zero;
            }
            float omega = degreesPerTick * Mathf.Deg2Rad / deltaTime;
            Vector3 r = point - axisPoint;
            return new Vector3(omega * r.z, 0f, -omega * r.x);
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
