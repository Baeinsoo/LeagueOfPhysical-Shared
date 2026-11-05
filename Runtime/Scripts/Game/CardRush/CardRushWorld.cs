using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 카드 러시의 시뮬 코어(3차 장애물 레이스). 물리 없이 선수 상태(<see cref="CardRushRunnerState"/>) 하나로 판정하고,
    /// 자리는 그 상태(<see cref="CardRushRunnerState.PosMm"/>)에서 계산해 적는다.
    /// 되감기 상태는 선수 상태 값 하나뿐이다 — 자리·결승 틱은 불러온 뒤 상태에서 다시 적는다.
    /// </summary>
    public class CardRushWorld : GameFramework.World.WorldBase
    {
        private readonly IMatchSeed seed;

        private readonly GameFramework.Netcode.SequenceBuffer<Dictionary<string, CardRushRunnerState>> saved
            = new GameFramework.Netcode.SequenceBuffer<Dictionary<string, CardRushRunnerState>>(SaveCapacity);

        private readonly List<GameFramework.World.Entity> runners = new List<GameFramework.World.Entity>();

        //  코스를 만든 씨앗. 클라는 씨앗을 GameInfo로 늦게 받으므로, 받기 전에 만든 코스는 씨앗이 바뀌면 다시 만든다.
        private ulong courseSeed;
        private bool seedChangeReported;

        /// <summary>이 판의 코스(장애물·구역 줄·첫 장). 맵 규칙이 없으면 null — 그동안 월드는 아무것도 안 한다.</summary>
        public CardRushTrack Track { get; private set; }

        public CardRushTuning Tuning { get; }

        /// <summary>
        /// 옛(2차) 문 코스 — 판정에 안 쓴다. 옛 그림·봇이 컴파일되게만 40칸으로 고정해 만든다. 3차 Task 7에서 지움.
        /// </summary>
        public CardRushCourse Course { get; private set; }

        /// <summary>옛 칸 이동·멈춤 틱 — 판정에 안 쓴다. 3차 Task 7에서 지움.</summary>
        public CardRushTiming Timing { get; }

        public CardRushMapRulesField Rules { get; }

        private const int LegacyCourseSteps = 40;

        public CardRushWorld(GameFramework.World.EntityRegistry entityRegistry,
                             GameFramework.World.WorldEventBuffer eventBuffer,
                             CardRushMapRulesField rules,
                             IMatchSeed seed,
                             CardRushConfig config)
            : base(entityRegistry, eventBuffer)
        {
            Rules = rules;
            this.seed = seed;
            Tuning = config.Tuning;
            Timing = config.Timing;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            EnsureCourse(tick);
            if (Track == null)
            {
                return;
            }

            CollectRunners();
            foreach (var entity in runners)
            {
                var runner = entity.Get<CardRushRunner>();
                if (runner.LanePending && Rules.HasRules)
                {
                    //  규칙보다 먼저 생긴 몸 — 받은 자리(옆 거리)로 이제 레인을 정한다. 0으로 두면 첫 장이 서버와 영영 갈린다.
                    runner.Lane = CardRushLanes.LaneIndexOf(Rules, runner.LaneOrigin);
                    runner.LaneOrigin = Rules.LaneOrigin(runner.Lane);
                    runner.LanePending = false;
                }
                var input = entity.Get<InputBuffer>()?.Current;
                bool tapped = input != null && input.Grab;
                //  탭이 없어도 매 틱 부른다 — 달리기·굴러오는 것·구역이 탭과 상관없이 같은 틱에 일어난다(서버와 내 예측이 같게).
                CardRushRunnerSim.Step(ref runner.State, tick, tapped, GameplayStartTick, runner.Lane, Track, Tuning);
                //  출발 전에도 적는다 — 출발선에 세운다.
                WritePose(entity, runner, tick);
            }
        }

        private void EnsureCourse(long tick)
        {
            ulong value = seed.Value;
            if (Track != null && courseSeed == value)
            {
                return;
            }
            //  출발 뒤엔 갈아 끼우지 않는다 — 선수 상태(장애물·구역 번호)가 옛 코스 기준이라 판정이 엉킨다.
            //  정상 경로에선 서버는 씨앗을 안 바꾸고 클라는 출발 전에 받는다. 여기 오면 버그라 한 번 크게 알린다.
            if (Track != null && HasStarted(tick))
            {
                if (seedChangeReported == false)
                {
                    seedChangeReported = true;
                    UnityEngine.Debug.LogError($"[CardRushWorld] 출발 뒤 매치 씨앗이 바뀌었다({courseSeed} → {value}, 틱 {tick}) — 코스를 그대로 둔다");
                }
                return;
            }
            if (Rules.HasRules)
            {
                Track = new CardRushTrack(value, Tuning);
                Course = new CardRushCourse(value, LegacyCourseSteps);
                courseSeed = value;
                //  클·서가 같은 줄을 쓰는지(마스터데이터·Shared가 갈렸는지) 두 로그를 맞대어 본다.
                //  씨앗 0은 클라가 GameInfo를 받기 전에 임시로 만든 코스라 안 찍는다(맞대 볼 때 헷갈린다).
                if (value != 0) UnityEngine.Debug.Log($"[CardRushWorld] 코스 지문 {Track.Fingerprint:X16} 장애물 {Track.Obstacles.Count} 구역 {Track.Zones.Count} 길이 {Track.TrackMm} (씨앗 {value})");
            }
        }

        //  id 순으로 돈다 — 선수끼리는 서로 안 건드리지만, 순서가 결과를 바꾸지 않는다는 걸 기대지 않는다(KnockbackWorld와 같이).
        private void CollectRunners()
        {
            runners.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>() && entity.Get<CardRushRunner>() != null)
                {
                    runners.Add(entity);
                }
            }
            runners.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
        }

        private void WritePose(GameFramework.World.Entity entity, CardRushRunner runner, long tick)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            if (transform != null)
            {
                var p = CardRushLanes.PositionAt(runner.LaneOrigin, Rules.Forward, runner.State.PosMm);
                transform.Position = new System.Numerics.Vector3(p.x, p.y, p.z);
            }

            var finish = entity.Get<FinishState>();
            if (finish != null)
            {
                //  늘 상태에서 다시 적는다 — 되감아 완주 전으로 돌아가면 결승 틱도 지워져야 한다.
                finish.FinishedTick = runner.State.FinishTick != 0 ? runner.State.FinishTick : FinishState.NotFinished;
            }
        }

        /// <summary>못 들어온 선수의 순위용 진행 값 = 간 거리(mm). float로 정확하다(최대 190 000은 2²⁴보다 작다).</summary>
        public float RankingProgress(in CardRushRunnerState state) => state.PosMm;

        protected override void SaveGameState(long tick)
        {
            var frame = new Dictionary<string, CardRushRunnerState>();
            foreach (var entity in EntityRegistry.All)
            {
                var runner = entity.Get<CardRushRunner>();
                if (runner != null && entity.Has<GameFramework.World.Simulated>())
                {
                    frame[entity.Id] = runner.State;   // struct 값 복사
                }
            }
            saved.Record(tick, frame);
        }

        protected override bool LoadGameState(long tick)
        {
            if (saved.TryGet(tick, out var frame) == false)
            {
                return false;
            }

            foreach (var pair in frame)
            {
                var entity = EntityRegistry.Get(pair.Key);
                var runner = entity?.Get<CardRushRunner>();
                if (runner == null)
                {
                    continue;
                }
                runner.State = pair.Value;
                if (Track != null)
                {
                    WritePose(entity, runner, tick);
                }
            }
            return true;
        }

        /// <summary>그 틱에 저장된 한 선수의 상태. 클라 보정 처리기가 "그때 내 예측이 뭐였나"를 물을 때 쓴다.</summary>
        public bool TryGetSaved(long tick, string entityId, out CardRushRunnerState state)
        {
            state = default;
            return saved.TryGet(tick, out var frame) && frame.TryGetValue(entityId, out state);
        }
    }
}
