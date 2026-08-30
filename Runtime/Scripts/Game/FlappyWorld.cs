using System.Collections.Generic;
using GameFramework;
using GameFramework.Physics;
using UnityEngine;

namespace LOP
{
    /// <summary>
    /// Flappy Race의 시뮬 코어. 클·서가 같은 구체 클래스를 돌려 결과가 갈리지 않게 한다.
    /// 한 틱: ⓪ 출발틱 전이면 아무것도 굴리지 않고 속도만 0으로 둔다.
    /// ① 스턴 시간 감소 → ② 속도(중력·플랩·고정 전진, 스턴 중이면 스킵) →
    /// ③ 맵에서 밀어내기(스폰 겹침 등) → ④ 맵은 막으며 이동(MoveBlockedByMap)
    /// + 부딪히면 스턴 진입(무적 중에도 막힘, 재진입만 안 함).
    ///
    /// <para><b>새끼리는 부딪히지 않는다 — 서로 통과한다.</b> 몸싸움을 두면 남의 새도 클라가
    /// 굴려야 하는데(안 굴리면 부딪힐 상대가 없다), 남의 입력은 오지 않으므로 "안 눌렀다"로
    /// 굴러 상대가 날갯짓할 때마다 크게 어긋났다. 몸싸움을 버리는 대신 남의 새를 <b>지연 스냅샷
    /// 보간</b>으로 그린다 — 클라가 굴리지 않으니 예측 오차가 아예 없다. 부수 효과로 내 새의
    /// 보정도 줄었다(보정의 80%가 옆에 새가 있을 때 났다 — 원인이 몸싸움이었다).
    /// 몸싸움 코드(<c>BodyCollisionSystem</c>·<c>BodyOverlap</c>·<c>VerticalBounce</c>)는
    /// 지우지 않았다 — 다른 게임이 쓴다.</para>
    /// </summary>
    public class FlappyWorld : GameFramework.World.WorldBase
    {
        private readonly FlappyMoveSystem _moveSystem;
        private readonly FlappyStunSystem _stunSystem;
        private readonly ICollisionQuery _collisionQuery;
        private readonly GameFramework.World.IMotionBridge _motionBridge;
        private readonly int _layerMask;

        // 매 틱 도는 코드라 목록을 새로 만들지 않고 비워서 다시 쓴다. 굴릴 대상(Simulated)만 담는다
        // — 새끼리 안 부딪히므로 "부딪힐 상대" 목록이 따로 필요 없다.
        private readonly List<GameFramework.World.Entity> _birds = new List<GameFramework.World.Entity>();

        // KinematicMover.Move에게 넘길 실제 쿼리를 감싸, sweep 도중 한 번이라도 히트가 있었는지만
        // 기록한다. 매 틱 재사용해 새 인스턴스를 만들지 않는다.
        // ⚠️ 센다: 이동 전 지면 탐침 + 수평 + 수직(+ stepOffset>0이면 턱 오르기 최대 3개 더).
        // 지면 탐침도 여기 잡힌다 — 커널이 "지면을 찾으려고" 쏜 캐스트가 실제 sweep이라 Flappy에선
        // "맵에 부딪혔다"로 읽힌다(아래 MoveBlockedByMap의 SawHit→Enter). 열린 항목 O4, 라이브에서
        // 먼저 본다(design §9).
        private readonly HitTrackingQuery _hitTracker = new HitTrackingQuery();

        // 스턴 타이머의 틱별 사진. 위치·속도는 WorldBase가 담는다.
        private readonly GameFramework.Netcode.SequenceBuffer<Dictionary<string, FlappySavedState>> _gameFrames
            = new GameFramework.Netcode.SequenceBuffer<Dictionary<string, FlappySavedState>>(SaveCapacity);

