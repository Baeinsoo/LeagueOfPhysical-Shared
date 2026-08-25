using NUnit.Framework;

namespace LOP.Tests
{
    /// <summary>
    /// 밀어내기가 "지금 World가 들고 있는 자리"를 보고 판정하는지 고정한다.
    /// 엔진 트랜스폼(리지드바디)은 물리 스텝 뒤에야 갱신돼 한 틱 늦고, 클라 롤백 재생 중에는
    /// 아예 얼어 있다 — 그걸 보고 겹침을 판정하면 라이브 틱과 재생 틱이 다른 답을 낸다.
    /// 이 검증은 물리 엔진 없이 된다: 밀어낼 벡터를 *계산*하는 데는 PhysX가 필요하지만,
    /// *어떤 포즈를 넘기는지*는 순수 배선이라 가짜 몸으로 관찰할 수 있다.
    /// </summary>
    public class MotionBridgeTests
    {
        /// <summary>넘겨받은 포즈를 기록하는 가짜 몸. 엔진 쪽 자리는 일부러 다른 값으로 들고 있는다.</summary>
        private class RecordingBody : GameFramework.World.PhysicsBody
        {
            public System.Numerics.Vector3 StalePose = new System.Numerics.Vector3(-999f, -999f, -999f);
            public System.Numerics.Vector3 ReceivedPosition;
            public System.Numerics.Quaternion ReceivedRotation;
            public int ReceivedLayerMask;
            public System.Numerics.Vector3 PushToReturn;

            public override bool IsKinematic => true;

            public override void SetPosition(System.Numerics.Vector3 position) { }
            public override void SetRotation(System.Numerics.Quaternion rotation) { }
            public override void SetVelocity(System.Numerics.Vector3 linear) { }

            public override System.Numerics.Vector3 GetPosition() => StalePose;
            public override System.Numerics.Quaternion GetRotation() => System.Numerics.Quaternion.Identity;
            public override System.Numerics.Vector3 GetVelocity() => System.Numerics.Vector3.Zero;

            public override System.Numerics.Vector3 ComputePushOut(
                System.Numerics.Vector3 position, System.Numerics.Quaternion rotation, int layerMask)
            {
                ReceivedPosition = position;
                ReceivedRotation = rotation;
                ReceivedLayerMask = layerMask;
                return PushToReturn;
            }
        }

        private const int EnvMask = 1 << 3;
        private const int CharMask = 1 << 9;

        private static GameFramework.World.Entity MakeEntity(
            System.Numerics.Vector3 position, out RecordingBody body)
        {
            var entity = new GameFramework.World.Entity("bird");
            entity.Add(new GameFramework.World.Transform
            {
                Position = position,
                Rotation = System.Numerics.Quaternion.CreateFromAxisAngle(
                    System.Numerics.Vector3.UnitY, 1.2f),
            });
            body = new RecordingBody();
            entity.Add<GameFramework.World.PhysicsBody>(body);
            return entity;
        }

        [Test]
        public void 밀어내기는_엔진_자리가_아니라_World_자리로_판정한다()
        {
            var worldPosition = new System.Numerics.Vector3(5f, 7f, 0f);
            var entity = MakeEntity(worldPosition, out RecordingBody body);
            var transform = entity.Get<GameFramework.World.Transform>();

            new MotionBridge(EnvMask, CharMask, 1f).Depenetrate(entity);

            Assert.AreEqual(worldPosition, body.ReceivedPosition);
            Assert.AreEqual(transform.Rotation, body.ReceivedRotation);
            Assert.AreNotEqual(body.StalePose, body.ReceivedPosition, "엔진 쪽 자리를 읽고 있다");
        }

        [Test]
        public void 밀어낸_만큼_World_자리가_움직인다()
        {
            var entity = MakeEntity(new System.Numerics.Vector3(5f, 7f, 0f), out RecordingBody body);
            body.PushToReturn = new System.Numerics.Vector3(0f, 0.25f, 0f);

            new MotionBridge(EnvMask, CharMask, 1f).Depenetrate(entity);

            Assert.AreEqual(7.25f, entity.Get<GameFramework.World.Transform>().Position.Y, 1e-4f);
        }

        [Test]
        public void 지형과_캐릭터는_서로_다른_레이어로_판정한다()
        {
            var entity = MakeEntity(System.Numerics.Vector3.Zero, out RecordingBody body);
            var bridge = new MotionBridge(EnvMask, CharMask, 1f);

            bridge.Depenetrate(entity);
            Assert.AreEqual(EnvMask, body.ReceivedLayerMask);

            bridge.Separate(entity);
            Assert.AreEqual(CharMask, body.ReceivedLayerMask);
        }
    }
}
