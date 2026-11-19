using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>倒したときに何が落ちるかの決め方</summary>
    public static class DropRule
    {
        /// <summary>
        /// 必ず落ちるものに、割合で当たったものを足す。
        /// 空の ItemId は乱数を引かずに飛ばす
        /// </summary>
        public static List<string> Roll(IEnumerable<string> always, IEnumerable<DropChance> chances, IRandom random)
        {
            var result = new List<string>();

            if (always != null)
            {
                foreach (string itemId in always)
                {
                    if (!string.IsNullOrEmpty(itemId)) result.Add(itemId);
                }
            }

            if (chances == null) return result;

            foreach (DropChance chance in chances)
            {
                if (chance == null || string.IsNullOrEmpty(chance.ItemId)) continue;
                if (Chance.Roll(chance.Percent, random)) result.Add(chance.ItemId);
            }

            return result;
        }
    }
}
