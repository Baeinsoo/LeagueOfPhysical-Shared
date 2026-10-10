using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 조리개 셔터 — 가운데를 덮은 날개들이 바깥으로 물러났다 돌아온다. 박자는 문과 같은 식(<see cref="DoorGeometry.Openness"/>).
    /// <para>날개마다 닫힌 자리(로컬)를 굽기 때 기억해 두고, 그 자리의 바깥 방향으로 <see cref="Travel"/>×열림만큼 민다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class IrisVolume : MonoBehaviour, IPosedObstacle
    {
        /// <summary>활짝 열렸을 때 날개가 물러나는 거리.</summary>
        public float Travel = 20f;

        public int Period, OpenTicks, MoveTicks, Phase;

        public Transform[] Blades;

        /// <summary>날개마다 닫힌 자리(로컬). 굽기 때 <see cref="Capture"/>로 적어 둔다.</summary>
        public Vector3[] Home;

        public static float OpennessAt(int period, int openTicks, int moveTicks, int phase, double tick)
            => DoorGeometry.Openness(new Door(System.Numerics.Vector3.Zero, 0f, 0f, 0f, 0f, period, openTicks, moveTicks, phase), tick);

        /// <summary>지금 날개 자리를 닫힌 자리로 기억한다(굽기·시험에서 부른다).</summary>
        public void Capture()
        {
            Home = new Vector3[Blades.Length];
            for (int i = 0; i < Blades.Length; i++)
            {
                Home[i] = Blades[i].localPosition;
            }
        }

        public void Pose(double tick)
        {
            if (Blades == null || Home == null)
            {
                return;
            }
            float open = OpennessAt(Period, OpenTicks, MoveTicks, Phase, tick);
            for (int i = 0; i < Blades.Length && i < Home.Length; i++)
            {
                var outward = new Vector3(Home[i].x, 0f, Home[i].z);
                outward = outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.right;
                Blades[i].localPosition = Home[i] + outward * (Travel * open);
            }
        }

        private ObstacleField field;

        [Inject]
        public void Construct(ObstacleField field)
        {
            this.field = field;
            field.Add(this);
        }

        private void OnDestroy() => field?.Remove(this);
    }
}
