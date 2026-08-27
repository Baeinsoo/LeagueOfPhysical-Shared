using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PanchigiTurnTests
    {
        private static readonly string[] TwoPlayers = { "A", "B" };

        private static PanchigiTurn Aiming(IReadOnlyList<string> players, int turnLimit = 60, int dropOutLimit = 3)
        {
            var turn = new PanchigiTurn(players, turnLimit, dropOutLimit);
            turn.OnRested(false, false);   // 판 시작 직후 한 번 — 여기서 첫 조준으로 들어간다
            return turn;
        }

        [Test]
        public void 낙_없이_치면_벌점이_안_쌓인다()
        {
            var turn = Aiming(TwoPlayers);
            string striker = turn.CurrentEntityId;

            turn.OnStruck(striker);
            turn.OnRested(false, false);

            Assert.AreEqual(0, turn.GetDropOutCount(striker));
        }

        [Test]
        public void 낙이_나면_친_사람에게_벌점이_붙는다()
        {
            var turn = Aiming(TwoPlayers);
            string striker = turn.CurrentEntityId;

            turn.OnStruck(striker);
            turn.OnRested(false, true);

            Assert.AreEqual(1, turn.GetDropOutCount(striker));
            Assert.AreEqual(0, turn.GetDropOutCount("B"), "낙은 친 사람만의 벌점이다");
        }

        [Test]
        public void 벌점이_한도에_닿으면_그_사람이_빠진다()
        {
            var turn = Aiming(TwoPlayers, dropOutLimit: 2);

            //  A가 두 번 낙 — 사이에 B의 차례가 한 번 낀다
            turn.OnStruck("A"); turn.OnRested(false, true);
            turn.OnStruck("B"); turn.OnRested(false, false);
            turn.OnStruck("A"); turn.OnRested(false, true);

            Assert.IsTrue(turn.IsEliminated("A"));
            Assert.IsFalse(turn.IsEliminated("B"));
        }

        [Test]
        public void 둘_중_하나가_빠지면_남은_사람이_이긴다()
        {
            var turn = Aiming(TwoPlayers, dropOutLimit: 1);

            turn.OnStruck("A");
            turn.OnRested(false, true);

            Assert.AreEqual(PanchigiPhase.Over, turn.Phase);
            Assert.AreEqual("B", turn.WinnerEntityId);
        }

        [Test]
        public void 빠진_사람에게는_차례가_안_돌아온다()
        {
            var turn = Aiming(new[] { "A", "B", "C" }, dropOutLimit: 1);

            turn.OnStruck("A");
            turn.OnRested(false, true);   // A 탈락, 아직 둘 남아 계속된다

            Assert.AreEqual(PanchigiPhase.Aiming, turn.Phase);

            //  한 바퀴를 다 돌려도 A는 안 나온다
            for (int i = 0; i < 6; i++)
            {
                Assert.AreNotEqual("A", turn.CurrentEntityId);
                turn.OnStruck(turn.CurrentEntityId);
                turn.OnRested(false, false);
            }
        }

        [Test]
        public void 낙과_전부_뒤집힘이_같이_나면_낙이_이긴다()
        {
            //  판을 되돌리면 뒤집힌 동전이 남지 않으므로 승리가 성립할 수 없다.
            var turn = Aiming(TwoPlayers);

            turn.OnStruck("A");
            turn.OnRested(true, true);

            Assert.AreNotEqual(PanchigiPhase.Over, turn.Phase, "낙이 났으면 그 턴에 이길 수 없다");
            Assert.AreEqual(1, turn.GetDropOutCount("A"));
        }

        [Test]
        public void 조준_시간을_넘겨도_벌점은_안_붙는다()
        {
            var turn = Aiming(TwoPlayers);
            string passer = turn.CurrentEntityId;

            turn.OnAimTimeout();

            Assert.AreEqual(0, turn.GetDropOutCount(passer), "패스는 낙이 아니다");
        }

        [Test]
        public void 한도가_영이면_탈락시키지_않는다()
        {
            //  설정으로 벌칙을 꺼 둘 수 있어야 한다.
            var turn = Aiming(TwoPlayers, dropOutLimit: 0);

            turn.OnStruck("A");
            turn.OnRested(false, true);

            Assert.IsFalse(turn.IsEliminated("A"));
            Assert.AreEqual(PanchigiPhase.Aiming, turn.Phase);
        }
    }
}
