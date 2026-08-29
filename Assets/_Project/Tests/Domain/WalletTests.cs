using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class WalletTests
    {
        [Test]
        public void 受け取ると増える()
        {
            Wallet wallet = new Wallet();

            wallet.Add(100);
            Assert.AreEqual(100, wallet.Amount);

            wallet.Add(200);
            Assert.AreEqual(300, wallet.Amount);
        }

        [Test]
        public void 負の量は受け取らない()
        {
            Wallet wallet = new Wallet();
            wallet.SetAmount(100);

            wallet.Add(-50);
            Assert.AreEqual(100, wallet.Amount, "負の Add で減ってはいけない");

            wallet.Add(0);
            Assert.AreEqual(100, wallet.Amount);
        }

        [Test]
        public void 足りていれば払える()
        {
            Wallet wallet = new Wallet();
            wallet.SetAmount(300);

            Assert.IsTrue(wallet.Spend(100));
            Assert.AreEqual(200, wallet.Amount);
        }

        [Test]
        public void ちょうど同額なら払える()
        {
            Wallet wallet = new Wallet();
            wallet.SetAmount(100);

            Assert.IsTrue(wallet.CanAfford(100));
            Assert.IsTrue(wallet.Spend(100));
            Assert.AreEqual(0, wallet.Amount);
        }

        [Test]
        public void 足りなければ何も減らない()
        {
            Wallet wallet = new Wallet();
            wallet.SetAmount(99);

            Assert.IsFalse(wallet.CanAfford(100));
            Assert.IsFalse(wallet.Spend(100));
            Assert.AreEqual(99, wallet.Amount, "部分的に引いてはいけない");
        }

        [Test]
        public void 負の額は払えない()
        {
            Wallet wallet = new Wallet();
            wallet.SetAmount(100);

            Assert.IsFalse(wallet.Spend(-10));
            Assert.AreEqual(100, wallet.Amount, "負の Spend で増えてはいけない");
        }

        [Test]
        public void 読み込みで負の値は0になる()
        {
            Wallet wallet = new Wallet();

            wallet.SetAmount(-5);
            Assert.AreEqual(0, wallet.Amount);

            wallet.SetAmount(1000);
            Assert.AreEqual(1000, wallet.Amount);
        }
    }
}
