namespace MemorialFloor.Game
{
    /// <summary>
    /// 拾ったものの受け取り先。Game から Legacy の実装を名指しせずに渡すための口。
    /// </summary>
    public interface IItemReceiver
    {
        /// <summary>受け取れたら 1、入らなければ 0</summary>
        int LoadInventory(string itemId, int durability);
    }
}
