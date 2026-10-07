using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 별 조각. 판정 데이터는 <see cref="CatchTargetField"/>에, 자세(그림)는 <see cref="ObstacleField"/>에 넣는다 —
    /// 시뮬이 매 틱 세우고(되감기 포함) 클라 뷰가 프레임마다 소수 틱으로 그린다. 충돌체는 없다(닿으면 잡는 것이지 막히는 것이 아니다).
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class StarVolume : MonoBehaviour, IPosedObstacle
    {
        public Vector3 Center;
        public float OrbitRadius = 40f;
        public float DegreesPerTick = 0.4f;
        public float StartDegrees;
        public float BobAmplitude = 20f;
        public int BobPeriod = 400;
        public float CatchRadius = 6f;

        public CatchTarget ToTarget() => new CatchTarget(Center, OrbitRadius, DegreesPerTick, StartDegrees, BobAmplitude, BobPeriod, CatchRadius);

        public void Pose(double tick)
        {
            transform.position = CatchTargetGeometry.PositionAt(ToTarget(), tick);
        }

        private CatchTargetField catchField;
        private ObstacleField obstacles;
        private CatchTarget registered;
        private bool hasRegistered;

        [Inject]
        public void Construct(CatchTargetField catchField, ObstacleField obstacles)
        {
            this.catchField = catchField;
            this.obstacles = obstacles;
            registered = ToTarget();
            hasRegistered = true;
            catchField.Add(registered);
            obstacles.Add(this);
        }

        //  라운드마다 맵을 다시 로드한다 — 안 빼면 다음 판에 별이 둘이 된다.
        private void OnDestroy()
        {
            if (hasRegistered)
            {
                catchField?.Remove(registered);
                obstacles?.Remove(this);
            }
        }
    }
}
