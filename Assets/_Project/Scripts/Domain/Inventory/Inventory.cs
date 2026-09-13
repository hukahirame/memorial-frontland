using System;
using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// スロット1つぶん。空きは ItemId が空文字。
    /// Unity の Inspector と JsonUtility が読むため、public フィールドで持つ（[D-007] の例外）。
    /// </summary>
    [Serializable]
    public sealed class ItemSlot
    {
        public string ItemId = Inventory.EmptySlot;
        public int Stock;
        public int MaxStock;
    }

    public enum AddOutcome
    {
        /// <summary>既存スロットに積まれた</summary>
        Stacked,
        /// <summary>空きスロットに新規配置された</summary>
        Placed,
        /// <summary>積める枠も空きスロットも無かった</summary>
        NoSpace
    }

    public enum RemoveOutcome
    {
        /// <summary>個数が減っただけ</summary>
        Decremented,
        /// <summary>個数が0になりスロットが空いた</summary>
        SlotCleared,
        /// <summary>そのアイテムを所持していない</summary>
        NotFound
    }

    public readonly struct AddResult
    {
        public AddOutcome Outcome { get; }
        public int SlotIndex { get; }
        public int Stock { get; }

        public AddResult(AddOutcome outcome, int slotIndex, int stock)
        {
            Outcome = outcome;
            SlotIndex = slotIndex;
            Stock = stock;
        }
    }

    public readonly struct RemoveResult
    {
        public RemoveOutcome Outcome { get; }
        public int SlotIndex { get; }
        public int Stock { get; }

        public RemoveResult(RemoveOutcome outcome, int slotIndex, int stock)
        {
            Outcome = outcome;
            SlotIndex = slotIndex;
            Stock = stock;
        }
    }

    /// <summary>
    /// スロット制インベントリの格納規則。表示・入出力は扱わない。
    /// 空きスロットは ItemId が空文字で表される。
    /// 渡されたスロットの並びを直接書き換える。
    /// </summary>
    public sealed class Inventory
    {
        public const string EmptySlot = "";

        private readonly IList<ItemSlot> _slots;

        public Inventory(IList<ItemSlot> slots)
        {
            _slots = slots;
        }

        public int SlotCount => _slots.Count;

        /// <summary>
        /// アイテムを1個追加する。同名で上限未満のスロットがあればそこに積み、
        /// 無ければ空きスロットに新規配置する。
        /// </summary>
        /// <param name="maxStockForNewSlot">新規配置時に設定される上限。既存スロットに積む場合は使われない</param>
        public AddResult Add(string itemId, int maxStockForNewSlot)
        {
            return Add(itemId, () => maxStockForNewSlot);
        }

        /// <summary>
        /// 上限値の取得が高コストな場合向け。新規配置が必要になった時点でのみ評価される。
        /// </summary>
        public AddResult Add(string itemId, System.Func<int> maxStockForNewSlot)
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemId == itemId && _slots[i].Stock < _slots[i].MaxStock)
                {
                    _slots[i].Stock++;
                    return new AddResult(AddOutcome.Stacked, i, _slots[i].Stock);
                }
            }

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemId == EmptySlot)
                {
                    _slots[i].ItemId = itemId;
                    _slots[i].Stock++;
                    _slots[i].MaxStock = maxStockForNewSlot();
                    return new AddResult(AddOutcome.Placed, i, _slots[i].Stock);
                }
            }

            return new AddResult(AddOutcome.NoSpace, -1, 0);
        }

        /// <summary>
        /// 指定アイテムの所持数を、複数スロットにまたがって合計する。
        /// </summary>
        public int CountOf(string itemId)
        {
            int total = 0;
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].ItemId == itemId) total += _slots[i].Stock;
            }
            return total;
        }
        /// <summary>
        /// アイテムを1個減らす。最後に見つかったスロットから減らし、
        /// 0以下になったらスロットを空にする。
        /// </summary>
        public RemoveResult Remove(string itemId)
        {
            int index = -1;
            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                if (_slots[i].ItemId == itemId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return new RemoveResult(RemoveOutcome.NotFound, -1, 0);

            _slots[index].Stock--;
            if (_slots[index].Stock <= 0)
            {
                _slots[index].ItemId = EmptySlot;
                _slots[index].MaxStock = 0;
                return new RemoveResult(RemoveOutcome.SlotCleared, index, 0);
            }

            return new RemoveResult(RemoveOutcome.Decremented, index, _slots[index].Stock);
        }
    }
}