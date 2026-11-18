using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>持ち物で選んだ品の情報の枠。名前・説明・絵を出し、使うボタンと捨てるボタンを並べる</summary>
    public sealed class ItemInfo : MonoBehaviour
    {
        /// <summary>使うボタンと捨てるボタンを並べるときの、1 つの幅と間。窓の幅は 500</summary>
        private const float PairWidth = 220f;
        private const float PairGap = 20f;

        [SerializeField, FormerlySerializedAs("mainImage")] private Image _image;
        [SerializeField, FormerlySerializedAs("nametxt")] private Text _nameLabel;
        [SerializeField, FormerlySerializedAs("txt")] private Text _description;
        [SerializeField, FormerlySerializedAs("button")] private Button _useButton;
        [SerializeField, FormerlySerializedAs("buttonsprite")] private Sprite _emptySprite;
        [SerializeField, FormerlySerializedAs("items")] private ItemBook _items;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private ItemActions _actions;

        /// <summary>選んでいる品。何も選んでいなければ null</summary>
        private ItemDefinition _current;

        /// <summary>捨てるボタン。使うボタンを写して作る。窓が閉じたまま Clear が呼ばれることがあるので、要るときに作る</summary>
        private Button _discard;

        /// <summary>使うボタンの、シーンで置いた位置と大きさ。並べるときに縮めるので、元を覚えておく</summary>
        private Vector2 _buttonPosition;
        private Vector2 _buttonSize;

        private void OnEnable()
        {
            _inventory.SlotChanged += ForgetEmptied;
            _inventory.Replaced += Clear;
        }

        private void OnDisable()
        {
            _inventory.SlotChanged -= ForgetEmptied;
            _inventory.Replaced -= Clear;
        }

        private void Start()
        {
            _useButton.gameObject.SetActive(false);
            DiscardButton().gameObject.SetActive(false);
        }

        /// <summary>品の情報を出す。原簿に無い品なら何もしない</summary>
        public void Show(string itemId)
        {
            ItemDefinition item = _items.Items.Find(itemId);
            if (item == null) return;

            _current = item;
            _nameLabel.text = item.Name;
            _description.text = item.Description;
            _image.sprite = Resources.Load<Sprite>(itemId);

            // 非表示のボタンからも文字を取る。既定の GetComponentInChildren は活性なものしか返さない
            _useButton.GetComponentInChildren<Text>(true).text = Label(item.Use);
            _useButton.gameObject.SetActive(item.Installable);
            DiscardButton().gameObject.SetActive(true);
            Lay(item.Installable);
        }

        public void Clear()
        {
            _current = null;
            _image.sprite = _emptySprite;
            _nameLabel.text = "";
            _description.text = "";
            _useButton.gameObject.SetActive(false);
            DiscardButton().gameObject.SetActive(false);
        }

        /// <summary>使うボタンから呼ぶ</summary>
        public void Use()
        {
            if (_current != null) _actions.Use(_current);
        }

        /// <summary>捨てるボタンから呼ぶ</summary>
        public void Throw()
        {
            if (_current != null) _actions.Throw(_current);
        }

        //枠が空いたら、その品を選んだままにしない
        private void ForgetEmptied(int index, int durability)
        {
            if (string.IsNullOrEmpty(_inventory.Slots[index].ItemId)) Clear();
        }

        private static string Label(ItemUse use)
        {
            switch (use)
            {
                case ItemUse.Eat: return "食べる";
                case ItemUse.Equip: return "装備";
                default: return "設置";
            }
        }

        private Button DiscardButton()
        {
            if (_discard != null) return _discard;

            var rect = (RectTransform)_useButton.transform;
            _buttonPosition = rect.anchoredPosition;
            _buttonSize = rect.sizeDelta;

            _discard = Instantiate(_useButton, _useButton.transform.parent);
            _discard.name = "Discard";
            //写すとシーンで繋いだ「使う」の呼び先も付いてくる。Inspector で繋いだ呼び先は RemoveAllListeners では外れない
            _discard.onClick = new Button.ButtonClickedEvent();
            _discard.onClick.AddListener(Throw);
            _discard.GetComponentInChildren<Text>(true).text = "捨てる";
            _discard.gameObject.SetActive(false);

            return _discard;
        }

        //使うボタンがあれば左右に並べ、無ければ捨てるボタンを使うボタンの位置に出す
        private void Lay(bool usable)
        {
            var use = (RectTransform)_useButton.transform;
            var throwAway = (RectTransform)DiscardButton().transform;
            float offset = (PairWidth + PairGap) / 2f;

            use.anchoredPosition = _buttonPosition + Vector2.left * offset;
            use.sizeDelta = new Vector2(PairWidth, _buttonSize.y);

            throwAway.anchoredPosition = usable ? _buttonPosition + Vector2.right * offset : _buttonPosition;
            throwAway.sizeDelta = usable ? new Vector2(PairWidth, _buttonSize.y) : _buttonSize;
        }
    }
}
