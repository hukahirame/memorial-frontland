using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>アイテム1種の素性。原簿の1行に対応する</summary>
    public sealed class ItemDefinition
    {
        /// <summary>システム内名称。インベントリとレシピが使う ID</summary>
        public string ItemId { get; }

        /// <summary>UI に出す表示名</summary>
        public string Name { get; }

        /// <summary>1スロットに積める上限</summary>
        public int MaxStock { get; }

        /// <summary>耐久値の上限。0 は耐久の概念を持たない</summary>
        public int MaxDurability { get; }

        /// <summary>設置できるか</summary>
        public bool Installable { get; }

        public string Description { get; }

        public ItemDefinition(string itemId, string name, int maxStock,
                              int maxDurability, bool installable, string description)
        {
            ItemId = itemId;
            Name = name;
            MaxStock = maxStock;
            MaxDurability = maxDurability;
            Installable = installable;
            Description = description;
        }
    }

    /// <summary>
    /// アイテムの原簿。CSV 1行が1種で、先頭行は見出しとして読み飛ばす。
    /// 列は 0:ID 1:名称 2:最大ストック 3:最大耐久値 4:設置可 10:紹介文。
    /// </summary>
    public sealed class ItemCatalog
    {
        private const int Columns = 11;
        private const int ItemIdColumn = 0;
        private const int NameColumn = 1;
        private const int MaxStockColumn = 2;
        private const int MaxDurabilityColumn = 3;
        private const int InstallableColumn = 4;
        private const int DescriptionColumn = 10;

        private readonly List<ItemDefinition> _items = new List<ItemDefinition>();

        public IReadOnlyList<ItemDefinition> All
        {
            get { return _items; }
        }

        public int Count
        {
            get { return _items.Count; }
        }

        /// <summary>
        /// 原簿を読む。列が足りない行と空行は捨てる。
        /// 数として読めない値は 0 にする
        /// </summary>
        public static ItemCatalog Parse(string csv)
        {
            ItemCatalog catalog = new ItemCatalog();
            catalog.Load(csv);

            return catalog;
        }

        /// <summary>読み直す。以前の中身は捨てる</summary>
        public void Load(string csv)
        {
            _items.Clear();
            if (string.IsNullOrEmpty(csv)) return;

            string[] lines = csv.Replace("\r", "").Split('\n');
            bool heading = true;

            foreach (string raw in lines)
            {
                string line = raw.Trim('\ufeff').Trim();
                if (line.Length == 0) continue;

                if (heading)
                {
                    heading = false;
                    continue;
                }

                string[] cell = line.Split(',');
                if (cell.Length < Columns) continue;

                _items.Add(new ItemDefinition(
                    cell[ItemIdColumn].Trim(),
                    cell[NameColumn].Trim(),
                    Number(cell[MaxStockColumn]),
                    Number(cell[MaxDurabilityColumn]),
                    Number(cell[InstallableColumn]) == 1,
                    cell[DescriptionColumn].Trim()));
            }
        }

        /// <summary>見つからなければ null。呼ぶ側で探索を書かない</summary>
        public ItemDefinition Find(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return null;

            for (int i = 0; i < _items.Count; i++)
            {
                if (_items[i].ItemId == itemId) return _items[i];
            }

            return null;
        }

        private static int Number(string cell)
        {
            int value;

            return int.TryParse(cell.Trim(), out value) ? value : 0;
        }
    }
}
