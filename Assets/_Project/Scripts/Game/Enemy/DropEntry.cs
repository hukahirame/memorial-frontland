using System;
using MemorialFloor.Domain;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>割合で落ちる取得物 1 つぶんの、敵の prefab に保存する入れ物。落とすときに DropChance にする</summary>
    [Serializable]
    public sealed class DropEntry
    {
        [FormerlySerializedAs("ItemId")] public string itemId = string.Empty;
        [FormerlySerializedAs("Percent")] public int percent;

        public DropChance ToChance()
        {
            return new DropChance(itemId, percent);
        }
    }
}
