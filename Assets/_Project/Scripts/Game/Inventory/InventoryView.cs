using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 持ち物の枠の箱を並べて描く。持ち物と装備の知らせを受けて、箱の絵と数、「装備中」の印を描き直す。
    /// 持ち物の画面の一番上に、PlayerInventory と並べて置く
    /// </summary>
    public sealed class InventoryView : MonoBehaviour
    {
        /// <summary>この物の直下で、箱より前に並ぶ物の数</summary>
        private const int BoxesFrom = 5;

        /// <summary>1 段に並ぶ箱の数</summary>
        private const int BoxesPerRow = 5;

        /// <summary>受け皿の大きさ。箱に対する比。品の絵が受け皿の縁に掛からないよう、ひと回り大きくする</summary>
        private const float SlotBackScale = 1.1f;

        /// <summary>印の字の大きさ。枠の中の大きさで、画面では枠と一緒に縮む</summary>
        private const float EquippedMarkSize = 36f;

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private ItemBook _items;
        [SerializeField] private ItemInfo _info;

        [Tooltip("空いた箱の絵")]
        [SerializeField] private Sprite _emptySprite;

        [Tooltip("枠の箱の後ろに敷く受け皿の絵。空いた箱は中身の絵が無く、敷かないとそこに箱があることが見えない")]
        [SerializeField] private Sprite _slotBack;

        private readonly List<InventoryBox> _boxes = new List<InventoryBox>();

        /// <summary>装備している武器の入った枠に付ける印。1 つだけ作り、その枠へ付け替える</summary>
        private RectTransform _equippedMark;

        private void Awake()
        {
            AddBoxes();

            //枠の箱は場に多めに並べてある。使わない箱が見えると、空きがあるのに拾えないように見える
            for (int i = BoxesFrom; i < transform.childCount; i++)
            {
                InventoryBox box = transform.GetChild(i).GetComponent<InventoryBox>();
                if (box == null) continue;

                if (_boxes.Count < _inventory.SlotCount) _boxes.Add(box);
                else box.gameObject.SetActive(false);
            }

            foreach (InventoryBox box in _boxes) box.Bind(_info);
            LayBacks();
        }

        private void OnEnable()
        {
            _inventory.SlotChanged += Redraw;
            _inventory.Replaced += RedrawAll;
            _inventory.Changed += MarkEquipped;
            _equipment.Changed += MarkEquipped;
        }

        private void OnDisable()
        {
            _inventory.SlotChanged -= Redraw;
            _inventory.Replaced -= RedrawAll;
            _inventory.Changed -= MarkEquipped;
            _equipment.Changed -= MarkEquipped;
        }

        //場の箱がスロット数より少なければ、1 段上の箱を写して下の段に並べる。段の間は、場の 1 段目と 2 段目の間と同じ
        private void AddBoxes()
        {
            int boxes = 0;
            while (BoxesFrom + boxes < transform.childCount && transform.GetChild(BoxesFrom + boxes).GetComponent<InventoryBox>() != null) boxes++;
            if (boxes >= _inventory.SlotCount || boxes < BoxesPerRow * 2) return;

            Vector2 rowStep = Box(BoxesPerRow).anchoredPosition - Box(0).anchoredPosition;
            for (int i = boxes; i < _inventory.SlotCount; i++)
            {
                RectTransform above = Box(i - BoxesPerRow);
                var box = (RectTransform)Instantiate(above.gameObject, transform).transform;
                box.name = above.name;
                box.SetSiblingIndex(BoxesFrom + i);
                box.anchoredPosition = above.anchoredPosition + rowStep;
            }
        }

        private RectTransform Box(int index)
        {
            return (RectTransform)transform.GetChild(BoxesFrom + index);
        }

        //受け皿は箱より前の並びに置き、箱の後ろに描く
        private void LayBacks()
        {
            if (_slotBack == null) return;

            var backs = new GameObject("SlotBacks", typeof(RectTransform)).GetComponent<RectTransform>();
            backs.gameObject.layer = gameObject.layer;
            backs.SetParent(transform, false);
            backs.anchorMin = Vector2.zero;
            backs.anchorMax = Vector2.one;
            backs.offsetMin = Vector2.zero;
            backs.offsetMax = Vector2.zero;
            backs.SetSiblingIndex(BoxesFrom);

            foreach (InventoryBox found in _boxes)
            {
                var box = (RectTransform)found.transform;
                var back = new GameObject("Back", typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
                back.gameObject.layer = gameObject.layer;
                back.SetParent(backs, false);
                back.anchorMin = box.anchorMin;
                back.anchorMax = box.anchorMax;
                back.pivot = box.pivot;
                back.anchoredPosition = box.anchoredPosition;
                back.sizeDelta = box.sizeDelta;
                back.localScale = new Vector3(box.localScale.x * SlotBackScale, box.localScale.y * SlotBackScale, 1f);

                Image image = back.GetComponent<Image>();
                image.sprite = _slotBack;
                image.type = Image.Type.Sliced;
                image.raycastTarget = false;
            }
        }

        private void Redraw(int index, int durability)
        {
            Draw(index, durability, false);
        }

        private void RedrawAll()
        {
            for (int i = 0; i < _boxes.Count; i++) Draw(i, 0, true);
        }

        /// <summary>箱を中身どおりに描く。fresh なら前の品を忘れて描き直す。耐久は保存しないので、描き直すと出ない</summary>
        private void Draw(int index, int durability, bool fresh)
        {
            InventoryBox box = _boxes[index];
            ItemSlot slot = _inventory.Slots[index];

            if (string.IsNullOrEmpty(slot.ItemId))
            {
                box.Clear(_emptySprite);
                return;
            }

            if (fresh || box.ItemId != slot.ItemId)
            {
                box.Clear(_emptySprite);
                box.Ready(slot.ItemId, durability, MaxDurabilityOf(slot.ItemId));
            }

            if (durability == 0) box.ShowStock(slot.Stock);
        }

        private int MaxDurabilityOf(string itemId)
        {
            ItemDefinition item = _items.Items.Find(itemId);

            return item == null ? 0 : item.MaxDurability;
        }

        /// <summary>装備している武器の入った枠に「装備中」の印を付ける。同じ武器が 2 つあれば前の枠に付ける</summary>
        private void MarkEquipped()
        {
            string weapon = _equipment.Equipped.ItemId;
            int index = -1;
            for (int i = 0; i < _boxes.Count && !string.IsNullOrEmpty(weapon); i++)
            {
                if (_inventory.Slots[i].ItemId == weapon)
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                if (_equippedMark != null) _equippedMark.gameObject.SetActive(false);
                return;
            }

            var box = (RectTransform)_boxes[index].transform;
            if (_equippedMark == null) _equippedMark = KeyHint.Badge("EquippedMark", box, "装備中", Vector2.up, EquippedMarkSize);

            _equippedMark.SetParent(box, false);
            _equippedMark.gameObject.SetActive(true);
        }
    }
}
