using System.Collections.Generic;
using NUnit.Framework;

namespace LOP.Tests
{
    public class PlayerLookTests
    {
        [Test]
        public void 생성자_slots를_그대로_보관한다()
        {
            var slots = new Dictionary<string, string> { ["hat"] = "hat_001" };
            var look = new PlayerLook(slots, "길동", 5);

            Assert.AreEqual("hat_001", look.Slots["hat"]);
        }

        [Test]
        public void 생성자_slots가_null이면_빈_사전이_된다()
        {
            var look = new PlayerLook(null, "길동", 5);

            Assert.AreEqual(0, look.Slots.Count);
        }

        [Test]
        public void 생성자_displayName이_null이면_빈_문자열이_된다()
        {
            var look = new PlayerLook(null, null, 5);

            Assert.AreEqual(string.Empty, look.DisplayName);
        }

        [Test]
        public void 생성자_accountLevel이_1보다_작으면_1이_된다()
        {
            var look = new PlayerLook(null, "길동", 0);

            Assert.AreEqual(1, look.AccountLevel);
        }

        [Test]
        public void 생성자_slots를_넘긴_뒤_호출자가_사전을_바꿔도_컴포넌트는_영향받지_않는다()
        {
            var slots = new Dictionary<string, string> { ["hat"] = "hat_001" };
            var look = new PlayerLook(slots, "길동", 5);

            slots["hat"] = "hat_999";

            Assert.AreEqual("hat_001", look.Slots["hat"]);
        }

        [Test]
        public void SlotOrNull_있는_슬롯코드면_품목코드를_반환한다()
        {
            var slots = new Dictionary<string, string> { ["hat"] = "hat_001" };
            var look = new PlayerLook(slots, "길동", 5);

            Assert.AreEqual("hat_001", look.SlotOrNull("hat"));
        }

        [Test]
        public void SlotOrNull_없는_슬롯코드면_null을_반환한다()
        {
            var look = new PlayerLook(null, "길동", 5);

            Assert.IsNull(look.SlotOrNull("hat"));
        }

        [Test]
        public void SlotOrNull_null코드면_null을_반환한다()
        {
            var slots = new Dictionary<string, string> { ["hat"] = "hat_001" };
            var look = new PlayerLook(slots, "길동", 5);

            Assert.IsNull(look.SlotOrNull(null));
        }
    }
}
