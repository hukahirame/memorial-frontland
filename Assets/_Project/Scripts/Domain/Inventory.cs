using System.Collections.Generic;

namespace MemorialFloor.Domain
{
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
    /// 空きスロットは itemId が空文字で表される。
    /// 呼び出し元のリストを直接書き換えるため、リストを保持せず都度生成すること。
    /// </summary>
    public sealed class Inventory
    {
        public const string EmptySlot = "";

        private readonly IList<string> _items;
        private readonly IList<int> _stocks;
        private readonly IList<int> _maxStocks;

        public Inventory(IList<string> items, IList<int> stocks, IList<int> maxStocks)
        {
            _items = items;
            _stocks = stocks;
            _maxStocks = maxStocks;
        }

        public int SlotCount => _items.Count;

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
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == itemId && _stocks[i] < _maxStocks[i])
                {
                    _stocks[i]++;
                    return new AddResult(AddOutcome.Stacked, i, _stocks[i]);
                }
            }

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == EmptySlot)
                {
                    _items[i] = itemId;
                    _stocks[i]++;
                    _maxStocks[i] = maxStockForNewSlot();
                    return new AddResult(AddOutcome.Placed, i, _stocks[i]);
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
            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i] == itemId) total += _stocks[i];
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
            for (int i = _items.Count - 1; i >= 0; i--)
            {
                if (_items[i] == itemId)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0) return new RemoveResult(RemoveOutcome.NotFound, -1, 0);

            _stocks[index]--;
            if (_stocks[index] <= 0)
            {
                _items[index] = EmptySlot;
                _maxStocks[index] = 0;
                return new RemoveResult(RemoveOutcome.SlotCleared, index, 0);
            }

            return new RemoveResult(RemoveOutcome.Decremented, index, _stocks[index]);
        }
    }
}