using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 여닫이 문 표시. 맵이 올라올 때 <see cref="DoorField"/>를 주입받아 스스로
    /// 등록한다.
    ///
    /// <para><see cref="LaserVolume"/>과 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서
    /// 굽고 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈
    /// 컴포넌트가 씬 주입을 끊는다.</para>
    ///
    /// <para>각도를 <b>도(degree)</b>로 노출하는 것은 씬 인스펙터에서 사람이 읽고 고치기 때문이다.
    /// 라디안 변환은 <see cref="ToDoor"/>에서 한 번만 한다.</para>
    ///
    /// <para><b>이 트랜스폼 자체는 회전이 없어야 한다.</b> <see cref="Pose"/>가 계산하는 패널
    /// 오프셋은 <see cref="Door.AxisAngle"/>(월드 축 기준)로 이미 방향이 잡혀 있는데, 그 위에
    /// 부모(이 오브젝트)가 또 회전을 갖고 있으면 자식의 로컬 좌표가 다시 꺾여 어긋난다.</para>
    /// </summary>
    [SceneInjectMonoBehaviour]
    public class DoorVolume : MonoBehaviour
    {
        /// <summary>덮는 폭의 절반(=구멍 반폭). 패널 하나는 이 값의 절반 길이다.</summary>
        public float HalfWidth = 5f;

        /// <summary>미끄러지는 방향과 직교하는 쪽 절반.</summary>
        public float HalfDepth = 5f;

        /// <summary>패널 두께(세로).</summary>
        public float Thickness = 0.5f;

        /// <summary>패널이 미끄러지는 방향(XZ 평면 각, 도). 이 오브젝트의 위치가 구멍 중심이다.</summary>
        public float AxisAngleDegrees = 0f;

        /// <summary>여닫힘 주기(틱). 0 이하면 항상 열려 있다.</summary>
        public int Period = 0;

        public int OpenTicks = 0;

        /// <summary>여닫는 데 걸리는 틱. 이 움직임 자체가 예고다.</summary>
        public int MoveTicks = 0;

        public int Phase = 0;

        /// <summary>구멍 왼쪽을 덮는 패널.</summary>
        public Transform PanelA;

        /// <summary>구멍 오른쪽을 덮는 패널.</summary>
        public Transform PanelB;

        public Door ToDoor() => new Door(
            transform.position.ToNumerics(),
            HalfWidth, HalfDepth, Thickness,
            AxisAngleDegrees * Mathf.Deg2Rad,
            Period, OpenTicks, MoveTicks, Phase);

        /// <summary>
        /// 이 틱의 열림 정도로 두 패널의 로컬 위치를 세팅한다. <see cref="DoorGeometry.PanelCenter"/>가
        /// 돌려주는 값은 <see cref="Door.Center"/>가 더해진 <b>월드</b> 좌표라서, 자식의 로컬 좌표엔
        /// 그 중심을 뺀 오프셋만 넣는다.
        /// </summary>
        public void Pose(double tick)
        {
            Door door = ToDoor();
            float openness = DoorGeometry.Openness(door, tick);

            SetPanel(PanelA, 0, door, openness);
            SetPanel(PanelB, 1, door, openness);
        }

        private static void SetPanel(Transform panel, int index, in Door door, float openness)
        {
            if (panel == null)
            {
                return; // 굽기 전 씬이나 테스트엔 패널이 없을 수 있다 — 조용히 건너뛴다.
            }

            System.Numerics.Vector3 center = DoorGeometry.PanelCenter(door, index, openness);
            panel.localPosition = (center - door.Center).ToUnity();
        }

        private DoorField field;

        [Inject]
        public void Construct(DoorField field)
        {
            this.field = field;
            field.Add(this);
        }

        private void OnDestroy()
        {
            // 라운드가 여러 판이면 맵을 다시 로드한다 — 안 빼면 문이 두 배가 된다.
            // (DoorVolume은 그 자체가 참조라서, Laser처럼 등록값을 따로 들고 있을 필요가 없다.)
            if (field != null)
            {
                field.Remove(this);
            }
        }

        // 배치가 곧 판정이다. 씬 뷰에서 문이 덮는 범위가 보여야 한다.
        // (LaserVolume.OnDrawGizmos와 같은 이유·같은 자리에 둔다.)
        private void OnDrawGizmos()
        {
            Door door = ToDoor();

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.9f);
            DrawPanel(door, 0, 0f); // 완전히 닫힌 자세
            DrawPanel(door, 1, 0f);

            Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.25f);
            DrawPanel(door, 0, 1f); // 완전히 열린 자세
            DrawPanel(door, 1, 1f);
        }

        private static void DrawPanel(in Door door, int index, float openness)
        {
            System.Numerics.Vector3 center = DoorGeometry.PanelCenter(door, index, openness);
            Vector3 size = new Vector3(door.HalfWidth, door.Thickness, door.HalfDepth * 2f);
            Gizmos.DrawWireCube(center.ToUnity(), size);
        }
    }
}
