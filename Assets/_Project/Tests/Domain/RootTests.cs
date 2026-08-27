using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class RootTests
    {
        /// <summary>MainSite の「はじまりの根源」に相当する初期値</summary>
        private static Root NewRoot()
        {
            return new Root("Root1", "はじまりの根源", "01010101", 1000, 0f, -100f);
        }

        [Test]
        public void 一日経つと蓄積値が10増える()
        {
            var root = NewRoot();

            root.AdvanceDay();

            Assert.AreEqual(10, root.Accumulation);
        }

        [Test]
        public void 一日経つと攻略度が3減る()
        {
            var root = NewRoot();
            root.Gain(10);

            root.AdvanceDay();

            Assert.AreEqual(7, root.Progress);
        }

        [Test]
        public void 攻略度は0を下回らない()
        {
            var root = NewRoot();

            root.AdvanceDay();

            Assert.AreEqual(0, root.Progress);
        }

        [Test]
        public void 蓄積値は0を下回らない()
        {
            var root = NewRoot();
            root.AdvanceDay(); //10

            root.Calm(30);

            Assert.AreEqual(0, root.Accumulation);
        }

        [Test]
        public void 危険度は変化しない()
        {
            var root = NewRoot();

            for (int i = 0; i < 20; i++) root.AdvanceDay();
            root.Calm(5);
            root.Gain(50);

            Assert.AreEqual(1000, root.Danger);
        }

        [TestCase(0, AccumulationLevel.Minimal)]
        [TestCase(14, AccumulationLevel.Minimal)]
        [TestCase(15, AccumulationLevel.Small)]
        [TestCase(39, AccumulationLevel.Small)]
        [TestCase(40, AccumulationLevel.Medium)]
        [TestCase(74, AccumulationLevel.Medium)]
        [TestCase(75, AccumulationLevel.High)]
        [TestCase(99, AccumulationLevel.High)]
        [TestCase(100, AccumulationLevel.Stampede)]
        [TestCase(200, AccumulationLevel.Stampede)]
        public void 蓄積値の段階は閾値どおりに変わる(int accumulation, AccumulationLevel expected)
        {
            var root = NewRoot();
            int days = (accumulation + 9) / 10; //1日 +10 なので、超えるまで進めてから端数を戻す
            for (int i = 0; i < days; i++) root.AdvanceDay();
            root.Calm(days * 10 - accumulation);

            Assert.AreEqual(expected, root.Level);
        }

        [Test]
        public void 十日放置すると氾濫する()
        {
            var root = NewRoot();

            for (int i = 0; i < 10; i++) root.AdvanceDay();

            Assert.AreEqual(100, root.Accumulation);
            Assert.AreEqual(AccumulationLevel.Stampede, root.Level);
        }

        [Test]
        public void スポーン地点は最初は未設定()
        {
            var root = NewRoot();

            Assert.IsFalse(root.HasSpawnPoint);
        }

        [Test]
        public void スポーン地点を設定すると保持される()
        {
            var root = NewRoot();

            root.PlaceSpawnPoint(1.5f, 2.5f, 3.5f);

            Assert.IsTrue(root.HasSpawnPoint);
            Assert.AreEqual(1.5f, root.SpawnX);
            Assert.AreEqual(2.5f, root.SpawnY);
            Assert.AreEqual(3.5f, root.SpawnZ);
        }
    }

    public class RootRegistryTests
    {
        private static Root NewRoot(string id)
        {
            return new Root(id, id + "の根源", "0101", 1000, 0f, 0f);
        }

        [Test]
        public void 追加した根源をIDで引ける()
        {
            var registry = new RootRegistry();
            registry.TryAdd(NewRoot("Root1"));

            var found = registry.Find("Root1");

            Assert.IsNotNull(found);
            Assert.AreEqual("Root1", found.Id);
        }

        [Test]
        public void 存在しないIDを引くとnullが返る()
        {
            var registry = new RootRegistry();

            Assert.IsNull(registry.Find("Root9"));
        }

        [Test]
        public void 同じIDの根源は二度追加されない()
        {
            var registry = new RootRegistry();
            registry.TryAdd(NewRoot("Root1"));

            bool added = registry.TryAdd(NewRoot("Root1"));

            Assert.IsFalse(added);
            Assert.AreEqual(1, registry.Count);
        }

        [Test]
        public void 二度追加しても最初の根源が残る()
        {
            var registry = new RootRegistry();
            var first = NewRoot("Root1");
            registry.TryAdd(first);
            first.Gain(40);

            registry.TryAdd(NewRoot("Root1"));

            Assert.AreEqual(40, registry.Find("Root1").Progress);
        }

        [Test]
        public void 一日経つと全ての根源が進む()
        {
            var registry = new RootRegistry();
            registry.TryAdd(NewRoot("Root1"));
            registry.TryAdd(NewRoot("Root2"));

            registry.AdvanceDay();

            Assert.AreEqual(10, registry.Find("Root1").Accumulation);
            Assert.AreEqual(10, registry.Find("Root2").Accumulation);
        }
    }
}
