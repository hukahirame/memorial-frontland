using System.Collections.Generic;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class InventoryTests
    {
        private List<string> _items;
        private List<int> _stocks;
        private List<int> _maxStocks;

        /// <summary>空きスロットを slotCount 個持つインベントリを作る</summary>
        private Inventory CreateEmpty(int slotCount)
        {
            _items = new List<string>();
            _stocks = new List<int>();
            _maxStocks = new List<int>();
            for (int i = 0; i < slotCount; i++)
            {
                _items.Add(Inventory.EmptySlot);
                _stocks.Add(0);
                _maxStocks.Add(0);
            }
            return new Inventory(_items, _stocks, _maxStocks);
        }

        [Test]
        public void 空きスロットに新規配置される()
        {
            var inv = CreateEmpty(3);

            var r = inv.Add("Branch", 10);

            Assert.AreEqual(AddOutcome.Placed, r.Outcome);
            Assert.AreEqual(0, r.SlotIndex);
            Assert.AreEqual(1, r.Stock);
            Assert.AreEqual("Branch", _items[0]);
            Assert.AreEqual(10, _maxStocks[0]);
        }

        [Test]
        public void 同名かつ上限未満なら既存スロットに積まれる()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 10);

            var r = inv.Add("Branch", 10);

            Assert.AreEqual(AddOutcome.Stacked, r.Outcome);
            Assert.AreEqual(0, r.SlotIndex);
            Assert.AreEqual(2, r.Stock);
            Assert.AreEqual(Inventory.EmptySlot, _items[1], "2つ目のスロットは消費されない");
        }

        [Test]
        public void 上限に達した同名は別スロットに積まれる()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 2);
            inv.Add("Branch", 2);

            var r = inv.Add("Branch", 2);

            Assert.AreEqual(AddOutcome.Placed, r.Outcome);
            Assert.AreEqual(1, r.SlotIndex);
            Assert.AreEqual(2, _stocks[0], "元のスロットは上限のまま");
        }

        [Test]
        public void 積める枠も空きも無ければ失敗する()
        {
            var inv = CreateEmpty(1);
            inv.Add("Branch", 1);

            var r = inv.Add("Ironsword", 1);

            Assert.AreEqual(AddOutcome.NoSpace, r.Outcome);
            Assert.AreEqual(-1, r.SlotIndex);
            Assert.AreEqual("Branch", _items[0], "既存スロットは書き換えられない");
        }

        [Test]
        public void 削除で個数が減る()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 10);
            inv.Add("Branch", 10);

            var r = inv.Remove("Branch");

            Assert.AreEqual(RemoveOutcome.Decremented, r.Outcome);
            Assert.AreEqual(1, r.Stock);
            Assert.AreEqual("Branch", _items[0]);
        }

        [Test]
        public void 個数がゼロになるとスロットが空く()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 10);

            var r = inv.Remove("Branch");

            Assert.AreEqual(RemoveOutcome.SlotCleared, r.Outcome);
            Assert.AreEqual(Inventory.EmptySlot, _items[0]);
            Assert.AreEqual(0, _maxStocks[0]);
        }

        [Test]
        public void 所持していないアイテムの削除は失敗する()
        {
            var inv = CreateEmpty(3);

            var r = inv.Remove("Branch");

            Assert.AreEqual(RemoveOutcome.NotFound, r.Outcome);
            Assert.AreEqual(-1, r.SlotIndex);
        }

        [Test]
        public void 削除は最後のスロットから行われる()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 1);
            inv.Add("Branch", 1);

            var r = inv.Remove("Branch");

            Assert.AreEqual(1, r.SlotIndex);
            Assert.AreEqual(1, _stocks[0], "先頭のスロットは減らない");
        }
    }
}