        public FlappyWorld(
            GameFramework.World.EntityRegistry entityRegistry,
            GameFramework.World.WorldEventBuffer eventBuffer,
            FlappyMoveSystem moveSystem,
            FlappyStunSystem stunSystem,
            ICollisionQuery collisionQuery,
            GameFramework.World.IMotionBridge motionBridge,
            int layerMask)
            : base(entityRegistry, eventBuffer)
        {
            _moveSystem = moveSystem;
            _stunSystem = stunSystem;
            _collisionQuery = collisionQuery;
            _motionBridge = motionBridge;
            _layerMask = layerMask;
        }

        protected override void Mutation(long tick, float deltaTime)
        {
            CollectBirds();

            if (HasStarted(tick) == false)
            {
                // 출발선에서 대기 중. 속도를 명시적으로 0으로 두는 이유는 스냅샷과 물리 팔로워가
                // 이 값을 읽기 때문이다 — 스폰 직후엔 어차피 0이지만 적어 두는 쪽이 안전하다.
                for (int i = 0; i < _birds.Count; i++)
                {
                    _birds[i].Get<GameFramework.World.Velocity>().Linear = System.Numerics.Vector3.Zero;
                }
                return;
            }

            // 시간 감소가 먼저다. 이번 틱에 풀릴 새는 이번 틱부터 움직인다.
            for (int i = 0; i < _birds.Count; i++)
            {
                _stunSystem.Tick(_birds[i], deltaTime);
            }

            for (int i = 0; i < _birds.Count; i++)
            {
                if (_stunSystem.IsStunned(_birds[i]))
                {
                    // 스턴 중인 새는 전진도 하지 않는다 — 시간 손실이 이 게임의 페널티다.
                    _birds[i].Get<GameFramework.World.Velocity>().Linear = System.Numerics.Vector3.Zero;
                    continue;
                }
                _moveSystem.Tick(_birds[i], deltaTime);
            }

            // 벽 안이면 밖으로 밀어낸다 — 스폰 겹침 등. 겹침이
            // 없으면 0을 돌려주므로 매 틱 불러도 공짜고, 그래서 "단단한 몸"이 상시 성립한다.
            // 겹침 판정은 World.Transform(진실원본) 자리로 한다. 엔진 트랜스폼을 보면 물리 스텝
            // 뒤에야 갱신되는 한 틱 전 자리를 보고, 롤백 재생 중엔 물리를 안 돌려 아예 얼어 있다
            // — 그러면 같은 코드가 라이브와 재생에서 다른 답을 낸다.
            for (int i = 0; i < _birds.Count; i++)
            {
                ClearVelocityIntoSurface(_birds[i], _motionBridge.Depenetrate(_birds[i]));
            }

            for (int i = 0; i < _birds.Count; i++)
            {
                MoveBlockedByMap(_birds[i], deltaTime);
            }
        }

