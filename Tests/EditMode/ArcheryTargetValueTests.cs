using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;

namespace LOP.Tests
{
    /// <summary>
    /// 과녁이 들고 다니는 <b>값</b>에 대한 시험. 값 그릇을 바꾸는 동안 원형 맵의 과녁이
    /// 한 글자도 안 바뀌는 것을 박제한다(특성 시험 — 고치기 <b>전</b>에 통과해야 한다).
    /// </summary>
    public class ArcheryTargetValueTests
    {
        private static ArcheryConfig CircleLikeConfig()
        {
            //  배포된 원형 맵 값 그대로(#ArcheryConfig.xlsx id=5, #ArcheryTarget.xlsx 6줄).
            //  여기 숫자가 그 파일과 같아야 이 시험이 실제로 도는 판을 재는 것이 된다.
            var kinds = new List<ArcheryTargetKind>
            {
                new ArcheryTargetKind(0.45f, 1, 30, false, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.30f, 2, 40, false, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.20f, 4, 30, false, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.45f, -3, 30, true, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.30f, -5, 40, true, ArcheryTargetShape.Sphere, null),
                new ArcheryTargetKind(0.20f, -10, 30, true, ArcheryTargetShape.Sphere, null),
            };
            return new ArcheryConfig(120, 3, 5, 3.5f, 0.3f, 0.6f, 1.2f, 0f, 1f,
                                     1.2f, 2.5f, 0f, 1.2f, 2.4f, 12, 20, kinds);
        }

        //  비교하기 쉬운 한 줄로 만든다. 부동소수는 반올림이 숨지 않게 비트로 적는다.
        private static string Canonical(List<ArcheryTarget> targets)
        {
            string B(float v) => System.BitConverter.SingleToInt32Bits(v).ToString("X8");
            var sb = new StringBuilder();
            foreach (var t in targets)
            {
                sb.Append(t.WaveIndex).Append('/').Append(t.SlotIndex)
                  .Append(' ').Append(B(t.Origin.x)).Append(',').Append(B(t.Origin.y)).Append(',').Append(B(t.Origin.z))
                  .Append(' ').Append(B(t.RiseSpeed)).Append(' ').Append(t.SpawnTick)
                  .Append(' ').Append(B(t.Radius)).Append(' ').Append(t.Points)
                  .Append(' ').Append(t.IsTrap ? 'T' : 'F').Append(' ').Append((int)t.Shape)
                  .Append('\n');
            }
            return sb.ToString();
        }

        [Test]
        public void 원형_맵_과녁_목록이_값_그릇을_바꿔도_그대로다()
        {
            var targets = new List<ArcheryTarget>();
            var sb = new StringBuilder();
            for (int wave = 0; wave < 5; wave++)
            {
                ArcheryWaveGenerator.Fill(targets, 4841841021168904955UL, wave, CircleLikeConfig(), 1000L);
                sb.Append(Canonical(targets));
            }

            //  이 문자열은 고치기 **전** 코드를 돌려서 얻은 것이다. 값 그릇을 바꾼 뒤에도 같아야 한다.
            //  다르면 원형 맵의 과녁이 실제로 달라진 것이다 — 문자열을 고쳐 통과시키지 말고 코드를 되돌릴 것.
            //  금값은 여러 줄짜리 문자열 리터럴이라 파일의 줄바꿈이 그대로 값이 된다 —
            //  이 저장소를 CRLF로 체크아웃하는 기계에서는 리터럴에 캐리지리턴이 섞여 들어가
            //  줄바꿈만 쓰는 위 Canonical과 영원히 안 맞는다. 비교 전에 한쪽으로 맞춘다.
            Assert.AreEqual(Golden.Replace("\r\n", "\n"), sb.ToString());
        }

