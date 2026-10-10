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
    public class IrisVolume : MonoBehaviour, IPosedObstacle, IMovingPlatform
    {
        /// <summary>활짝 열렸을 때 날개가 물러나는 거리.</summary>
        /// <summary>위에 선 사람을 날개와 같이 옮길지. 기본 꺼짐 — 조리개는 열리면 발밑이 빠져 떨어지는 관문이다(사용자 10-10).</summary>
        public bool Rideable = false;

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

        public bool TryGetPart(Collider hit, Vector3 feet, out Transform part)
        {
            //  날개 이음매에서도 클·서가 같은 날개를 고르게, 발 방향과 가장 가까운 날개(물러나는 방향 기준)를 고른다.
            part = null;
            if (Blades == null || Home == null)
            {
                return false;
            }
            Vector3 local = transform.InverseTransformPoint(feet);
            var dir = new Vector3(local.x, 0f, local.z);
            float best = float.NegativeInfinity;
            for (int i = 0; i < Blades.Length && i < Home.Length; i++)
            {
                float d = Vector3.Dot(dir, Outward(i));
                if (d > best)
                {
                    best = d;
                    part = Blades[i];
                }
            }
            return part != null;
        }

        public Matrix4x4 PartWorldAt(Transform part, double tick)
        {
            int i = System.Array.IndexOf(Blades, part);
            if (i < 0 || i >= Home.Length)
            {
                return part.localToWorldMatrix;
            }
            float open = OpennessAt(Period, OpenTicks, MoveTicks, Phase, tick);
            Vector3 local = Home[i] + Outward(i) * (Travel * open);
            return transform.localToWorldMatrix * Matrix4x4.TRS(local, part.localRotation, part.localScale);
        }

        private Vector3 Outward(int i)
        {
            var outward = new Vector3(Home[i].x, 0f, Home[i].z);
            return outward.sqrMagnitude > 1e-6f ? outward.normalized : Vector3.right;
        }

        bool IMovingPlatform.Rideable => Rideable;

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