        protected override void SaveGameState(long tick)
        {
            var frame = new Dictionary<string, FlappySavedState>();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Has<GameFramework.World.Simulated>())
                {
                    frame[entity.Id] = FlappySavedState.Capture(entity);
                }
            }
            _gameFrames.Record(tick, frame);
        }

        protected override bool LoadGameState(long tick)
        {
            if (!_gameFrames.TryGet(tick, out var frame))
            {
                return false;
            }
            foreach (var pair in frame)
            {
                var entity = EntityRegistry.Get(pair.Key);
                if (entity != null)
                {
                    pair.Value.RestoreTo(entity);
                }
            }
            return true;
        }

        /// <summary>그 틱에 저장해 둔 스턴 상태. 서버 스냅과 비교해 되돌릴지 정할 때 쓴다.</summary>
        public bool TryGetSavedStun(long tick, string entityId, out FlappySavedState state)
        {
            if (_gameFrames.TryGet(tick, out var frame) && frame.TryGetValue(entityId, out state))
            {
                return true;
            }
            state = default;
            return false;
        }

        // 굴릴 대상만 모은다(Simulated). 서버는 모든 새에, 클라는 내 새에만 그 표식이 붙는다
        // — 남의 새는 클라가 굴리지 않고 지연 스냅샷 보간으로 그린다.
        // "새인가"는 EntityKind로 가린다 — FlappyStun은 스턴 *타이머*라 정체성 표식이
        // 아니다(Task10 리뷰에서 교정: 우연히 유효했을 뿐 의미상 틀린 기준이었다).
        // CapsuleShape는 아이템도 갖고 있어(ItemCreator) 기준이 될 수 없다.
        // id 순으로 세운다. 레지스트리 순회 순서는 정해져 있지 않은데, 처리 순서가 클·서에서
        // 같아야 두 쪽이 같은 결과에 이른다.
        private void CollectBirds()
        {
            _birds.Clear();
            foreach (var entity in EntityRegistry.All)
            {
                if (entity.Get<EntityKind>()?.Kind != EntityType.Character)
                {
                    continue;   // 새가 아니다
                }
                if (entity.Has<GameFramework.World.Simulated>() == false)
                {
                    continue;   // 클라에서 남의 새 — 보간으로 그린다
                }
                _birds.Add(entity);
            }
            _birds.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
        }

        // 맵은 막는다 — KinematicMover가 벽까지만 이동시키고 미끄러뜨린다(collide-and-slide).
        // (이름 참고: 예전엔 "MoveThroughMap"이었다 — 그 이름이 뜻하던 통과가 이 슬라이스의
        // 취지였다. 지금은 반대로 막으므로 이름도 그에 맞춘다.)
        // "부딪혔는가"는 스턴 진입에 따로 필요하다 — KinematicMoveResult엔 그 정보가 없어서
        // (grounded만 있음) _hitTracker로 실제 쿼리를 감싸 sweep 도중 히트가 있었는지 기록한다.
        // ⚠️ 이 히트에는 이동 전 지면 탐침(맨 위 _hitTracker 선언부 참고)도 섞여 있다 — 그래서
        // 지면 5cm 이내에서 세로 속도가 (-1.4, 0]이면(플랩 포물선 꼭짓점 부근) 맵에 안 닿았어도
        // 스턴이 걸릴 수 있다. 열린 항목 O4, 라이브에서 먼저 본다.
        //  파묻힌 데서 밀려 나왔다면, 그 벽 쪽으로 파고들던 속도는 지운다.
        //  안 지우면: 캡슐이 콜라이더 *안*에서 시작한 sweep은 히트를 못 내(시작 겹침은 무시된다)
        //  "닿았으니 속도 0" 경로가 안 돌고, 막혀 있는데 중력만 계속 쌓인다. 그 상태로 밀어내기와
        //  줄다리기가 붙어 새가 제자리에서 갈리고, 그 미세한 차이가 클·서에서 갈려 보정이 계속 난다.
        //  (역사: 이 실측(-14까지 쌓이는 동안 실제로는 0.11밖에 안 내려감)은 수평 sweep이 몸을
        //  들어올려 검사하던 시절 — 즉 오르막에서 파묻힘이 상시 일어나던 시절 — 값이다. 이 브랜치가
        //  그 파묻힘을 없앴으므로 지금은 상시 근거가 아니라 "왜 이 코드가 생겼는지"의 역사다.
        //  지금 이 함수가 실제로 발동하는 경로는 둘뿐이다: ① 스폰 겹침 ② MoveBlockedByMap의
        //  z=0 클램프가 몸을 벽 안으로 되밀 때. (셋째였던 "몸싸움이 새를 지형 안으로 처박을 때"는
        //  새끼리 안 부딪히게 되면서 사라졌다.) 발동할 때 세로 속도를 만드는 게 커널 규칙
        //  D5("경사는 세로 속도를 안 늘린다")와 어긋난다는 점은 열린 항목 O1로 남아 있다.)
        //  민 방향의 반대 성분만 덜어낸다 — 벽을 따라 흐르던 속도는 살려 둬야 미끄러져 빠져나온다.
        private static void ClearVelocityIntoSurface(GameFramework.World.Entity bird, System.Numerics.Vector3 push)
        {
            if (push.LengthSquared() <= 0f)
            {
                return;
            }
            var velocity = bird.Get<GameFramework.World.Velocity>();
            if (velocity == null)
            {
                return;
            }
            System.Numerics.Vector3 outward = System.Numerics.Vector3.Normalize(push);
            float into = System.Numerics.Vector3.Dot(velocity.Linear, outward);
            if (into < 0f)
            {
                velocity.Linear -= outward * into;
            }
        }

        private void MoveBlockedByMap(GameFramework.World.Entity entity, float deltaTime)
        {
            var transform = entity.Get<GameFramework.World.Transform>();
            var velocity = entity.Get<GameFramework.World.Velocity>();
            var body = entity.Get<GameFramework.World.CapsuleShape>();
            if (transform == null || velocity == null || body == null)
            {
                return;
            }

            _hitTracker.Reset(_collisionQuery);
            //  새는 날아다니므로 턱을 오를 이유가 없다. 0을 준다.
            var result = KinematicMover.Move(new KinematicMoveInput(
                transform.Position.ToUnity(), velocity.Linear.ToUnity(),
                body.Radius, body.Height, deltaTime, _layerMask, stepOffset: 0f), _hitTracker);

            if (_hitTracker.SawHit)
            {
                // 무적 중이면 Enter가 알아서 무시한다 — 여기선 "닿았다"만 알리면 된다.
                _stunSystem.Enter(entity);
            }

            // z는 0에 붙잡는다. 미끄러짐이 남은 이동을 충돌면에 투영하는데, 그 면의 법선에 z가
            // 섞여 있으면 새가 조금씩 옆으로 새어 x-y 레인을 영영 벗어난다.
            // 이 게임은 레이스 코스가 x-y 평면 한 장이라는 전제 위에 있다 — 스폰 지점도 z=0이다.
            // 언젠가 z가 다른 맵을 만들면 이 줄이 새를 원점 평면으로 끌어당기므로 같이 손봐야 한다.
            var moved = result.position.ToNumerics();
            transform.Position = new System.Numerics.Vector3(moved.X, moved.Y, 0f);
            // 벽/바닥에 막힌 축의 속도도 같이 지워야 한다 — 안 지우면 다음 틱 중력 누적이
            // "막혀서 멈춘 적 없다는 듯" 옛 속도 위에 계속 쌓인다(KinematicMoveSystem과 같은 관례).
            // 속도 z도 여기서 지운다. 위치만 잡고 두면 남은 z가 스냅샷에 실려 나가고, 남의 화면에서
            // 그 속도로 외삽되는 동안 새가 레인 밖으로 벌어져 보인다(다음 스냅샷이 오면 되돌아온다).
            var movedVelocity = result.velocity.ToNumerics();
            velocity.Linear = new System.Numerics.Vector3(movedVelocity.X, movedVelocity.Y, 0f);
            _motionBridge.PushMotion(entity);
        }

        private sealed class HitTrackingQuery : ICollisionQuery
        {
            private ICollisionQuery _inner;
            public bool SawHit { get; private set; }

            public void Reset(ICollisionQuery inner)
            {
                _inner = inner;
                SawHit = false;
            }

            public CollisionHit CapsuleCast(Vector3 point1, Vector3 point2, float radius,
                Vector3 direction, float distance, int layerMask)
            {
                var hit = _inner.CapsuleCast(point1, point2, radius, direction, distance, layerMask);
                if (hit.HasHit)
                {
                    SawHit = true;
                }
                return hit;
            }

            //  아래 둘은 그대로 넘기기만 한다. 이 데코레이터가 보려는 건 "이동이 벽에 막혔나"뿐이고,
            //  그건 sweep(CapsuleCast)만 답한다 — 시야 검사나 범위 조회는 이동을 막지 않는다.
            public CollisionHit Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask)
                => _inner.Raycast(origin, direction, distance, layerMask);

            public CollisionHit[] OverlapSphere(Vector3 center, float radius, int layerMask)
                => _inner.OverlapSphere(center, radius, layerMask);
        }
    }
}
