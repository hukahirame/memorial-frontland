using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>品の原簿の持ち主。常駐シーンに 1 つ置き、起動したときに原簿の CSV を読む</summary>
    public sealed class ItemBook : MonoBehaviour
    {
        [SerializeField] private TextAsset _itemData;

        public ItemCatalog Items { get; } = new ItemCatalog();

        //使う側はどれも Start 以降に引く
        private void Awake()
        {
            Items.Load(_itemData.text);
        }
    }
}
