using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 동전이 <b>모로 서지 못한다</b>는 것을 물리로 확인한다. 판치기의 승리 조건이 "전부 뒤집힘"이라
    /// 세로로 선 동전은 뒤집힌 것도 안 뒤집힌 것도 아닌 채로 남아 판을 막는다.
    ///
    /// <para><b>왜 실제로 굴려 보는가.</b> 이건 계산으로 못 맞힌다 — 처음엔 "동전이 두꺼워서 선다"고
    /// 보고 두께를 실제 비율(14:1)로 줄였는데 <b>서는 비율이 24/24로 하나도 안 바뀌었다.</b> 각도만 재면
    /// 7도→4도로 좋아진 것처럼 보이지만, 회전이 섞인 조건에서는 차이가 0이었다. 진짜 원인은 두께가 아니라
    /// <b>박스 콜라이더의 평평한 옆면</b>이었다 — 평평하면 거기 그대로 앉는다.</para>
    /// </summary>
    public class CoinStandingTests
    {
        private const float Radius = 0.15f;
        private const float Thickness = 0.04f;

        //  이보다 세계 위쪽과 나란하면 "누웠다"로 본다(0.9 ≈ 26도).
        private const float FlatDot = 0.9f;

        //  열려 있는 씬에 만들었다 지운다 — 테스트용 씬을 새로 열면 에디터가 물고 있는 씬 상태에
        //  걸린다("저장 안 된 씬이 있으면 additive로 못 연다"). 대신 다른 물체와 안 겹치게 멀리 둔다.
        private const float Far = -1000f;

        private SimulationMode previousMode;
        private GameObject ground;

        [SetUp]
        public void SetUp()
        {
            //  에디트 모드에서는 물리가 저절로 돌지 않는다 — 우리가 스텝을 먹인다.
            previousMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;

            ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "coin-test-ground";
            ground.transform.localScale = new Vector3(10f, 1f, 10f);
            ground.transform.position = new Vector3(0f, Far - 0.5f, 0f);
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = previousMode;
            if (ground != null) { Object.DestroyImmediate(ground); }
        }

        [Test]
        public void 모로_세운_동전은_조금만_건드려도_쓰러진다()
        {
            //  회전과 기울기를 격자로 훑는다. 한 조건만 보면 우연에 속는다 — 실제로 어떤 값에서는
            //  구르다 다시 옆면으로 착지해 서는 일이 있었다.
            //
            //  **완벽히 수직·무회전(회전 0 · 기울기 0)은 뺀다.** 좌우 대칭인 몸의 무게중심이 닿는 점
            //  바로 위에 있으면 회전을 만들 힘이 수학적으로 0이라, 옆면을 아무리 뾰족하게 깎아도
            //  그대로 서 있다(20%까지 깎아 봐도 같았다). 실제 동전이 안 서는 것은 모양 때문이 아니라
            //  공기·진동·미세한 비대칭 같은 잡음이 늘 있기 때문인데, 우리 세계엔 그 잡음이 없다.
            //  판에서는 동전이 늘 움직이다 멎으므로 이 조건은 손으로 놓지 않는 한 나오지 않는다.
            //  실제 플레이에서 서는 것이 보이면 그때 "멎었는데 서 있으면 아주 약한 힘을 준다"를 얹는다.
            int stood = 0, total = 0;
            for (int s = 0; s <= 5; s++)
            {
                for (int t = 0; t <= 3; t++)
                {
                    if (s == 0 && t == 0) { continue; }   // 위 문단 참고
                    total++;
                    if (StandsAfterSettling(spin: s * 0.4f, tiltDegrees: t)) { stood++; }
                }
            }

            //  하나라도 서면 그 판은 막힌다. 여기는 "거의"가 없다.
            //  (박스 콜라이더로 되돌리면 전부 선다 — 이 테스트가 그것을 잡는다.)
            Assert.AreEqual(0, stood, $"{total}가지 중 {stood}가지가 모로 선 채로 멎었다");
        }

        [Test]
        public void 납작하게_누운_동전은_그대로_있는다()
        {
            //  옆면을 둥글게 깎으면서 반대로 *누운* 동전이 흔들리게 만들면 안 된다.
            //  서지 못하게 만드는 것과 가만히 못 있게 만드는 것은 다른 문제다.
            GameObject coin = SpawnCoin(Quaternion.identity, Far + Radius);
            Simulate(150);

            float flatness = Mathf.Abs(Vector3.Dot(coin.transform.up, Vector3.up));
            Assert.Greater(flatness, FlatDot, "누워 있던 동전이 스스로 기울었다");
            Object.DestroyImmediate(coin);
        }

        private bool StandsAfterSettling(float spin, float tiltDegrees)
        {
            //  얇은 축(Y)을 눕혀 옆면이 바닥에 닿게 세우고, 거기서 tilt만큼 더 기울인다.
            GameObject coin = SpawnCoin(Quaternion.Euler(0f, 0f, 90f - tiltDegrees), Far + Radius + 0.001f);
            //  구르는 축으로 아주 작은 회전 — 동전이 멈추기 직전 늘 이만큼은 남아 있다.
            coin.GetComponent<Rigidbody>().angularVelocity = new Vector3(spin, 0f, 0f);

            Simulate(150);

            float flatness = Mathf.Abs(Vector3.Dot(coin.transform.up, Vector3.up));
            Object.DestroyImmediate(coin);
            return flatness <= FlatDot;
        }

        private GameObject SpawnCoin(Quaternion rotation, float height)
        {
            var go = new GameObject("coin-test-coin");

            var entity = new GameFramework.World.Entity("coin");
            entity.Add(new GameFramework.World.Transform
            {
                Position = new System.Numerics.Vector3(0f, height, 0f),
                Rotation = new System.Numerics.Quaternion(rotation.x, rotation.y, rotation.z, rotation.w),
            });
            entity.Add(new GameFramework.World.Velocity());
            entity.Add(new GameFramework.World.DiscShape(Radius, Thickness));
            entity.Add(new GameFramework.World.PhysicsConfig(
                GameFramework.World.BodyKind.Dynamic, freezeRotation: false, isTrigger: false));

            //  실제로 게임이 쓰는 그 팩토리를 그대로 부른다 — 여기서 몸을 따로 조립하면
            //  테스트가 통과해도 게임의 동전은 다른 몸일 수 있다.
            PhysicsBodyFactory.Create(go, entity);
            return go;
        }

        private static void Simulate(int steps)
        {
            for (int i = 0; i < steps; i++)
            {
                Physics.Simulate(0.02f);
            }
        }
    }
}
