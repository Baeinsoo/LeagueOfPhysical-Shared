using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 동전 몸으로 쓸 원기둥 메시. <b>옆면이 둥글어야</b> 모로 세워도 굴러 넘어진다 —
    /// 평평한 옆면을 가진 박스는 그대로 안정하게 선다(실측: 기울기 7도까지 버팀).
    ///
    /// <para><b>면 개수가 곧 "얼마나 못 서는가"다.</b> 옆면 하나의 폭이 <c>π·지름/면수</c>이고,
    /// 그 폭이 넓을수록 그 면으로 안정하게 앉는다. 유니티 기본 원기둥(20면)은 이 동전에서 옆면이
    /// 4.7cm라 두께(4cm)보다 넓어 여전히 선다. 그래서 여기서 직접 깎는다.</para>
    /// </summary>
    public static class DiscMesh
    {
        //  실제 100원(지름 24mm)을 이 크기로 키우면 옆면 한 칸이 1.6mm쯤 되는 값. 이보다 성기면
        //  옆면이 넓어져 서기 시작하고, 촘촘히 할수록 볼록 껍데기 한도(255면)에 가까워진다.
        private const int SideCount = 64;

        //  같은 치수의 동전이 여럿이라 매번 만들지 않는다. 치수가 달라지는 일은 사실상 없지만
        //  키로 두어야 나중에 크기가 다른 동전이 생겨도 서로 덮어쓰지 않는다.
        //  모서리를 반지름의 몇 %만큼 깎을지. 겉보기는 그대로면서 옆면 접촉만 선으로 만든다.
        private const float RimInsetRatio = 0.05f;

        private static readonly Dictionary<(float, float), Mesh> cache = new();

        public static Mesh Get(float radius, float thickness)
        {
            var key = (radius, thickness);
            if (cache.TryGetValue(key, out Mesh cached) && cached != null)
            {
                return cached;
            }

            Mesh mesh = Build(radius, thickness);
            cache[key] = mesh;
            return mesh;
        }

        private static Mesh Build(float radius, float thickness)
        {
            float half = thickness * 0.5f;
            //  옆면 한가운데만 반지름을 다 주고 위아래 모서리는 살짝 깎는다 — 옆면이 평평하면
            //  거기 그대로 앉아 버린다. 가운데가 볼록하면 닿는 곳이 선 하나뿐이라 못 버틴다.
            float rimInset = radius * RimInsetRatio;
            var vertices = new Vector3[SideCount * 3];
            for (int i = 0; i < SideCount; i++)
            {
                float angle = i / (float)SideCount * Mathf.PI * 2f;
                float cos = Mathf.Cos(angle), sin = Mathf.Sin(angle);
                vertices[i] = new Vector3(cos * (radius - rimInset), half, sin * (radius - rimInset));
                vertices[i + SideCount] = new Vector3(cos * radius, 0f, sin * radius);
                vertices[i + SideCount * 2] = new Vector3(cos * (radius - rimInset), -half, sin * (radius - rimInset));
            }

            //  볼록 메시 콜라이더는 우리가 준 점들의 볼록 껍데기만 쓴다 — 삼각형이 정확할 필요는
            //  없지만, 메시가 비어 있으면 유니티가 거부하므로 옆면만 이어 준다.
            var triangles = new List<int>(SideCount * 12);
            for (int i = 0; i < SideCount; i++)
            {
                int next = (i + 1) % SideCount;
                int a = i, b = next, m = i + SideCount, mn = next + SideCount;
                int c = i + SideCount * 2, d = next + SideCount * 2;
                triangles.Add(a); triangles.Add(b); triangles.Add(m);
                triangles.Add(b); triangles.Add(mn); triangles.Add(m);
                triangles.Add(m); triangles.Add(mn); triangles.Add(c);
                triangles.Add(mn); triangles.Add(d); triangles.Add(c);
            }

            var mesh = new Mesh { name = $"Disc_{radius}_{thickness}" };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
