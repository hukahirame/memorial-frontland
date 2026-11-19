namespace MemorialFloor.Domain
{
    /// <summary>割合で落ちる取得物 1 つぶん。敵の prefab に保存する入れ物は Game の DropEntry</summary>
    public sealed class DropChance
    {
        public DropChance(string itemId, int percent)
        {
            ItemId = itemId ?? string.Empty;
            Percent = percent;
        }

        /// <summary>空なら落ちない</summary>
        public string ItemId { get; }

        public int Percent { get; }
    }
}
