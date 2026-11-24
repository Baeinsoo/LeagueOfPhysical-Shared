using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>태풍 잔해 조각의 종류 — 판정은 모두 상자, 그림만 다르다(통나무는 상자 안에 원기둥).</summary>
    public enum StormDebrisKind { Log, Trunk, Branch, Crown, RootBall, Plank }

    /// <summary>잔해 조각 하나. 자리는 부모(도는 판·조리개 날개) 기준. 로컬 +X가 (cos <see cref="Yaw"/>, 0, sin <see cref="Yaw"/>)를 본다.</summary>
    public readonly struct StormDebrisPiece
    {
        public readonly StormDebrisKind Kind;
        public readonly Vector3 Center;
        /// <summary>x = 길이(로컬 X), y = 두께, z = 폭.</summary>
        public readonly Vector3 Size;
        public readonly float Yaw;
        public StormDebrisPiece(StormDebrisKind kind, Vector3 center, Vector3 size, float yaw)
        { Kind = kind; Center = center; Size = size; Yaw = yaw; }

        /// <summary>맵 빌더와 같은 부호 — 로컬 +X가 (cos, 0, sin)을 보게.</summary>
        public Quaternion Rotation => Quaternion.Euler(0f, -Yaw, 0f);
    }

    /// <summary>
    /// 태풍의 눈 큰 장애물을 잔해 모양으로 바꾸는 순수 식(시제품). 클·서가 같은 식으로 같은 상자를 짓는다 — 판정이 갈리지 않게
    /// 콜라이더를 짚거나 난수에 기대지 않고, 표의 값과 조각 번호만 쓴다.
    /// </summary>
    public static class StormDebrisGeometry
    {
        public const float LogWidth = 2.4f;
        public const float PlankWidth = 2.6f;

        /// <summary>틈 난 원판 → 통나무 뗏목. 나란한 통나무 줄에서 틈 부채꼴(0°~<paramref name="gapDegrees"/>)과 그 둘레 여유를 뺀다. 끝은 줄마다 들쭉날쭉.</summary>
        public static List<StormDebrisPiece> Raft(float radius, float gapDegrees, int seed)
        {
            var pieces = new List<StormDebrisPiece>();
            float margin = LogWidth * 0.75f;
            int rows = Mathf.FloorToInt(radius * 2f / LogWidth);
            float z0 = -(rows - 1) * LogWidth * 0.5f;
            const float step = 0.5f;
            int steps = Mathf.CeilToInt(radius * 2f / step);
            for (int row = 0; row < rows; row++)
            {
                float z = z0 + row * LogWidth;
                //  줄 끝은 조금만 흩뜨린다 — 테두리 고리 둘(RimRing)이 그 아래를 메워 가장자리에 몸이 빠질 띠가 없다.
                float endA = radius - Jitter(seed, row * 2) * 1.0f;
                float endB = radius - Jitter(seed, row * 2 + 1) * 1.0f;
                float start = float.NaN, last = float.NaN;
                for (int s = 0; s <= steps + 1; s++)
                {
                    float x = -radius + s * step;
                    float end = x < 0f ? endA : endB;
                    bool keep = s <= steps && InsideEnd(x, z, step, end) && OutsideGap(x, z, gapDegrees, margin);
                    if (keep)
                    {
                        if (float.IsNaN(start)) start = x;
                        last = x;
                    }
                    else if (float.IsNaN(start) == false)
                    {
                        AddLog(pieces, start, last, z, step, seed, row);
                        start = float.NaN;
                    }
                }
            }
            //  테두리 통나무 고리 둘 — 나란한 줄은 원 둘레를 계단처럼 남겨 그 사이로 몸이 빠진다(리뷰 11-25).
            RimRing(pieces, StormDebrisKind.Log, radius - LogWidth * 0.5f - 0.05f, LogWidth, gapDegrees, margin, Vector3.zero);
            RimRing(pieces, StormDebrisKind.Log, radius - LogWidth * 1.5f - 0.05f, LogWidth, gapDegrees, margin, Vector3.zero);
            return pieces;
        }

        /// <summary>
        /// 원 둘레를 따라 접선 방향 조각을 늘어놓는다(가운데 반지름 <paramref name="center"/>, 폭 <paramref name="width"/>).
        /// 틈 부채꼴(0°~<paramref name="gapDegrees"/>)과 가장자리 여유는 비운다. 이웃 조각과 조금 겹쳐 빈틈이 없다.
        /// </summary>
        private static void RimRing(List<StormDebrisPiece> pieces, StormDebrisKind kind, float center, float width,
                                    float gapDegrees, float margin, Vector3 pivot)
        {
            const float stepDegrees = 4.4f;
            float outer = center + width * 0.5f;
            float marginDegrees = margin / center * Mathf.Rad2Deg;
            float first = gapDegrees + marginDegrees + stepDegrees * 0.5f;
            float last = 360f - marginDegrees - stepDegrees * 0.5f;
            int n = Mathf.CeilToInt((last - first) / stepDegrees) + 1;
            float spacing = (last - first) / (n - 1);
            float half = outer * Mathf.Sin(spacing * 0.5f * Mathf.Deg2Rad) + 0.15f;
            for (int i = 0; i < n; i++)
            {
                float deg = first + spacing * i;
                pieces.Add(new StormDebrisPiece(kind, Direction(deg) * center - pivot, new Vector3(half * 2f, LogWidth, width), deg + 90f));
            }
        }

        //  통나무 상자 귀퉁이가 끝(들쭉날쭉한 원) 안에 있어야 한다 — 벽을 파고들지 않게.
        private static bool InsideEnd(float x, float z, float step, float end)
        {
            float hz = Mathf.Abs(z) + LogWidth * 0.5f;
            float hx = Mathf.Abs(x) + step * 0.5f;
            return hx * hx + hz * hz <= end * end;
        }

        private static void AddLog(List<StormDebrisPiece> pieces, float a, float b, float z, float step, int seed, int row)
        {
            float len = b - a + step;
            if (len < 2.5f) return;
            //  높이를 흩뜨리면 이웃 줄 사이 턱(0.12m 넘음)이 벽이 된다 — 윗면을 맞춘다.
            pieces.Add(new StormDebrisPiece(StormDebrisKind.Log, new Vector3((a + b) * 0.5f, 0f, z), new Vector3(len, LogWidth, LogWidth), 0f));
        }

        /// <summary>점이 틈 부채꼴 밖에 있고, 틈 가장자리 두 선에서 <paramref name="margin"/> 이상 떨어져 있나.</summary>
        public static bool OutsideGap(float x, float z, float gapDegrees, float margin)
        {
            float deg = Mathf.Atan2(z, x) * Mathf.Rad2Deg;
            if (deg < 0f) deg += 360f;
            if (deg <= gapDegrees) return false;
            var p = new Vector2(x, z);
            return EdgeDistance(p, 0f) >= margin && EdgeDistance(p, gapDegrees) >= margin;
        }

        private static float EdgeDistance(Vector2 p, float degrees)
        {
            var d = new Vector2(Mathf.Cos(degrees * Mathf.Deg2Rad), Mathf.Sin(degrees * Mathf.Deg2Rad));
            float along = Vector2.Dot(p, d);
            return along <= 0f ? p.magnitude : Mathf.Abs(p.x * d.y - p.y * d.x);
        }

        /// <summary>풍차 날개 하나 → 뽑힌 거목(줄기 + 곁가지 세 쌍 + 끝 잎 덩이). 날개 폭 안에만 둔다. 가운데 뿌리 덩이는 <see cref="RootBall"/>.</summary>
        public static List<StormDebrisPiece> Tree(float bladeDegrees, float length, float width, int seed)
        {
            var pieces = new List<StormDebrisPiece>();
            const float trunk = 4f, inner = 4f;
            float crownWidth = Mathf.Min(width - 2f, 10f);
            //  끝 잎 덩이 바깥 귀퉁이까지 벽(59) 안에 남게 줄기 길이를 줄인다.
            float outer = Mathf.Min(length - 1f, Mathf.Sqrt(58.9f * 58.9f - crownWidth * crownWidth * 0.25f));
            float crownLen = 8f;
            float trunkEnd = outer - crownLen;
            Vector3 dir = Direction(bladeDegrees);
            pieces.Add(new StormDebrisPiece(StormDebrisKind.Trunk, dir * ((inner + trunkEnd) * 0.5f), new Vector3(trunkEnd - inner, trunk * 0.9f, trunk), bladeDegrees));
            float half = width * 0.5f;
            for (int k = 0; k < 3; k++)
            {
                float at = inner + (trunkEnd - inner) * (0.3f + 0.25f * k);
                for (int s = -1; s <= 1; s += 2)
                {
                    float spread = 45f + Jitter(seed, k * 2 + (s + 1)) * 15f;
                    float yaw = bladeDegrees + s * spread;
                    //  가지 끝 상자 귀퉁이까지 날개 폭 안에 남게 길이를 줄인다.
                    float len = Mathf.Min(9f, (half - 1.2f) / Mathf.Sin(spread * Mathf.Deg2Rad));
                    Vector3 c = dir * at + Direction(yaw) * (len * 0.5f) + Vector3.up * 0.4f;
                    pieces.Add(new StormDebrisPiece(StormDebrisKind.Branch, c, new Vector3(len, 1.4f, 1.4f), yaw));
                    //  가지 끝 쪽 잎 덩이 — 가운데가 가지 75% 자리라 옆으로 날개 폭을 넘지 않는다.
                    Vector3 leaf = dir * at + Direction(yaw) * (len * 0.75f) + Vector3.up * 0.9f;
                    pieces.Add(new StormDebrisPiece(StormDebrisKind.Crown, leaf, new Vector3(3f, 2.6f, 3f), yaw));
                }
            }
            pieces.Add(new StormDebrisPiece(StormDebrisKind.Crown, dir * (outer - crownLen * 0.5f), new Vector3(crownLen, 3.5f, crownWidth), bladeDegrees));
            return pieces;
        }

        /// <summary>풍차 가운데 — 엉킨 뿌리 흙덩이.</summary>
        public static StormDebrisPiece RootBall() => new StormDebrisPiece(StormDebrisKind.RootBall, Vector3.zero, new Vector3(9f, 5f, 9f), 0f);

        /// <summary>
        /// 조리개 날개 하나(부채꼴 <paramref name="a0"/>~<paramref name="a1"/>도, 반지름 <paramref name="radius"/>) → 접선 방향 판자 더미.
        /// 판자 길이는 부채꼴 바깥 경계 기준 + 조금 — 닫혔을 때 이웃 날개 판자와 겹친다. 바깥 귀퉁이는 반지름 안. 자리는 <paramref name="pivot"/> 기준.
        /// </summary>
        public static List<StormDebrisPiece> PlankPile(float a0, float a1, float radius, Vector3 pivot, int seed)
        {
            var pieces = new List<StormDebrisPiece>();
            float mid = (a0 + a1) * 0.5f;
            float halfAngle = (a1 - a0) * 0.5f * Mathf.Deg2Rad;
            Vector3 dir = Direction(mid);
            int n = 0;
            for (float r = PlankWidth * 0.5f; r + PlankWidth * 0.5f <= radius; r += PlankWidth, n++)
            {
                                float rOut = r + PlankWidth * 0.5f;
                //  바깥 경계 기준 + 여유 — 이웃 날개 판자와 안쪽에서 겹쳐 닫혔을 때 빈틈이 없다(물러나면 겹침이 풀린다).
                float half = rOut * Mathf.Tan(halfAngle) + 0.4f;
                float maxHalf = Mathf.Sqrt(Mathf.Max(0f, radius * radius - rOut * rOut));
                half = Mathf.Min(half, maxHalf);
                if (half < 0.8f) continue;
                //  줄마다 비틀면 줄 사이에 쐐기 틈이 생긴다 — 비틀지 않고 높이·두께만 흩뜨린다.
                float yaw = mid + 90f;
                //  두께는 달라도 윗면은 같은 높이(0.6) — 걸어 다닐 때 턱이 벽이 되지 않게.
                float thick = Jitter(seed, 500 + n) < 0.3f ? 1.6f : 1.2f;
                Vector3 c = dir * r + Vector3.up * (0.6f - thick * 0.5f) - pivot;
                pieces.Add(new StormDebrisPiece(StormDebrisKind.Plank, c, new Vector3(half * 2f, thick, PlankWidth), yaw));
            }
            //  바깥 테두리 — 곧은 판자는 원호를 못 따라가 벽 쪽에 띠가 빈다(리뷰 11-25). 날개 부채꼴만큼 둘레 조각을 깐다.
            {
                const float rimWidth = 3.0f;
                float center = radius - rimWidth * 0.5f - 0.05f;
                float outer = center + rimWidth * 0.5f;
                float span = a1 - a0;
                int count = Mathf.CeilToInt(span / 4.4f);
                float step = span / count;
                float half = outer * Mathf.Sin(step * 0.5f * Mathf.Deg2Rad) + 0.15f;
                for (int i = 0; i < count; i++)
                {
                    float deg = a0 + step * (i + 0.5f);
                    pieces.Add(new StormDebrisPiece(StormDebrisKind.Plank, Direction(deg) * center + Vector3.up * 0f - pivot,
                                                    new Vector3(half * 2f, 1.2f, rimWidth), deg + 90f));
                }
            }

            //  위에 엇갈려 얹힌 들보 셋 — 줄무늬 판이 아니라 쌓인 더미로 보이게. 날개 이등분선 가까이 두어 부채꼴 밖으로 안 나간다.
            for (int k = 0; k < 3; k++)
            {
                float rc = 14f + k * 13f + Jitter(seed, 700 + k) * 6f;
                float len = 9f + Jitter(seed, 710 + k) * 7f;
                float lateral = (Jitter(seed, 720 + k) - 0.5f) * Mathf.Max(0f, rc * Mathf.Tan(halfAngle) - 4f);
                float yaw = mid + (Jitter(seed, 730 + k) - 0.5f) * 30f;
                Vector3 side = new Vector3(-dir.z, 0f, dir.x);
                Vector3 c = dir * rc + side * lateral + Vector3.up * 1.4f - pivot;
                pieces.Add(new StormDebrisPiece(StormDebrisKind.Plank, c, new Vector3(len, 0.8f, 1.3f), yaw));
            }
            return pieces;
        }

        public static Vector3 Direction(float degrees)
            => new Vector3(Mathf.Cos(degrees * Mathf.Deg2Rad), 0f, Mathf.Sin(degrees * Mathf.Deg2Rad));

        /// <summary>0~1 결정론 값 — 정수 해시만 써서 플랫폼마다 같은 답.</summary>
        public static float Jitter(int seed, int i)
        {
            unchecked
            {
                uint h = (uint)(seed * 73856093) ^ (uint)(i * 19349663) ^ 0x9E3779B9u;
                h ^= h >> 16; h *= 0x7FEB352Du; h ^= h >> 15; h *= 0x846CA68Bu; h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }
    }

    /// <summary>
    /// 태풍의 눈 큰 장애물의 판정을 잔해로 갈아 끼운다(시제품). 원래 판(메시 콜라이더)은 끄고 같은 부모 아래 상자 콜라이더를 단다 —
    /// 움직임(돌기·날개 물러나기)·올라타기는 기존 마커(<see cref="SpinnerVolume"/>·<see cref="IrisVolume"/>)가 그대로 맡는다.
    /// 시뮬이 매 틱 첫머리에 부른다. 한 번 지은 것은 표식(<see cref="StormDebrisBuilt"/>)으로 건너뛴다.
    /// </summary>
    public static class StormDebrisRebuilder
    {
        /// <summary>원판 이름 → 틈 각도. 맵 굽기 표(클라 SkydiveCylinderLayout.Discs)와 같은 값이어야 한다.</summary>
        public static readonly Dictionary<string, float> DiscGaps = new Dictionary<string, float>
        {
            { "Disc_1440", 90f }, { "Disc_760", 70f }, { "Disc_740", 70f },
        };

        /// <summary>판 바깥 반지름(원통 59 — 벽 60).</summary>
        public const float PlateRadius = 59f;

        /// <summary>새로 지었으면 true — 부른 쪽이 물리 엔진에 트랜스폼을 반영해야 한다.</summary>
        public static bool EnsureBuilt(ObstacleField obstacles)
        {
            if (obstacles == null) return false;
            bool built = false;
            for (int i = 0; i < obstacles.All.Count; i++)
            {
                if (obstacles.All[i] is Component c && c != null && c.GetComponent<StormDebrisBuilt>() == null)
                {
                    c.gameObject.AddComponent<StormDebrisBuilt>();
                    built |= Build(c);
                }
            }
            return built;
        }

        private static bool Build(Component obstacle)
        {
            string name = obstacle.gameObject.name;
            int seed = Seed(name);
            if (obstacle is SpinnerVolume && DiscGaps.TryGetValue(name, out float gap))
            {
                var plate = obstacle.transform.Find("Plate");
                if (plate == null) return false;
                Disable(plate.gameObject);
                Spawn(obstacle.transform, StormDebrisGeometry.Raft(PlateRadius, gap, seed));
                return true;
            }
            if (obstacle is SpinnerVolume && name.StartsWith("Mill_"))
            {
                var blades = new List<Transform>();
                foreach (Transform child in obstacle.transform)
                {
                    if (child.name.StartsWith("Blade_")) blades.Add(child);
                }
                foreach (var blade in blades)
                {
                    var mesh = blade.GetComponent<MeshFilter>()?.sharedMesh;
                    if (mesh == null) continue;
                    Vector3 size = mesh.bounds.size;
                    //  날개는 로컬 +X가 (cos, 0, sin)을 보게 -deg로 돌려 놓았다.
                    float deg = -blade.localEulerAngles.y;
                    Disable(blade.gameObject);
                    Spawn(obstacle.transform, StormDebrisGeometry.Tree(deg, size.x + 2f, size.z, seed + blade.GetSiblingIndex()));
                }
                if (blades.Count > 0) Spawn(obstacle.transform, new List<StormDebrisPiece> { StormDebrisGeometry.RootBall() });
                return blades.Count > 0;
            }
            if (obstacle is IrisVolume iris && iris.Blades != null && iris.Home != null)
            {
                int n = iris.Blades.Length;
                float w = 360f / n;
                for (int b = 0; b < n && b < iris.Home.Length; b++)
                {
                    Disable(iris.Blades[b].gameObject);
                    Spawn(iris.Blades[b], StormDebrisGeometry.PlankPile(b * w, (b + 1) * w, PlateRadius, iris.Home[b], seed + b));
                }
                return n > 0;
            }
            return false;
        }

        private static void Disable(GameObject go)
        {
            foreach (var col in go.GetComponents<Collider>()) col.enabled = false;
            var r = go.GetComponent<Renderer>();
            if (r != null) r.enabled = false;
        }

        private static void Spawn(Transform parent, List<StormDebrisPiece> pieces)
        {
            int layer = LayerMask.NameToLayer("Default");
            for (int i = 0; i < pieces.Count; i++)
            {
                var p = pieces[i];
                var go = new GameObject("Debris_" + p.Kind);
                go.layer = layer;
                go.transform.SetParent(parent, false);
                go.transform.localPosition = p.Center;
                go.transform.localRotation = p.Rotation;
                go.AddComponent<BoxCollider>().size = p.Size;
                go.AddComponent<StormDebrisTag>().Kind = p.Kind;
            }
        }

        //  이름만으로 정하는 씨앗 — string.GetHashCode는 런타임마다 달라 클·서가 갈릴 수 있다.
        private static int Seed(string name)
        {
            unchecked
            {
                int h = 17;
                for (int i = 0; i < name.Length; i++) h = h * 31 + name[i];
                return h;
            }
        }
    }
}
