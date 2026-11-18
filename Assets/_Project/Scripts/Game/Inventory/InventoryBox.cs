using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>持ち物の枠の箱 1 つ。入っている品の絵と数か耐久を出し、押すとその品の情報を出す</summary>
    public sealed class InventoryBox : MonoBehaviour
    {
        private Image _image;
        private Text _count;
        private Slider _durability;
        private ItemInfo _info;

        /// <summary>入っている品。空いていれば null</summary>
        public string ItemId { get; private set; }

        //箱は場に並べたものを写して増やすので、子は名前で引く
        private void Awake()
        {
            _image = GetComponent<Image>();
            _count = transform.Find("Text").GetComponent<Text>();
            _durability = transform.Find("Slider").GetComponent<Slider>();
        }

        /// <summary>押したときに品を出す情報の枠。並べる側が渡す</summary>
        public void Bind(ItemInfo info)
        {
            _info = info;
        }

        /// <summary>品が入ってきた。耐久を持つ品は数の代わりに耐久を出す。絵は原画のまま載せるので、縮尺が合わないことがある</summary>
        public void Ready(string itemId, int durability, int maxDurability)
        {
            ItemId = itemId;
            _image.sprite = Resources.Load<Sprite>(itemId);
            _count.text = "1";

            if (durability == 0) return;

            _count.text = "";
            _durability.transform.localScale = Vector3.one;
            if (maxDurability > 0) _durability.maxValue = maxDurability;
            _durability.value = durability;
        }

        public void ShowStock(int stock)
        {
            _count.text = stock.ToString();
        }

        /// <summary>枠が空いた。前の品を覚えたままだと、押したときに持っていない品が出て使えてしまう</summary>
        public void Clear(Sprite empty)
        {
            ItemId = null;
            _image.sprite = empty;
            _count.text = "";
            _durability.transform.localScale = new Vector3(0, 1, 1); //場の既定。横を 0 にして隠す
        }

        /// <summary>ボタンから呼ぶ</summary>
        public void Select()
        {
            if (ItemId != null && _info != null) _info.Show(ItemId);
        }
    }
}