        //  중력 상수(ArcheryTrajectory.Gravity)가 20 -> 9.81로 바뀌면서 여기 실린 RiseSpeed
        //  8자리 hex만 다시 구웠다(sqrt(2gH)가 g에 그대로 걸리므로). 그 값을 뽑는 자리·개수·
        //  순서·나머지 필드는 전부 그대로다 — 중력과 무관한 것까지 같이 흔들리면 코드가
        //  아니라 이 golden부터 의심해야 한다.
        private const string Golden =
@"0/0 C0061A92,3F173BF2,BF5CB10B 40A1CA51 1000 3E4CCCCD -10 T 0
0/1 4056ED78,3EC515B3,BF0D5DD3 40C27454 1012 3E4CCCCD -10 T 0
0/2 3F40AF48,3F06D71F,C007B7B6 40A240CC 1024 3E4CCCCD -10 T 0
0/3 BF702A68,3EF101B1,C01C0B92 40C3A8EF 1036 3E99999A -5 T 0
1/0 C00876AF,3EBE4DC0,BF3F96CD 40C72716 1120 3E99999A 2 F 0
1/1 3FE4E639,3F0AB29C,BEED16EF 40BF51AA 1132 3E4CCCCD 4 F 0
1/2 BD8D5C5A,3F10360C,3F7BDE23 40A276FA 1144 3E4CCCCD 4 F 0
2/0 3FFCB2EA,3EC33F5B,40240C31 40B17413 1240 3EE66666 -3 T 0
2/1 C003CE8B,3EBED11B,402E2824 40A2F8DE 1252 3E99999A -5 T 0
2/2 BF8BDC54,3EB87D6F,3F543766 40A8F957 1264 3E4CCCCD -10 T 0
2/3 3F38DE98,3F00B9F4,C002C299 40D7D724 1276 3E99999A -5 T 0
2/4 3F802D2B,3F0B5F45,3F778C04 40DAC8DB 1288 3E99999A -5 T 0
3/0 BF80114B,3ED9CAF2,BF02939F 40A1748E 1360 3E99999A 2 F 0
3/1 3EFF6CD7,3EA6DBE3,402C6A53 40CC7E31 1372 3E99999A -5 T 0
3/2 BF872C3E,3F1208D2,BFF11A91 40BAC402 1384 3EE66666 1 F 0
3/3 40223BC1,3ED8C958,400F2BB3 40BC3CB8 1396 3E99999A -5 T 0
4/0 3FB0BED8,3E9A9AAD,3D91768C 40A6575F 1480 3E99999A 2 F 0
4/1 40519DDE,3F196C9A,3DE2B4E7 40C219A0 1492 3E99999A -5 T 0
4/2 BFB67DCE,3F06A60B,3EE0A231 40B9015B 1504 3E4CCCCD 4 F 0
4/3 3FA7A02F,3F093226,C03EC0F1 40C52AD9 1516 3E99999A -5 T 0
";

        [Test]
        public void 솟는_과녁의_수명은_솟는_속도에서_나온다()
        {
            var targets = new List<ArcheryTarget>();
            ArcheryWaveGenerator.Fill(targets, 12345UL, 0, CircleLikeConfig(), 0L);

            Assert.IsNotEmpty(targets, "과녁이 하나도 안 떴다 — 이 시험은 아무것도 재지 못한다");
            foreach (var t in targets)
            {
                //  값으로 실렸어도 웨이브 과녁의 수명은 예전 식 그대로여야 한다.
                Assert.AreEqual(2f * t.RiseSpeed / ArcheryTargetMotion.Gravity, t.LifetimeSeconds, 1e-6f);
            }
        }

        [Test]
        public void 웨이브_과녁은_주인이_없어_누구나_가져간다()
        {
            var targets = new List<ArcheryTarget>();
            ArcheryWaveGenerator.Fill(targets, 12345UL, 0, CircleLikeConfig(), 0L);

            Assert.IsNotEmpty(targets, "과녁이 하나도 안 떴다 — 이 시험은 아무것도 재지 못한다");
            foreach (var t in targets)
            {
                Assert.AreEqual(string.Empty, t.OwnerUserId);
                Assert.IsTrue(ArcheryHitRules.CanTake(t, "누구든"));
            }
        }

        [Test]
        public void 주인이_있으면_주인만_가져간다()
        {
            var mine = new ArcheryTarget(0, 0, Vector3.zero, 0f, 0L, 0.5f, 5, false,
                                         ArcheryTargetShape.Face, null, Vector3.back, 4f, "user-1");

            Assert.IsTrue(ArcheryHitRules.CanTake(mine, "user-1"));
            Assert.IsFalse(ArcheryHitRules.CanTake(mine, "user-2"),
                "남의 과녁을 가져갈 수 있으면 태워 버리는 방해가 열린다(스펙 5절)");
        }

        [Test]
        public void 서_있는_과녁은_수명이_솟는_속도와_무관하다()
        {
            var standing = new ArcheryTarget(0, 0, new Vector3(0f, 1f, 30f), 0f, 100L, 0.61f, 5, false,
                                             ArcheryTargetShape.Face, null, Vector3.back, 4f, "user-1");

            Assert.AreEqual(4f, standing.LifetimeSeconds);
            //  제자리에 선 과녁 — 어느 시각에 물어도 같은 자리다.
            Assert.AreEqual(standing.Origin, ArcheryTargetMotion.PositionAt(standing, 150d, 0.02f));
            Assert.IsTrue(ArcheryTargetMotion.IsAlive(standing, 150d, 0.02f));
            Assert.IsFalse(ArcheryTargetMotion.IsAlive(standing, 400d, 0.02f),
                "4초(200틱)가 지났는데 아직 살아 있다");
        }
    }
}
