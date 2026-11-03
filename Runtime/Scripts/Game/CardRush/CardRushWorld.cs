using System.Collections.Generic;

namespace LOP
{
    /// <summary>
    /// 카드 러시의 시뮬 코어. 물리 없이 선수 상태(<see cref="CardRushRunnerState"/>) 하나로 판정하고, 자리는 그 상태에서 계산해 적는다.
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

        /// <summary>이 판의 코스. 맵 규칙이 없거나 잘못됐으면 null — 그동안 월드는 아무것도 안 한다.</summary>
        public CardRushCourse Course { get; private set; }

        public CardRushTiming Timing { get; }

        public CardRushMapRulesField Rules { get; }

        public CardRushWorld(GameFramework.World.EntityRegistry entityRegistry,
                             GameFramework.World.WorldEventBuffer eventBuffer,
                             CardRushMapRulesField rules,
                             IMatchSeed seed,
                             CardRushTiming timing)
            : base(entityRegistry, eventBuffer)
        {
            Rules = rules;
            this.seed = seed;
            Timing = timing;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            EnsureCourse(tick);
            if (Course == null)
            {
                return;
            }

            CollectRunners();
            foreach (var entity in runners)
            {
                var runner = entity.Get<CardRushRunner>();
                var input = entity.Get<InputBuffer>()?.Current;
                bool tapped = input != null && input.Grab;
                //  탭이 없어도 매 틱 부른다 — 6초 자동 던짐이 여기서 난다(서버와 내 예측이 같은 틱에).
                CardRushJudge.Advance(ref runner.State, tick, tapped, GameplayStartTick, Course, Timing);
                //  출발 전에도 적는다 — 출발선에 세운다.
                WritePose(entity, runner, tick);
            }
        }

        private void EnsureCourse(long tick)
        {
            ulong value = seed.Value;
            if (Course != null && courseSeed == value)
            {
                return;
            }
            //  출발 뒤엔 갈아 끼우지 않는다 — 선수 상태(칸·시도)가 옛 코스 기준이라 판정이 엉킨다.
            //  정상 경로에선 서버는 씨앗을 안 바꾸고 클라는 출발 전에 받는다. 여기 오면 버그라 한 번 크게 알린다.
            if (Course != null && HasStarted(tick))
            {
                if (seedChangeReported == false)
                {
                    seedChangeReported = true;
                    UnityEngine.Debug.LogError($"[CardRushWorld] 출발 뒤 매치 씨앗이 바뀌었다({courseSeed} → {value}, 틱 {tick}) — 코스를 그대로 둔다");
                }
                return;
            }
            if (Rules.HasRules && Rules.Validate(out _))
            {
                Course = new CardRushCourse(value, Rules.StepCount, Rules.Sections);
                courseSeed = value;
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
                float progress = CardRushLanes.Progress(runner.State, tick, Timing.MoveTicks);
                var p = CardRushLanes.PositionAt(runner.LaneOrigin, Rules.Forward, Rules.StepSpacing, progress);
                transform.Position = new System.Numerics.Vector3(p.x, p.y, p.z);
            }

            var finish = entity.Get<FinishState>();
            if (finish != null)
            {
                //  늘 상태에서 다시 적는다 — 되감아 완주 전으로 돌아가면 결승 틱도 지워져야 한다.
                finish.FinishedTick = runner.State.IsFinished(Course.StepCount) ? runner.State.MoveStartTick : FinishState.NotFinished;
            }
        }

        /// <summary>
        /// 못 들어온 선수의 순위용 진행 값. 틱을 <b>판 기준</b>(출발 틱 = 0)으로 바꿔 <see cref="CardRushProgress.ForRanking"/>에 넘긴다 —
        /// 서버 절대 틱을 그대로 넘기면 오래 켠 서버에서 같은 칸의 이웃 틱을 못 가른다(약 13만 틱 한계).
        /// </summary>
        public float RankingProgress(in CardRushRunnerState state, long horizonTick)
        {
            long start = GameplayStartTick == long.MaxValue ? 0 : GameplayStartTick;
            long moveStart = state.MoveStartTick == 0 ? 0 : System.Math.Max(0, state.MoveStartTick - start);
            long horizon = System.Math.Max(0, horizonTick - start);
            return CardRushProgress.ForRanking(state.Step, moveStart, horizon);
        }

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
                if (Course != null)
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
