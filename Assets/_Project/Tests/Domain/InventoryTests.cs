using System.Collections.Generic;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class InventoryTests
    {
        private List<ItemSlot> _slots;

        /// <summary>空きスロットを slotCount 個持つインベントリを作る</summary>
        private Inventory CreateEmpty(int slotCount)
        {
            _slots = new List<ItemSlot>();
            for (int i = 0; i < slotCount; i++) _slots.Add(new ItemSlot());

            return new Inventory(_slots);
        }

        [Test]
        public void 空きスロットに新規配置される()
        {
            var inv = CreateEmpty(3);

            var r = inv.Add("Branch", 10);

            Assert.AreEqual(AddOutcome.Placed, r.Outcome);
            Assert.AreEqual(0, r.SlotIndex);
            Assert.AreEqual(1, r.Stock);
            Assert.AreEqual("Branch", _slots[0].ItemId);
            Assert.AreEqual(10, _slots[0].MaxStock);
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
            Assert.AreEqual(Inventory.EmptySlot, _slots[1].ItemId, "2つ目のスロットは消費されない");
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
            Assert.AreEqual(2, _slots[0].Stock, "元のスロットは上限のまま");
        }

        [Test]
        public void 積める枠も空きも無ければ失敗する()
        {
            var inv = CreateEmpty(1);
            inv.Add("Branch", 1);

            var r = inv.Add("Ironsword", 1);

            Assert.AreEqual(AddOutcome.NoSpace, r.Outcome);
            Assert.AreEqual(-1, r.SlotIndex);
            Assert.AreEqual("Branch", _slots[0].ItemId, "既存スロットは書き換えられない");
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
            Assert.AreEqual("Branch", _slots[0].ItemId);
        }

        [Test]
        public void 個数がゼロになるとスロットが空く()
        {
            var inv = CreateEmpty(3);
            inv.Add("Branch", 10);

            var r = inv.Remove("Branch");

            Assert.AreEqual(RemoveOutcome.SlotCleared, r.Outcome);
            Assert.AreEqual(Inventory.EmptySlot, _slots[0].ItemId);
            Assert.AreEqual(0, _slots[0].MaxStock);
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
            Assert.AreEqual(1, _slots[0].Stock, "先頭のスロットは減らない");
        }
    }
}