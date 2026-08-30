using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class HealthTests
    {
        private static Health Ready(int max, int current)
        {
            Health health = new Health();
            health.SetMax(max);
            health.SetCurrent(current);
            return health;
        }

        [Test]
        public void 上限が決まるまでは未準備()
        {
            Health health = new Health();

            Assert.IsFalse(health.IsReady);
            health.SetMax(300);
            Assert.IsTrue(health.IsReady);
        }

        [Test]
        public void 損害のぶん減る()
        {
            Health health = Ready(300, 300);

            health.Take(50);
            Assert.AreEqual(250, health.Current);
            Assert.IsFalse(health.IsDead);
        }

        [Test]
        public void 損害が残りを超えても0で止まる()
        {
            Health health = Ready(300, 10);

            health.Take(999);
            Assert.AreEqual(0, health.Current, "負にはならない");
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void 回復は上限で頭打ちになる()
        {
            Health health = Ready(300, 298);

            health.Heal(5);
            Assert.AreEqual(300, health.Current);
        }

        [Test]
        public void 死んでいると回復しない()
        {
            Health health = Ready(300, 0);

            health.Heal(5);
            Assert.AreEqual(0, health.Current, "回復で生き返らせない");
            Assert.IsTrue(health.IsDead);
        }

        [Test]
        public void 負の量では動かない()
        {
            Health health = Ready(300, 100);

            health.Take(-10);
            Assert.AreEqual(100, health.Current, "負の損害で増えてはいけない");

            health.Heal(-10);
            Assert.AreEqual(100, health.Current, "負の回復で減ってはいけない");
        }

        [Test]
        public void 読み込みは0と上限の間に収める()
        {
            Health health = Ready(300, 0);

            health.SetCurrent(-5);
            Assert.AreEqual(0, health.Current);

            health.SetCurrent(9999);
            Assert.AreEqual(300, health.Current);
        }

        [Test]
        public void 上限を下げると現在値も切り詰められる()
        {
            Health health = Ready(300, 300);

            health.SetMax(100);
            Assert.AreEqual(100, health.Current);
        }

        [Test]
        public void 上限は1を下回らない()
        {
            Health health = new Health();

            health.SetMax(0);
            Assert.AreEqual(1, health.Max);

            health.SetMax(-5);
            Assert.AreEqual(1, health.Max);
        }
    }
}
