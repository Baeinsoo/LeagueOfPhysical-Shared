using System.Collections.Generic;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// 태풍의 눈 맵의 "몸개그 장애물" 넷 — 닿아도 절대 죽지 않고, 튕기거나 기절하거나 떠오른다(웹 시제품 11-24, 사용자 선택).
    /// <para>모두 <b>틱만 넣으면 자리가 나오는 식</b>이라 클·서·되감기가 같은 답을 낸다(레이저·문과 같은 성질). 콜라이더를 쓰지 않는다.</para>
    /// </summary>
    public enum StormHazardKind
    {
        None = 0,
        CloudCushion = 1,   // 벽에서 피어난 뭉게구름 — 위에서 밟으면 뽀잉
        StormCloud = 2,     // 떠다니는 먹구름 덩이 — 박으면 지지직 감전 기절
        GullFlock = 3,      // 돌풍에 휩쓸린 갈매기 떼 — 휩쓸려 옆으로 날아가고 머리에 한 마리가 앉는다
        Updraft = 4,        // 상승 기류 소용돌이 — 빙글빙글 떠올랐다가 휙 뱉어낸다
    }

    /// <summary>벽에 붙은 뭉게구름 쿠션. 윗면은 <see cref="Top"/> 높이의 원판(반지름 <see cref="Radius"/>).</summary>
    public readonly struct CloudCushion
    {
        public readonly Vector3 Top;
        public readonly float Radius;
        public CloudCushion(Vector3 top, float radius) { Top = top; Radius = radius; }
    }

    /// <summary>원통 가운데를 축으로 도는 먹구름 덩이. 자리는 <see cref="PositionAt"/>.</summary>
    public readonly struct StormCloud
    {
        public readonly float Y, Orbit, StartDegrees, DegreesPerTick, Bob, Radius;
        public readonly int BobPeriod;
        public StormCloud(float y, float orbit, float startDegrees, float degreesPerTick, float bob, int bobPeriod, float radius)
        { Y = y; Orbit = orbit; StartDegrees = startDegrees; DegreesPerTick = degreesPerTick; Bob = bob; BobPeriod = bobPeriod; Radius = radius; }

        public Vector3 PositionAt(double tick)
        {
            double a = (StartDegrees + DegreesPerTick * tick) * System.Math.PI / 180.0;
            double bob = BobPeriod > 0 ? System.Math.Sin(tick * 2.0 * System.Math.PI / BobPeriod + StartDegrees) * Bob : 0.0;
            return new Vector3((float)(System.Math.Cos(a) * Orbit), (float)(Y + bob), (float)(System.Math.Sin(a) * Orbit));
        }
    }

    /// <summary>원통을 가로지르는 갈매기 떼(V자 또는 넓은 판). 선두가 <see cref="LaneLength"/> 길이 길을 주기적으로 지나간다.</summary>
    public readonly struct GullFlock
    {
        public readonly float Y, HeadingDegrees, Offset, SpeedPerTick, LaneLength;
        public readonly int Count, Phase;
        /// <summary>0이면 V자, 아니면 한 줄에 이만큼씩 엇갈려 늘어선 넓은 판.</summary>
        public readonly int Columns;
        public GullFlock(float y, float headingDegrees, float offset, float speedPerTick, float laneLength, int count, int phase, int columns = 0)
        { Y = y; HeadingDegrees = headingDegrees; Offset = offset; SpeedPerTick = speedPerTick; LaneLength = laneLength; Count = count; Phase = phase; Columns = columns; }

        /// <summary>새 한 마리의 판정 반지름(몸 반지름은 따로 더한다).</summary>
        public const float BirdRadius = 1.8f;

        public Vector3 Heading
        {
            get { float a = HeadingDegrees * Mathf.Deg2Rad; return new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)); }
        }

        /// <summary>i번째 새의 자리. 0이 선두, 나머지는 V자로 뒤에 선다.</summary>
        public Vector3 BirdAt(int index, double tick)
        {
            Vector3 dir = Heading;
            Vector3 side = new Vector3(-dir.z, 0f, dir.x);
            double travel = ((tick + Phase) * SpeedPerTick) % LaneLength;
            float lead = (float)travel - LaneLength * 0.5f;
            if (Columns > 0)
            {
                int line = index / Columns, col = index % Columns;
                float across = (col - (Columns - 1) * 0.5f) * 3.4f + (line % 2 == 1 ? 1.7f : 0f);
                return dir * (lead - line * 3.4f) + side * (Offset + across) + Vector3.up * Y;
            }
            int row = (index + 1) / 2;
            float sign = index == 0 ? 0f : (index % 2 == 1 ? 1f : -1f);
            return dir * (lead - row * 2.6f) + side * (Offset + sign * row * 2.4f) + Vector3.up * Y;
        }
    }

    /// <summary>바닥 원(<see cref="Base"/>, 반지름 <see cref="Radius"/>)에서 <see cref="Height"/>만큼 솟은 상승 기류 기둥.</summary>
    public readonly struct Updraft
    {
        public readonly Vector3 Base;
        public readonly float Radius, Height;
        public Updraft(Vector3 @base, float radius, float height) { Base = @base; Radius = radius; Height = height; }
    }

    /// <summary>한 맵의 몸개그 장애물 배치. 비어 있으면 아무 일도 안 한다.</summary>
    public class StormHazardField
    {
        public readonly List<CloudCushion> Cushions = new List<CloudCushion>();
        public readonly List<StormCloud> Clouds = new List<StormCloud>();
        public readonly List<GullFlock> Flocks = new List<GullFlock>();
        public readonly List<Updraft> Updrafts = new List<Updraft>();
        public bool IsEmpty => Cushions.Count == 0 && Clouds.Count == 0 && Flocks.Count == 0 && Updrafts.Count == 0;
    }

    /// <summary>
    /// 맵별 배치표. 시제품이라 맵 씬 마커가 아니라 코드에 둔다 — 서버가 맵을 다시 받지 않아도 로컬에서 바로 돈다.
    /// 높이는 태풍의 눈(TbMap 12, 원통 반지름 60·출발 1600·출구 300)의 기존 장애물 사이 빈칸이다.
    /// </summary>
    public static class StormHazardLayout
    {
        public const int TyphoonEyeMapId = 12;
        private const float WallRadius = 60f;

        public static StormHazardField ForMap(int mapId)
        {
            var field = new StormHazardField();
            if (mapId != TyphoonEyeMapId)
            {
                return field;
            }

            //  밀도(사용자 11-24 "거의 충돌날 일이 없다"): 100m 떨어질 때마다 피할 것이 하나쯤 나오게.
            //  먹구름은 띠마다 소용돌이 팔 셋 — 팔 사이가 길이고, 팔이 통째로 돈다(읽고 피하거나 펴서 기다린다).
            float[] bands = { 1530f, 1390f, 1205f, 1085f, 840f, 690f, 520f, 430f };
            for (int b = 0; b < bands.Length; b++)
            {
                SpiralBand(field, bands[b], b * 37f, (b % 2 == 0 ? 1f : -1f) * (0.22f + 0.04f * (b % 3)), b);
            }

            //  뭉게구름 쿠션 — 원통 가운데쯤. 둘은 조리개 바로 아래 한가운데(열리면 다들 거기로 빠진다 → 뽀잉).
            void Cushion(float y, float r, float deg, float size) => field.Cushions.Add(new CloudCushion(OnCircle(r, deg, y), size));
            Cushion(1490f, 30f, 40f, 11f); Cushion(1345f, 35f, 200f, 11f); Cushion(1125f, 0f, 0f, 10f); Cushion(1060f, 25f, 120f, 11f);
            Cushion(885f, 30f, 300f, 11f); Cushion(800f, 38f, 60f, 11f); Cushion(625f, 28f, 230f, 11f); Cushion(568f, 0f, 0f, 10f);
            Cushion(465f, 33f, 150f, 11f);

            //  갈매기 떼 — 3줄 x 10마리 넓은 판으로 원통을 가로지른다(초속 20~25m).
            float lane = WallRadius * 2f + 40f;
            field.Flocks.Add(new GullFlock(1465f, 30f, 0f, 0.4f, lane, 30, 0, columns: 10));
            field.Flocks.Add(new GullFlock(1335f, 200f, 12f, 0.45f, lane, 30, 90, columns: 10));
            field.Flocks.Add(new GullFlock(1185f, 120f, -10f, 0.4f, lane, 30, 40, columns: 10));
            field.Flocks.Add(new GullFlock(975f, 300f, 5f, 0.5f, lane, 30, 150, columns: 10));
            field.Flocks.Add(new GullFlock(870f, 70f, -15f, 0.45f, lane, 30, 220, columns: 10));
            field.Flocks.Add(new GullFlock(500f, 250f, 8f, 0.5f, lane, 30, 310, columns: 10));

            //  상승 기류 소용돌이 — 바람 기둥(Up_960, 0° 반지름 35)과 다른 자리.
            void Lift(float r, float deg, float baseY, float height) => field.Updrafts.Add(new Updraft(OnCircle(r, deg, baseY), 11f, height));
            Lift(35f, 220f, 1470f, 35f); Lift(30f, 300f, 1110f, 40f); Lift(35f, 150f, 940f, 40f);
            Lift(25f, 240f, 780f, 40f); Lift(30f, 20f, 700f, 35f); Lift(30f, 330f, 470f, 30f);
            return field;
        }

        /// <summary>먹구름 소용돌이 띠 — 팔 셋(가운데 6m → 벽 55m, 110° 비틀림)에 구름을 11m 간격으로 늘어놓는다. 이웃 구름이 겹쳐 팔 사이로만 빠진다.</summary>
        private static void SpiralBand(StormHazardField field, float y, float baseDegrees, float degreesPerTick, int band)
        {
            const int arms = 3;
            const float r0 = 6f, r1 = 55f, twist = 110f, spacing = 11f, radius = 6f;
            for (int arm = 0; arm < arms; arm++)
            {
                float armStart = baseDegrees + arm * 360f / arms;
                Vector3 prev = default;
                float carried = 0f;
                for (int s = 0; s <= 200; s++)
                {
                    float t = s / 200f;
                    float deg = armStart + twist * t;
                    float r = Mathf.Lerp(r0, r1, t);
                    Vector3 p = OnCircle(r, deg, 0f);
                    if (s > 0) carried += Vector3.Distance(p, prev);
                    if (s == 0 || carried >= spacing)
                    {
                        float bob = 2f + StormDebrisGeometry.Jitter(band * 7 + arm, s) * 1.5f;
                        field.Clouds.Add(new StormCloud(y, r, deg, degreesPerTick, bob, 360 + band * 20, radius));
                        carried = 0f;
                    }
                    prev = p;
                }
            }
        }

        private static Vector3 OnCircle(float radius, float degrees, float y)
        {
            float a = degrees * Mathf.Deg2Rad;
            return new Vector3(Mathf.Cos(a) * radius, y, Mathf.Sin(a) * radius);
        }
    }

    /// <summary>
    /// 몸개그 장애물이 몸에 남긴 상태. 기간은 모두 "끝나는 틱"이다(ADR-0015).
    /// <see cref="LastHitTick"/>·<see cref="LastHitKind"/>는 화면이 "방금 무엇에 닿았나"를 읽는 자리다.
    /// </summary>
    public class StormState : GameFramework.World.Component
    {
        public long StunEndTick;
        public long LiftEndTick;
        public float EjectX, EjectZ;
        public long CushionReadyTick, CloudReadyTick, GullReadyTick, UpdraftReadyTick;
        public long GullOnHeadEndTick;
        public StormHazardKind LastHitKind;
        public long LastHitTick = -1;

        public bool ControlLocked(long tick) => tick < StunEndTick || tick < LiftEndTick;
    }

    /// <summary>되감기용 사진.</summary>
    public readonly struct StormSavedState
    {
        public readonly long StunEndTick, LiftEndTick, CushionReadyTick, CloudReadyTick, GullReadyTick, UpdraftReadyTick, GullOnHeadEndTick, LastHitTick;
        public readonly float EjectX, EjectZ;
        public readonly StormHazardKind LastHitKind;

        private StormSavedState(StormState s)
        {
            StunEndTick = s.StunEndTick; LiftEndTick = s.LiftEndTick; CushionReadyTick = s.CushionReadyTick; CloudReadyTick = s.CloudReadyTick;
            GullReadyTick = s.GullReadyTick; UpdraftReadyTick = s.UpdraftReadyTick; GullOnHeadEndTick = s.GullOnHeadEndTick; LastHitTick = s.LastHitTick;
            EjectX = s.EjectX; EjectZ = s.EjectZ; LastHitKind = s.LastHitKind;
        }

        public static StormSavedState? Capture(GameFramework.World.Entity entity)
        {
            var s = entity.Get<StormState>();
            return s == null ? (StormSavedState?)null : new StormSavedState(s);
        }

        public void RestoreTo(GameFramework.World.Entity entity)
        {
            var s = entity.Get<StormState>();
            if (s == null)
            {
                return;
            }
            s.StunEndTick = StunEndTick; s.LiftEndTick = LiftEndTick; s.CushionReadyTick = CushionReadyTick; s.CloudReadyTick = CloudReadyTick;
            s.GullReadyTick = GullReadyTick; s.UpdraftReadyTick = UpdraftReadyTick; s.GullOnHeadEndTick = GullOnHeadEndTick; s.LastHitTick = LastHitTick;
            s.EjectX = EjectX; s.EjectZ = EjectZ; s.LastHitKind = LastHitKind;
        }
    }

    /// <summary>
    /// 몸개그 장애물 판정과 효과. 무상태 — 상태는 <see cref="StormState"/>에 있다.
    /// <list type="bullet">
    /// <item><see cref="ApplyLift"/>: 이동 속도가 정해진 뒤, 옮기기 전에 — 떠오르는 동안 속도를 덮고, 끝나는 틱에 휙 뱉는다.</item>
    /// <item><see cref="Detect"/>: 옮긴 뒤에 — 이번 틱에 닿은 것을 찾아 속도·기절을 정한다(다음 틱 이동에 먹는다).</item>
    /// </list>
    /// </summary>
    public class StormHazardSystem
    {
        public const float StunSeconds = 1.4f;
        public const float GullStunSeconds = 0.7f;
        public const float GullOnHeadSeconds = 5f;
        public const float LiftSeconds = 2.2f;
        public const float LiftSpeed = 9f;
        public const float EjectSpeed = 20f;

        public static int Ticks(float seconds, float deltaTime) => deltaTime > 0f ? Mathf.Max(1, Mathf.RoundToInt(seconds / deltaTime)) : 1;

        public void ApplyLift(GameFramework.World.Entity diver, long tick, float deltaTime, StormHazardField field)
        {
            var state = diver.Get<StormState>();
            var velocity = diver.Get<GameFramework.World.Velocity>();
            if (state == null || velocity == null || field == null)
            {
                return;
            }
            if (tick < state.LiftEndTick)
            {
                //  기둥 가운데를 빙글빙글 돌며 떠오른다 — 가운데 쪽으로 살짝 끌고 옆으로 돌린다.
                Vector3 feet = GameFramework.World.EntityMotionExtensions.GetPosition(diver);
                Vector3 center = NearestUpdraftCenter(field, feet);
                Vector3 toCenter = new Vector3(center.x - feet.x, 0f, center.z - feet.z);
                Vector3 swirl = new Vector3(-toCenter.z, 0f, toCenter.x).normalized * 7f;
                Vector3 pull = toCenter * 1.5f;
                velocity.Linear = new System.Numerics.Vector3(swirl.x + pull.x, LiftSpeed, swirl.z + pull.z);
            }
            else if (tick == state.LiftEndTick)
            {
                velocity.Linear = new System.Numerics.Vector3(state.EjectX * EjectSpeed, 6f, state.EjectZ * EjectSpeed);
            }
        }

        public void Detect(GameFramework.World.Entity diver, long tick, float deltaTime, StormHazardField field, in SkydiveConfig config, Vector3 before)
        {
            var state = diver.Get<StormState>();
            var velocity = diver.Get<GameFramework.World.Velocity>();
            if (state == null || velocity == null || field == null || field.IsEmpty)
            {
                return;
            }
            var finish = diver.Get<FinishState>();
            if (finish != null && finish.FinishedTick != FinishState.NotFinished)
            {
                return;
            }
            Vector3 feet = GameFramework.World.EntityMotionExtensions.GetPosition(diver);
            Vector3 body = feet + Vector3.up * (config.BodyHeight * 0.5f);
            float bodyRadius = config.BodyRadius;

            //  뭉게구름 쿠션: 발이 윗면을 위에서 아래로 지나면 튕긴다. 세게 떨어질수록 더 높이.
            if (tick >= state.CushionReadyTick)
            {
                for (int i = 0; i < field.Cushions.Count; i++)
                {
                    CloudCushion c = field.Cushions[i];
                    if (before.y >= c.Top.y && feet.y < c.Top.y + 0.2f && Horizontal(feet, c.Top) < c.Radius + bodyRadius)
                    {
                        float downward = -velocity.Linear.Y;
                        float up = downward > 35f ? 32f : 22f;
                        Vector3 inward = new Vector3(-c.Top.x, 0f, -c.Top.z).normalized * 10f;
                        SetPosition(diver, new Vector3(feet.x, c.Top.y + 0.05f, feet.z));
                        velocity.Linear = new System.Numerics.Vector3(velocity.Linear.X + inward.x, up, velocity.Linear.Z + inward.z);
                        Unglide(diver);
                        state.CushionReadyTick = tick + Ticks(0.25f, deltaTime);
                        Hit(state, StormHazardKind.CloudCushion, tick);
                        return;
                    }
                }
            }

            //  먹구름 덩이: 몸이 구름 덩이에 닿으면 바깥으로 튕겨 나가며 기절.
            if (tick >= state.CloudReadyTick)
            {
                for (int i = 0; i < field.Clouds.Count; i++)
                {
                    StormCloud c = field.Clouds[i];
                    Vector3 p = c.PositionAt(tick);
                    Vector3 d = body - p;
                    if (d.sqrMagnitude < (c.Radius + bodyRadius) * (c.Radius + bodyRadius))
                    {
                        Vector3 n = d.sqrMagnitude > 1e-6f ? d.normalized : Vector3.up;
                        velocity.Linear = new System.Numerics.Vector3(n.x * 14f, Mathf.Max(4f, n.y * 10f), n.z * 14f);
                        state.StunEndTick = tick + Ticks(StunSeconds, deltaTime);
                        state.CloudReadyTick = tick + Ticks(1.2f, deltaTime);
                        Unglide(diver);
                        Hit(state, StormHazardKind.StormCloud, tick);
                        return;
                    }
                }
            }

            //  갈매기 떼: 한 마리라도 닿으면 떼가 가는 쪽으로 휩쓸리고 짧게 기절, 머리에 한 마리가 앉는다.
            if (tick >= state.GullReadyTick)
            {
                for (int f = 0; f < field.Flocks.Count; f++)
                {
                    GullFlock flock = field.Flocks[f];
                    if (Mathf.Abs(body.y - flock.Y) > 4f)
                    {
                        continue;
                    }
                    for (int b = 0; b < flock.Count; b++)
                    {
                        Vector3 bird = flock.BirdAt(b, tick);
                        if ((bird - body).sqrMagnitude < (GullFlock.BirdRadius + bodyRadius) * (GullFlock.BirdRadius + bodyRadius))
                        {
                            Vector3 dir = flock.Heading;
                            velocity.Linear = new System.Numerics.Vector3(dir.x * 24f, Mathf.Max(velocity.Linear.Y, -8f), dir.z * 24f);
                            state.StunEndTick = tick + Ticks(GullStunSeconds, deltaTime);
                            state.GullOnHeadEndTick = tick + Ticks(GullOnHeadSeconds, deltaTime);
                            state.GullReadyTick = tick + Ticks(1.5f, deltaTime);
                            Unglide(diver);
                            Hit(state, StormHazardKind.GullFlock, tick);
                            return;
                        }
                    }
                }
            }

            //  상승 기류: 기둥 안에 들어오면 정해진 시간 떠올랐다가, 들어온 쪽 반대로 휙 뱉는다.
            if (tick >= state.UpdraftReadyTick && tick >= state.LiftEndTick)
            {
                for (int i = 0; i < field.Updrafts.Count; i++)
                {
                    Updraft u = field.Updrafts[i];
                    if (body.y >= u.Base.y && body.y <= u.Base.y + u.Height && Horizontal(body, u.Base) < u.Radius)
                    {
                        Vector3 outward = new Vector3(body.x - u.Base.x, 0f, body.z - u.Base.z);
                        if (outward.sqrMagnitude < 1e-4f)
                        {
                            outward = new Vector3(-u.Base.x, 0f, -u.Base.z);
                        }
                        outward = outward.normalized;
                        state.LiftEndTick = tick + Ticks(LiftSeconds, deltaTime);
                        state.EjectX = outward.x;
                        state.EjectZ = outward.z;
                        state.UpdraftReadyTick = state.LiftEndTick + Ticks(3f, deltaTime);
                        Unglide(diver);
                        Hit(state, StormHazardKind.Updraft, tick);
                        return;
                    }
                }
            }
        }

        private static void Hit(StormState state, StormHazardKind kind, long tick)
        {
            state.LastHitKind = kind;
            state.LastHitTick = tick;
        }

        private static void Unglide(GameFramework.World.Entity diver)
        {
            var posture = diver.Get<Posture>();
            if (posture != null)
            {
                posture.Gliding = false;
            }
        }

        private static void SetPosition(GameFramework.World.Entity diver, Vector3 position)
        {
            var transform = diver.Get<GameFramework.World.Transform>();
            if (transform != null)
            {
                transform.Position = new System.Numerics.Vector3(position.x, position.y, position.z);
            }
        }

        private static float Horizontal(Vector3 a, Vector3 b)
        {
            float dx = a.x - b.x, dz = a.z - b.z;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }

        private static Vector3 NearestUpdraftCenter(StormHazardField field, Vector3 at)
        {
            Vector3 best = at;
            float bestDistance = float.MaxValue;
            for (int i = 0; i < field.Updrafts.Count; i++)
            {
                Vector3 c = field.Updrafts[i].Base;
                float d = Horizontal(c, at) + Mathf.Abs(at.y - c.y) * 0.1f;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c;
                }
            }
            return best;
        }
    }
}
