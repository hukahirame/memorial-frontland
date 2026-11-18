using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>プレイヤーが手に持っている武器と、手の絵。プレイヤーの一番上に置く</summary>
    public sealed class PlayerEquipment : MonoBehaviour
    {
        /// <summary>手に持つ絵。原簿には置けないので、品の ID と対で持つ</summary>
        [Serializable]
        public sealed class HandSprite
        {
            public string ItemId;
            public Sprite Sprite;
        }

        [Tooltip("手に持った武器の絵を出すところ")]
        [SerializeField] private SpriteRenderer _hand;

        [SerializeField] private List<HandSprite> _handSprites = new List<HandSprite>();

        public Equipment Equipped { get; } = new Equipment();

        /// <summary>持つ武器が替わった</summary>
        public event Action Changed;

        /// <summary>武器を持つ。手の絵も替える。装備できない品なら何もしない</summary>
        public void Equip(ItemDefinition item)
        {
            if (!Equipped.Equip(item)) return;

            HandSprite inHand = _handSprites.Find(pair => pair.ItemId == item.ItemId);
            if (inHand != null && _hand != null) _hand.sprite = inHand.Sprite;

            Changed?.Invoke();
        }

        /// <summary>素手に戻す。手の絵はそのまま</summary>
        public void Unequip()
        {
            Equipped.Unequip();
            Changed?.Invoke();
        }
    }
}
