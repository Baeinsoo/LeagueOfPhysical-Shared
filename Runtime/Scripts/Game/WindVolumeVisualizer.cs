using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 바람 볼륨에서 눈에 보이는 부분. 화살표를 바람 쪽으로 흘려 보내고, 범위 표시를 따로 들고 있어
    /// <see cref="ShowBounds"/>로 껐다 켤 수 있다 — 끄면 화살표와 흐름만 남는다.
    ///
    /// <para><see cref="WindVolume"/>과 같은 이유로 <b>공용 패키지</b>에 있다: 맵 씬은 클라에서 굽고
    /// 서버가 읽는데, 스크립트가 한쪽에만 있으면 반대쪽에서 missing script가 되고 그 빈 컴포넌트가
    /// 씬 주입을 끊는다.</para>
    /// </summary>
    // ExecuteAlways를 쓰지 않는다 — 편집 중에 화살표를 움직이면 씬에 그 자리가 그대로 저장돼서
    // 구운 배치가 망가진다.
    [RequireComponent(typeof(WindVolume))]
    public class WindVolumeVisualizer : MonoBehaviour
    {
        /// <summary>화살표들의 부모. 이 자식들이 바람 방향으로 흘러간다.</summary>
        public Transform ArrowsRoot;

        /// <summary>범위 표시(원기둥 면)의 부모. 통째로 껐다 켜려고 따로 둔다.</summary>
        public GameObject BoundsRoot;

        private WindVolume volume;
        private Transform[] arrows;
        private float[] baseAlong;      // 화살표마다 바람 축 위의 처음 위치
        private Vector3[] basePerp;     // 그 축과 직각인 나머지 성분
        private float[] extents;        // 화살표마다 되감기까지 갈 수 있는 거리
        private Vector3 axis;
        private float speed;
        private float offset;

        /// <summary>범위 표시를 보여 줄지.</summary>
        public bool ShowBounds
        {
            get => BoundsRoot != null && BoundsRoot.activeSelf;
            set
            {
                if (BoundsRoot != null)
                {
                    BoundsRoot.SetActive(value);
                }
            }
        }

        private void Awake()
        {
            // 서버는 -batchmode -nographics로 돌아 그릴 화면이 없다.
            if (Application.isBatchMode)
            {
                enabled = false;
                return;
            }

            volume = GetComponent<WindVolume>();
            speed = volume.Wind.magnitude;
            if (ArrowsRoot == null || speed <= 0.001f)
            {
                enabled = false;
                return;
            }

            axis = volume.Wind / speed;

            int count = ArrowsRoot.childCount;
            arrows = new Transform[count];
            baseAlong = new float[count];
            basePerp = new Vector3[count];
            extents = new float[count];

            for (int i = 0; i < count; i++)
            {
                Transform arrow = ArrowsRoot.GetChild(i);
                Vector3 p = arrow.localPosition;
                arrows[i] = arrow;
                baseAlong[i] = Vector3.Dot(p, axis);
                basePerp[i] = p - axis * baseAlong[i];
                extents[i] = FlowExtent(p, axis, volume.Radius, volume.Height);
            }
        }

        private void Update()
        {
            offset += speed * Time.deltaTime;

            for (int i = 0; i < arrows.Length; i++)
            {
                float along = WrapAlong(baseAlong[i], offset, extents[i]);
                arrows[i].localPosition = basePerp[i] + axis * along;
            }
        }

        /// <summary>
        /// 바람 축을 따라 <paramref name="offset"/>만큼 민 자리. 끝에 닿으면 반대쪽 끝에서 다시 들어온다.
        /// </summary>
        public static float WrapAlong(float along, float offset, float extent)
        {
            if (extent <= 0.0001f)
            {
                return along;
            }

            float half = extent * 0.5f;
            float shifted = along + offset + half;
            return shifted - Mathf.Floor(shifted / extent) * extent - half;
        }

        /// <summary>
        /// 그 자리의 화살표가 원기둥 안에서 바람 축을 따라 갈 수 있는 거리.
        ///
        /// <para>세로 바람이면 원기둥 높이 그대로다. 가로 바람이면 <b>그 화살표가 지나는 현(chord)의
        /// 길이</b>다 — 지름으로 감으면 가장자리 화살표가 되감길 때 원기둥 밖으로 튀어나온다.</para>
        /// </summary>
        public static float FlowExtent(Vector3 localPos, Vector3 windAxis, float radius, float height)
        {
            if (Mathf.Abs(windAxis.y) > 0.5f)
            {
                return height;
            }

            var flat = new Vector2(windAxis.x, windAxis.z);
            if (flat.sqrMagnitude < 0.0001f)
            {
                return height;
            }
            flat.Normalize();

            // 바람과 직각 방향으로 중심축에서 얼마나 떨어져 있나
            float perp = localPos.x * flat.y - localPos.z * flat.x;
            float halfChord = radius * radius - perp * perp;
            return halfChord <= 0f ? 0f : 2f * Mathf.Sqrt(halfChord);
        }
    }
}
