using GameFramework;
using LOP;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    public class MovementSystemTests
    {
        const float Tolerance = 1e-4f;

        private static MovementResult Move(Vector3 cur, float h, float v,
            float speed = 5f, float maxAccel = 100f, float dt = 0.1f)
            => MovementSystem.ProcessMovement(new MovementInput(cur, h, v, speed, maxAccel, dt));

        [Test]
        public void Tick_InputlessEntity_ResolvesExternalForceIntoVelocity()
        {
            var movement = new MovementSystem(new GameFramework.World.StatsSystem(), new MotionContributionSystem());

            var e = new GameFramework.World.Entity("e1");
            e.Add(new GameFramework.World.Velocity { Linear = new System.Numerics.Vector3(2, 5, 0) });  // 브레인 속도
            var mc = new MotionContributions();
            mc.Items.Add(new MotionContribution(new System.Numerics.Vector3(0, 0, 8),
                MotionContributionMode.Additive, 0, 0, 100, 1f));   // 활성 넉백, decay 1
            e.Add(mc);
            // InputBuffer 없음 (AI/원격)

            movement.Tick(e, 0, 0.05f);

            var v = e.Get<GameFramework.World.Velocity>().Linear;
            Assert.Less(System.Numerics.Vector3.Distance(v, new System.Numerics.Vector3(2, 5, 8)), 1e-4f,
                "입력 없어도 외력 folding, y 보존");
        }

        [Test]
        public void NoInput_BrakesTowardZero_PreservesY()
        {
            // 방향 입력이 없으면 목표 0으로 제동(정지). rate(=maxAccel·dt=10) >= 현재(5) → 0. 위아래 속도는 보존.
            var r = Move(new Vector3(5f, -7.5f, 0f), 0f, 0f);

            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(r.velocity.y, Is.EqualTo(-7.5f).Within(Tolerance));
            Assert.IsFalse(r.hasRotation);
        }

        [Test]
        public void Forward_AcceleratesTowardSpeed_PreservesY()
        {
            // 멈춰 있다가 앞으로 입력 → 한 번에 최대 속도. 위아래 속도(3)는 그대로.
            var r = Move(new Vector3(0f, 3f, 0f), 0f, 1f);

            Assert.IsTrue(r.hasRotation);
            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(r.velocity.y, Is.EqualTo(3f).Within(Tolerance));
            Assert.That(r.velocity.z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void DirectionChange_BrakesPerpendicular_NoDrift()
        {
            // 오른쪽으로 8 가다가 위로 입력 → 오른쪽 속도(8)가 사라지고 위로만(5). 옆으로 안 미끄러짐.
            var r = Move(new Vector3(8f, 0f, 0f), 0f, 1f, maxAccel: 200f);

            Assert.That(r.velocity.x, Is.EqualTo(0f).Within(Tolerance), "방향전환 시 직각 관성이 남으면 안 됨(드리프트)");
            Assert.That(r.velocity.z, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void OverSpeed_BrakesTowardMoveSpeed()
        {
            // 최대 속도(5)보다 빠른 8에서 같은 방향 입력 → 목표인 5로 줄어든다.
            var r = Move(new Vector3(-8f, 0f, 0f), -1f, 0f);

            Assert.That(r.velocity.x, Is.EqualTo(-5f).Within(Tolerance));
        }

        [Test]
        public void PreservesYVelocity()
        {
            var r = Move(new Vector3(0f, -7.5f, 0f), 1f, 0f);

            Assert.That(r.velocity.y, Is.EqualTo(-7.5f).Within(Tolerance));
        }

        [Test]
        public void Rotation_FacesMoveDirection()
        {
            // 오른쪽(+x) 입력 → 오른쪽(90도)을 바라봄
            var r = Move(Vector3.zero, 1f, 0f);

            Assert.IsTrue(r.hasRotation);
            Assert.That(r.rotation.y, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Reverse_AcceleratesOppositeInstant()
        {
            // 왼쪽으로 8 가다가 오른쪽 입력 → 한 번에 오른쪽 5로 바뀜(반응 빠를 때).
            var r = Move(new Vector3(-8f, 0f, 0f), 1f, 0f, maxAccel: 200f);

            Assert.That(r.velocity.x, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void AccelCap_LimitsChangePerTick()
        {
            // 반응 빠르기를 작게(20) 주면 → 한 틱에 2까지만 (최대 5에는 아직 못 미침).
            var r = Move(Vector3.zero, 0f, 1f, maxAccel: 20f);

            Assert.That(r.velocity.z, Is.EqualTo(2f).Within(Tolerance));
        }
    }

    /// <summary>인스턴스 Tick — PlayerInput(이번 틱 입력)을 읽어 World.Velocity/Transform에 쓰는 이동 시스템.</summary>
    public class MovementSystemTickTests
    {
        const float Tolerance = 1e-3f;
        const float Dt = 0.1f;   // MaxAcceleration(100)×0.1=10 ≥ moveSpeed(5) → 한 틱에 목표 도달

        private MovementSystem system;

        [SetUp]
        public void SetUp()
        {
            system = new MovementSystem(new GameFramework.World.StatsSystem(), new MotionContributionSystem());
        }

        // Current에 커맨드가 확정된(호스트가 소비 완료한) 조종 엔티티를 만든다.
        private static GameFramework.World.Entity CreateControlledEntity(Vector3 velocity, InputCommand current)
        {
            var entity = new GameFramework.World.Entity("e1");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new GameFramework.World.Velocity { Linear = velocity.ToNumerics() });
            var stats = new GameFramework.World.Stats();
            stats.BaseStats[(int)GameFramework.World.EntityStatType.MoveSpeed] = 5f;
            stats.BaseStats[(int)GameFramework.World.EntityStatType.JumpPower] = 12f;
            entity.Add(stats);
            entity.Add(new InputBuffer { Current = current });
            return entity;
        }

        [Test]
        public void NoInputBuffer_DoesNothing()
        {
            // 조종 안 되는 엔티티(AI/원격/아이템)는 InputBuffer가 없다 → 속도 그대로.
            var entity = new GameFramework.World.Entity("e1");
            entity.Add(new GameFramework.World.Velocity { Linear = new Vector3(3f, 0f, 0f).ToNumerics() });

            system.Tick(entity, 0, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void NoCurrentCommand_DoesNothing()
        {
            // 버퍼는 있으나 이번 틱 확정 커맨드가 없으면(Current=null) 손대지 않는다.
            var entity = CreateControlledEntity(new Vector3(3f, 0f, 0f), null);

            system.Tick(entity, 0, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(3f).Within(Tolerance));
        }

        [Test]
        public void Input_WritesVelocityAndRotation()
        {
            // 오른쪽 입력 → 오른쪽 5, 90도 바라봄
            var entity = CreateControlledEntity(Vector3.zero, new InputCommand { Horizontal = 1f });

            system.Tick(entity, 0, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(5f).Within(Tolerance));
            Assert.That(entity.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles.y, Is.EqualTo(90f).Within(Tolerance));
        }

        [Test]
        public void Jump_SetsYFromJumpPowerStat()
        {
            var entity = CreateControlledEntity(new Vector3(0f, -3f, 0f), new InputCommand { Jump = true });

            system.Tick(entity, 0, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().y, Is.EqualTo(12f).Within(Tolerance));
        }

        [Test]
        public void ZeroInput_BrakesHorizontal_PreservesY()
        {
            // 무입력 틱(호스트가 0 커맨드를 확정) → 수평은 0으로 제동, 수직은 중력 몫이라 보존.
            var entity = CreateControlledEntity(new Vector3(5f, -7.5f, 0f), new InputCommand());

            system.Tick(entity, 0, Dt);

            Vector3 v = entity.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(v.x, Is.EqualTo(0f).Within(Tolerance));
            Assert.That(v.y, Is.EqualTo(-7.5f).Within(Tolerance));
        }

        [Test]
        public void ActiveMotionEffect_DerivesDashVelocity_FromFacing_IgnoresInput()
        {
            // 대시 활성 창 안 → 모터가 forward×Speed를 직접 쓴다(입력 무시). 기본 rotation=identity → forward=+z.
            var entity = CreateControlledEntity(new Vector3(15f, 0f, 0f), new InputCommand { Vertical = 1f });
            var abilities = new Abilities();
            abilities.ActiveAbility = new ActiveAbility(2, AbilityPhase.Startup, 0, 100, 200, null,
                new AbilityEffect[] { new MotionEffect(15f) });   // 창 [0,100)
            entity.Add(abilities);

            system.Tick(entity, 0, Dt);   // 0 ∈ [0,100)

            Vector3 v = entity.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(v.x, Is.EqualTo(0f).Within(Tolerance), "입력 무시(락)");
            Assert.That(v.z, Is.EqualTo(15f).Within(Tolerance), "forward(+z)×15");
        }

        [Test]
        public void OutsideDashWindow_WalksNormally()
        {
            // 창 밖 tick이면 대시 아님 → 걷기(입력대로).
            var entity = CreateControlledEntity(Vector3.zero, new InputCommand { Horizontal = 1f });
            var abilities = new Abilities();
            abilities.ActiveAbility = new ActiveAbility(2, AbilityPhase.Startup, 0, 5, 10, null,
                new AbilityEffect[] { new MotionEffect(15f) });   // 창 [0,5)
            entity.Add(abilities);

            system.Tick(entity, 5, Dt);   // 5 ∉ [0,5) → 걷기

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(5f).Within(Tolerance));
        }

        [Test]
        public void AdditiveContribution_AddsOnTopOfWalk()
        {
            // 외부 Additive 기여(넉백류)는 걷기 위에 가산된다.
            var entity = CreateControlledEntity(Vector3.zero, new InputCommand { Horizontal = 1f }); // 걷기 → x=5
            var contribs = new MotionContributions();
            contribs.Items.Add(new MotionContribution(new System.Numerics.Vector3(0f, 0f, 4f),
                MotionContributionMode.Additive, 0, 0, 10));
            entity.Add(contribs);

            system.Tick(entity, 0, Dt);

            Vector3 v = entity.Get<GameFramework.World.Velocity>().Linear.ToUnity();
            Assert.That(v.x, Is.EqualTo(5f).Within(Tolerance), "걷기 x=5");
            Assert.That(v.z, Is.EqualTo(4f).Within(Tolerance), "additive z=4 가산");
        }

        // 진행 중 어빌리티(배율/점프)를 붙인 조종 엔티티. 창 [10,100) active, [100,200) recovery.
        private static void AttachAbility(GameFramework.World.Entity e, float active, bool blockJump)
        {
            var ab = new Abilities();
            ab.ActiveAbility = new ActiveAbility(3, AbilityPhase.Active, 10, 100, 200, null,
                new AbilityEffect[0], 1f, active, 1f, blockJump);
            e.Add(ab);
        }

        [Test]
        public void ActiveAbility_RootsMovement_ButKeepsRotation()
        {
            // activeMoveScale=0 → 수평 0으로 정지, 그러나 방향은 입력(오른쪽=90도)따라 살아있음.
            var entity = CreateControlledEntity(Vector3.zero, new InputCommand { Horizontal = 1f });
            AttachAbility(entity, active: 0f, blockJump: false);

            system.Tick(entity, 50, Dt);   // 50 ∈ [10,100) active

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(0f).Within(Tolerance), "루팅");
            Assert.That(entity.Get<GameFramework.World.Transform>().Rotation.ToUnity().eulerAngles.y, Is.EqualTo(90f).Within(Tolerance), "회전 살아있음");
        }

        [Test]
        public void ActiveAbility_PartialScale_SlowsMovement()
        {
            // activeMoveScale=0.4 → 목표 5 × 0.4 = 2.
            var entity = CreateControlledEntity(Vector3.zero, new InputCommand { Horizontal = 1f });
            AttachAbility(entity, active: 0.4f, blockJump: false);

            system.Tick(entity, 50, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(2f).Within(Tolerance));
        }

        [Test]
        public void BlockJump_IgnoresJumpInput()
        {
            var entity = CreateControlledEntity(new Vector3(0f, -3f, 0f), new InputCommand { Jump = true });
            AttachAbility(entity, active: 1f, blockJump: true);

            system.Tick(entity, 50, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().y, Is.EqualTo(-3f).Within(Tolerance), "점프 무시(y 미변경)");
        }

        [Test]
        public void AI_RootsResidualVelocity_DuringAttack()
        {
            // InputBuffer 없는 AI + 잔류 속도(5,0,0) + activeMoveScale=0 → 수평 0(미끄러짐 소멸).
            var entity = new GameFramework.World.Entity("ai");
            entity.Add(new GameFramework.World.Transform());
            entity.Add(new GameFramework.World.Velocity { Linear = new Vector3(5f, 0f, 0f).ToNumerics() });
            AttachAbility(entity, active: 0f, blockJump: false);

            system.Tick(entity, 50, Dt);

            Assert.That(entity.Get<GameFramework.World.Velocity>().Linear.ToUnity().x, Is.EqualTo(0f).Within(Tolerance));
        }
    }
}
