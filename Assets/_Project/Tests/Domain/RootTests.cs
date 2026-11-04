using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class RootTests
    {
        /// <summary>幅の下端を返す。増え方を固定した根源では、何を引いても同じだけ増える</summary>
        private static readonly IRandom Dice = new FakeRandom();

        /// <summary>MainSite の「はじまりの根源」に相当する初期値。日数で数えられるよう、増え方は 10 に固定する</summary>
        private static Root NewRoot()
        {
            return new Root("Root1", "はじまりの根源", "01010101", 1000, 0f, -100f, 10, 10);
        }

        [Test]
        public void 一日経つと蓄積値が10増える()
        {
            var root = NewRoot();

            root.AdvanceDay(Dice);

            Assert.AreEqual(10, root.Accumulation);
        }

        [Test]
        public void 一日経つと攻略度が3減る()
        {
            var root = NewRoot();
            root.Gain(10);

            root.AdvanceDay(Dice);

            Assert.AreEqual(7, root.Progress);
        }

        [Test]
        public void 攻略度は0を下回らない()
        {
            var root = NewRoot();

            root.AdvanceDay(Dice);

            Assert.AreEqual(0, root.Progress);
        }

        [Test]
        public void 攻略度は上限で止まる()
        {
            var root = NewRoot();

            root.Gain(Root.MaxProgress - 5);
            root.Gain(15);

            Assert.AreEqual(Root.MaxProgress, root.Progress);
        }

        [Test]
        public void 上げたらいくつになるかを上げる前に知れる()
        {
            var root = NewRoot();
            root.Gain(90);

            Assert.AreEqual(Root.MaxProgress, root.ProgressAfterGain(15));
            Assert.AreEqual(95, root.ProgressAfterGain(5));
            Assert.AreEqual(90, root.Progress, "知るだけで上がる");
        }

        [Test]
        public void 保存から戻すと攻略度と蓄積値が入る()
        {
            var root = NewRoot();

            root.Restore(40, 55);
            Assert.AreEqual(40, root.Progress);
            Assert.AreEqual(55, root.Accumulation);

            root.Restore(Root.MaxProgress + 50, -5);
            Assert.AreEqual(Root.MaxProgress, root.Progress, "上限を超えて戻る");
            Assert.AreEqual(0, root.Accumulation, "負の蓄積値が戻る");
        }

        [Test]
        public void 蓄積値は0を下回らない()
        {
            var root = NewRoot();
            root.AdvanceDay(Dice); //10

            root.Calm(30);

            Assert.AreEqual(0, root.Accumulation);
        }

        [Test]
        public void 危険度は変化しない()
        {
            var root = NewRoot();

            for (int i = 0; i < 20; i++) root.AdvanceDay(Dice);
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
            for (int i = 0; i < days; i++) root.AdvanceDay(Dice);
            root.Calm(days * 10 - accumulation);

            Assert.AreEqual(expected, root.Level);
        }

        [Test]
        public void 十日放置すると氾濫する()
        {
            var root = NewRoot();

            for (int i = 0; i < 10; i++) root.AdvanceDay(Dice);

            Assert.AreEqual(100, root.Accumulation);
            Assert.AreEqual(AccumulationLevel.Stampede, root.Level);
        }

        [Test]
        public void 氾濫すると蓄積値が0に戻り拠点に5体攻め込む()
        {
            var root = NewRoot();
            for (int i = 0; i < 10; i++) root.AdvanceDay(Dice);

            int raiders = root.Overflow();

            Assert.AreEqual(5, raiders);
            Assert.AreEqual(0, root.Accumulation);
        }

        [Test]
        public void 氾濫していなければ何も起きない()
        {
            var root = NewRoot();
            for (int i = 0; i < 9; i++) root.AdvanceDay(Dice); //90

            int raiders = root.Overflow();

            Assert.AreEqual(0, raiders);
            Assert.AreEqual(90, root.Accumulation);
        }

        [Test]
        public void 氾濫は続けて起きない()
        {
            var root = NewRoot();
            for (int i = 0; i < 10; i++) root.AdvanceDay(Dice);
            root.Overflow();

            root.AdvanceDay(Dice);

            Assert.AreEqual(0, root.Overflow());
        }

        [TestCase(0, 10)]
        [TestCase(9, 10)]
        [TestCase(10, 9)]
        [TestCase(30, 7)]
        [TestCase(50, 5)]
        [TestCase(70, 3)]
        [TestCase(200, 1)]
        public void 攻略度が上がるほど日ごとの蓄積が減る(int progress, int expected)
        {
            var root = NewRoot();
            root.Gain(progress);

            Assert.AreEqual(expected, root.MaxDailyGain);

            root.AccumulateDaily(Dice);
            Assert.AreEqual(expected, root.Accumulation);
        }

        [Test]
        public void 次の零時に氾濫するかを前もって知れる()
        {
            var root = NewRoot();
            for (int i = 0; i < 8; i++) root.AccumulateDaily(Dice); //80

            Assert.IsFalse(root.MayOverflowNextDay);

            root.AccumulateDaily(Dice); //90
            Assert.IsTrue(root.MayOverflowNextDay);
        }

        [Test]
        public void 攻略度が高いと氾濫の前夜も遅れる()
        {
            var root = NewRoot();
            for (int i = 0; i < 9; i++) root.AccumulateDaily(Dice); //90
            root.Gain(50); //次の増加は 5

            Assert.IsFalse(root.MayOverflowNextDay);
        }

        [Test]
        public void 氾濫している最中は前夜とみなさない()
        {
            var root = NewRoot();
            for (int i = 0; i < 10; i++) root.AccumulateDaily(Dice); //100

            Assert.IsFalse(root.MayOverflowNextDay);
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
            return new Root(id, id + "の根源", "0101", 1000, 0f, 0f, 10, 10);
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
        public void 全て捨てると空になる()
        {
            var registry = new RootRegistry();
            registry.TryAdd(NewRoot("Root1"));
            registry.TryAdd(NewRoot("Root2"));

            registry.Clear();

            Assert.AreEqual(0, registry.Count);
            Assert.IsNull(registry.Find("Root1"));
        }

        [Test]
        public void 一日経つと全ての根源が進む()
        {
            var registry = new RootRegistry();
            registry.TryAdd(NewRoot("Root1"));
            registry.TryAdd(NewRoot("Root2"));

            registry.AdvanceDay(new FakeRandom());

            Assert.AreEqual(10, registry.Find("Root1").Accumulation);
            Assert.AreEqual(10, registry.Find("Root2").Accumulation);
        }
    }

    public class BaseRaidTests
    {
        [Test]
        public void 氾濫で攻め込んだ敵は位置を持たず満タンで控える()
        {
            var raid = new BaseRaid();
            raid.Invade(5);

            Assert.AreEqual(5, raid.Kept.Count);
            Assert.IsFalse(raid.Kept[0].HasPosition);
            Assert.AreEqual(Raider.FullHp, raid.Kept[0].Hp);
        }

        [Test]
        public void 離れるときに控えた敵は位置と体力を持つ()
        {
            var raid = new BaseRaid();
            raid.Keep(new Raider(1f, 2f, 3f, 40));

            Raider kept = raid.Kept[0];
            Assert.IsTrue(kept.HasPosition);
            Assert.AreEqual(1f, kept.X);
            Assert.AreEqual(2f, kept.Y);
            Assert.AreEqual(3f, kept.Z);
            Assert.AreEqual(40, kept.Hp);
        }

        [Test]
        public void 出すと控えをすべて渡して空になる()
        {
            var raid = new BaseRaid();
            raid.Invade(2);
            raid.Keep(new Raider(1f, 2f, 3f, 40));

            Assert.AreEqual(3, raid.TakeAll().Count);
            Assert.AreEqual(0, raid.Kept.Count);
        }

        [Test]
        public void 負の数は控えない()
        {
            var raid = new BaseRaid();

            raid.Invade(-3);

            Assert.AreEqual(0, raid.Kept.Count);
        }
    }
}
