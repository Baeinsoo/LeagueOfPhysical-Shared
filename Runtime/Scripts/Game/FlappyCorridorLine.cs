using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 맵 씬에 놓는 광산 코스 중심선 표시. 점(x 오름차순, 통로 중심 y)은 빌더(<c>FlappyMineCourseBuilder</c>)가
    /// 굽기 끝에 통째로 채운다. 클라의 <c>FlappyCorridorCamera</c>가 이걸 찾아 카메라 피벗을 통로에 고정한다.
    ///
    /// <para><see cref="FlappyWindmill"/>·<see cref="FlappyBoostPad"/>와 같은 이유로 <b>공용 패키지</b>에
    /// 있다: 맵 씬은 클라에서 굽고 서버도 로드하는데, 스크립트가 클라에만 있으면 서버에서 missing
    /// script가 되어 그 빈 컴포넌트가 씬 전체를 깨뜨린다. 다만 이건 서버가 읽는 곳이 없어 주입은
    /// 없다 — 전통 코스(옛 굽기)엔 이 표시가 없어 <see cref="CenterAt"/>이 0을 주지만,
    /// FlappyCorridorCamera가 아예 찾지 못하면 그 0도 쓰이지 않는다.</para>
    /// </summary>
    public class FlappyCorridorLine : MonoBehaviour
    {
        [SerializeField] private Vector2[] points = System.Array.Empty<Vector2>();

        /// <summary>점(x 오름차순, 통로 중심 y). 빌더가 굽기 끝에 통째로 갈아 끼운다.</summary>
        public Vector2[] Points
        {
            get => points;
            set => points = value ?? System.Array.Empty<Vector2>();
        }

        /// <summary>통로 중심 높이: 점들을 선형 보간한다. 점이 없으면 0, 양 끝 밖은 끝 값(MineCourse.Lin과 같은 규칙).</summary>
        public float CenterAt(float x)
        {
            if (points == null || points.Length == 0)
            {
                return 0f;
            }
            if (x <= points[0].x)
            {
                return points[0].y;
            }
            for (int i = 1; i < points.Length; i++)
            {
                if (x <= points[i].x)
                {
                    Vector2 a = points[i - 1], b = points[i];
                    float t = (x - a.x) / Mathf.Max(1e-6f, b.x - a.x);
                    return Mathf.Lerp(a.y, b.y, t);
                }
            }
            return points[points.Length - 1].y;
        }
    }
}
