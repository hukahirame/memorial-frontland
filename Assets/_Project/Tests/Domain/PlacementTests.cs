using System.Linq;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class PlacementLedgerTests
    {
        private static Placement Torch(float x)
        {
            return new Placement("Torch", x, 0.25f, -3f);
        }

        [Test]
        public void 控えの無い場所は空()
        {
            PlacementLedger ledger = new PlacementLedger();

            Assert.IsEmpty(ledger.In("MainSite"));
            Assert.IsEmpty(ledger.In(null));
        }

        [Test]
        public void 書き直すと前の控えは残らない()
        {
            PlacementLedger ledger = new PlacementLedger();

            ledger.Record("MainSite", new[] { Torch(1f), Torch(2f) });
            ledger.Record("MainSite", new[] { Torch(3f) });

            Assert.AreEqual(1, ledger.In("MainSite").Count);
            Assert.AreEqual(3f, ledger.In("MainSite")[0].X);
        }

        [Test]
        public void ほかの場所の控えには触れない()
        {
            PlacementLedger ledger = new PlacementLedger();

            ledger.Record("MainSite", new[] { Torch(1f) });
            ledger.Record("Root1", new[] { Torch(5f), Torch(6f) });
            ledger.Record("Root1", new Placement[0]);

            Assert.AreEqual(1, ledger.In("MainSite").Count);
            Assert.IsEmpty(ledger.In("Root1"));
        }

        [Test]
        public void 一つも無くなった場所は控えのある場所から外れる()
        {
            PlacementLedger ledger = new PlacementLedger();

            ledger.Record("MainSite", new[] { Torch(1f) });
            ledger.Record("Root1", new[] { Torch(5f) });
            ledger.Record("Root1", null);

            CollectionAssert.AreEqual(new[] { "MainSite" }, ledger.Areas.ToArray());
        }

        [Test]
        public void 書き直したあとに元の並びを変えても控えは変わらない()
        {
            PlacementLedger ledger = new PlacementLedger();
            Placement[] placed = { Torch(1f) };

            ledger.Record("MainSite", placed);
            placed[0] = Torch(9f);

            Assert.AreEqual(1f, ledger.In("MainSite")[0].X);
        }

        [Test]
        public void 控えは置いた物の向きも持つ()
        {
            PlacementLedger ledger = new PlacementLedger();

            ledger.Record("MainSite", new[] { new Placement("Fence", 4f, 0f, -2f, 90f), Torch(1f) });

            Assert.AreEqual(90f, ledger.In("MainSite")[0].Yaw);
            Assert.AreEqual(0f, ledger.In("MainSite")[1].Yaw, "向きを渡さずに置いた物が向きを持つ");
        }

        [Test]
        public void 全て捨てると空になる()
        {
            PlacementLedger ledger = new PlacementLedger();

            ledger.Record("MainSite", new[] { Torch(1f) });
            ledger.Clear();

            Assert.IsEmpty(ledger.Areas);
            Assert.IsEmpty(ledger.In("MainSite"));
        }
    }
}
