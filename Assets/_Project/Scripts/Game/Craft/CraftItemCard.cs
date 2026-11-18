using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>作業台の画面で選んだ品の、絵・名前・説明の枠</summary>
    public sealed class CraftItemCard : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("mainImage")] private Image _image;
        [SerializeField, FormerlySerializedAs("nametxt")] private Text _nameLabel;
        [SerializeField, FormerlySerializedAs("txt")] private Text _description;

        /// <summary>品を出す。原簿に無い品なら前のまま</summary>
        public void Show(ItemDefinition item)
        {
            if (item == null) return;

            _nameLabel.text = item.Name;
            _image.sprite = Resources.Load<Sprite>(item.ItemId);
            _description.text = item.Description;
        }
    }
}
