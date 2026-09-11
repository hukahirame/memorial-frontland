using System.Collections.Generic;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class DropRuleTests
    {
        private static DropChance Drop(string itemId, int percent)
        {
            return new DropChance { ItemId = itemId, Percent = percent };
        }

        [Test]
        public void 必ず落ちるものが並んだ順に返る()
        {
            List<string> always = new List<string> { "Slime", "Jelly" };

            List<string> got = DropRule.Roll(always, null, new FakeRandom());

            CollectionAssert.AreEqual(new[] { "Slime", "Jelly" }, got);
        }

        [Test]
        public void 割合に当たったものが後ろに足される()
        {
            List<string> always = new List<string> { "Jelly" };
            List<DropChance> chances = new List<DropChance> { Drop("Slimecore", 10) };

            List<string> got = DropRule.Roll(always, chances, new FakeRandom(9));

            CollectionAssert.AreEqual(new[] { "Jelly", "Slimecore" }, got);
        }

        [Test]
        public void 割合を外したものは足されない()
        {
            List<DropChance> chances = new List<DropChance> { Drop("Slimecore", 10) };

            List<string> got = DropRule.Roll(null, chances, new FakeRandom(10));

            CollectionAssert.IsEmpty(got);
        }

        [Test]
        public void 空の取得物は乱数を引かずに飛ばす()
        {
            List<string> always = new List<string> { "Jelly", "", null };
            List<DropChance> chances = new List<DropChance> { Drop("", 100), null };
            FakeRandom random = new FakeRandom();

            List<string> got = DropRule.Roll(always, chances, random);

            CollectionAssert.AreEqual(new[] { "Jelly" }, got);
            Assert.AreEqual(0, random.Draws);
        }

        [Test]
        public void 割合ごとに1回ずつ引く()
        {
            List<DropChance> chances = new List<DropChance>
            {
                Drop("A", 50), Drop("B", 50), Drop("C", 50)
            };
            FakeRandom random = new FakeRandom(0, 99, 0);

            List<string> got = DropRule.Roll(null, chances, random);

            CollectionAssert.AreEqual(new[] { "A", "C" }, got);
            Assert.AreEqual(3, random.Draws);
        }

        [Test]
        public void 渡した一覧を書き換えない()
        {
            List<string> always = new List<string> { "Jelly" };
            List<DropChance> chances = new List<DropChance> { Drop("Slimecore", 100) };

            DropRule.Roll(always, chances, new FakeRandom());

            CollectionAssert.AreEqual(new[] { "Jelly" }, always);
        }
    }
}
