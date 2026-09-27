using System.Collections.Generic;
using GameFramework;
using UnityEngine;
using VContainer;

namespace LOP
{
    public enum FlappyAirflowKind { None, Up, Down }

    /// <summary>기류 하나가 덮는 사각형. <see cref="FlappyBoostRect"/>와 같은 자리의 값 객체다.</summary>
    public class FlappyAirflowRect
    {
        public readonly float X0, X1, Y0, Y1;
        public readonly FlappyAirflowKind Kind;

        public FlappyAirflowRect(float x0, float x1, float y0, float y1, FlappyAirflowKind kind)
        {
            X0 = System.Math.Min(x0, x1);
            X1 = System.Math.Max(x0, x1);
            Y0 = System.Math.Min(y0, y1);
            Y1 = System.Math.Max(y0, y1);
            Kind = kind;
        }

        public bool Contains(float x, float y) => x >= X0 && x <= X1 && y >= Y0 && y <= Y1;
    }

    /// <summary>
    /// 이 판의 기류 전부. 부스트 패드 필드와 같은 모양이고 같은 이유로 물리 질의를 안 쓴다 —
    /// 사각형 포함을 산술로만 본다(롤백 재생은 물리를 안 돌린다).
    /// 겹치면 X0가 작은 것, 같으면 Y0가 작은 것이 이긴다 — 등록 순서가 답을 바꾸지 못하게.
    /// </summary>
    public class FlappyAirflowField
    {
        private readonly List<FlappyAirflowRect> _rects = new List<FlappyAirflowRect>();

        public int Count => _rects.Count;

        public void Add(FlappyAirflowRect rect)
        {
            if (rect == null || _rects.Contains(rect))
            {
                return;
            }
            _rects.Add(rect);
        }

        public void Remove(FlappyAirflowRect rect) => _rects.Remove(rect);

        public FlappyAirflowKind Sample(float x, float y)
        {
            FlappyAirflowRect best = null;
            for (int i = 0; i < _rects.Count; i++)
            {
                var r = _rects[i];
                if (r.Contains(x, y) == false)
                {
                    continue;
                }
                if (best == null || r.X0 < best.X0 || (r.X0 == best.X0 && r.Y0 < best.Y0))
                {
                    best = r;
                }
            }
            return best == null ? FlappyAirflowKind.None : best.Kind;
        }
    }

    /// <summary>
    /// 맵 씬의 기류 표시. <see cref="FlappyBoostPad"/>와 같은 이유로 공용 패키지에 있다 — 맵 씬은
    /// 서버도 읽으므로 한쪽에만 있으면 missing script가 된다. 콜라이더를 쓰지 않는다.
    /// </summary>
    [ExecuteAlways]
    [SceneInjectMonoBehaviour]
    public class FlappyAirflow : MonoBehaviour
    {
        public float Width = 7f;
        public float Height = 20f;
        public FlappyAirflowKind Kind = FlappyAirflowKind.Up;

        private FlappyAirflowField field;
        private FlappyAirflowRect rect;

        [Inject]
        public void Construct(FlappyAirflowField field)
        {
            this.field = field;
            Vector3 c = transform.position;
            rect = new FlappyAirflowRect(c.x - Width * 0.5f, c.x + Width * 0.5f,
                                         c.y - Height * 0.5f, c.y + Height * 0.5f, Kind);
            field.Add(rect);
        }

        private void OnDestroy()
        {
            if (field != null && rect != null)
            {
                field.Remove(rect);
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Kind == FlappyAirflowKind.Up ? new Color(0.5f, 0.85f, 0.6f, 0.5f) : new Color(0.55f, 0.85f, 0.9f, 0.5f);
            Gizmos.DrawWireCube(transform.position, new Vector3(Width, Height, 1f));
        }
    }
}
