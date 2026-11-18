using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// プレイヤーの持ち物。枠の並びを持ち、品を入れて出す。持っているだけで効く品（速さの首飾り、攻撃の品）の効き目もここで決める。
    /// 枠の絵は InventoryView が知らせを受けて描く
    /// </summary>
    public sealed class PlayerInventory : MonoBehaviour
    {
        /// <summary>速さの首飾り 1 つで足す歩く速さ。持っているだけで効く</summary>
        private const float NecklaceSpeed = 0.3f;

        private const string NecklaceId = "Speedneckless";

        /// <summary>始めに持つ武器。手に持って始まる</summary>
        private const string StartingWeaponId = "Ironsword";

        /// <summary>始めに持つ材料とその数</summary>
        private const string StartingMaterialId = "Branch";
        private const int StartingMaterials = 14;

        [Tooltip("スロット数。起動時にこの数まで空きスロットを用意する")]
        [SerializeField, FormerlySerializedAs("slotCount")] private int _slotCount = 15;

        [SerializeField, FormerlySerializedAs("movement")] private PlayerMovement _movement;
        [SerializeField, FormerlySerializedAs("items")] private ItemBook _items;
        [SerializeField, FormerlySerializedAs("equipment")] private PlayerEquipment _equipment;
        [SerializeField, FormerlySerializedAs("slots")] private List<ItemSlot> _slots = new List<ItemSlot>();

        /// <summary>枠 1 つの中身が変わった。2 つ目は入ってきた品の耐久で、耐久を持たない品と、入ってきたのでないときは 0</summary>
        public event Action<int, int> SlotChanged;

        /// <summary>中身をまとめて入れ替えた</summary>
        public event Action Replaced;

        /// <summary>中身が変わった。どの変わり方でも 1 回</summary>
        public event Action Changed;

        public int SlotCount
        {
            get { return _slotCount; }
        }

        /// <summary>保存と復元のための口。並びは書き換えず、中身を入れ替える</summary>
        public IReadOnlyList<ItemSlot> Slots
        {
            get { return _slots; }
        }

        /// <summary>格納規則。並びを直接書き換える</summary>
        public Inventory Inventory
        {
            get { return new Inventory(_slots); }
        }

        /// <summary>持っているだけで足される攻撃力。装備しない品の原簿の攻撃力を、持っている数だけ足す</summary>
        public int HeldAttack
        {
            get
            {
                int total = 0;
                foreach (ItemSlot slot in _slots)
                {
                    ItemDefinition item = _items.Items.Find(slot.ItemId);
                    if (item != null && item.Use != ItemUse.Equip) total += item.Attack * slot.Stock;
                }

                return total;
            }
        }

        private void Awake()
        {
            while (_slots.Count < _slotCount) _slots.Add(new ItemSlot());
        }

        private void Start()
        {
            for (int i = 0; i < StartingMaterials; i++) LoadInventory(StartingMaterialId, 0);
            LoadInventory(StartingWeaponId, 0);

            //手の絵はシーンで鉄の剣にしてあり、素手のままだと絵と強さが食い違う
            _equipment.Equip(_items.Items.Find(StartingWeaponId));
        }

        /// <summary>読み込んだ内容で埋め直す。参照は差し替えない。首飾りの加速も中身に合わせ直す</summary>
        public void ReplaceSlots(List<ItemSlot> loaded)
        {
            _slots.Clear();
            if (loaded != null) _slots.AddRange(loaded);

            while (_slots.Count < _slotCount) _slots.Add(new ItemSlot());

            ApplySpeed();
            Replaced?.Invoke();
            Changed?.Invoke();
        }

        /// <summary>持ち物にある武器を持つ。持っていない品と装備できない品は持たない</summary>
        public void Wield(string itemId)
        {
            if (Inventory.CountOf(itemId) <= 0) return;

            _equipment.Equip(_items.Items.Find(itemId));
        }

        /// <summary>1 つ入れる。入れば 1、入る枠が無ければ -1</summary>
        public int LoadInventory(string itemId, int durability)
        {
            AddResult result = Inventory.Add(itemId, () => MaxStockOf(itemId)); //新規配置時のみ評価される
            if (result.Outcome == AddOutcome.NoSpace) return -1;

            //所持しているだけで加速する。速さは入ったあとの数から決めるので、入らずに地面に残った分は数えない
            if (itemId == NecklaceId) ApplySpeed();

            SlotChanged?.Invoke(result.SlotIndex, result.Outcome == AddOutcome.Placed ? durability : 0);
            Changed?.Invoke();
            return 1;
        }

        /// <summary>1 つ出す。持っていなければ何もしない</summary>
        public void UnloadInventory(string itemId)
        {
            RemoveResult result = Inventory.Remove(itemId);
            if (result.Outcome == RemoveOutcome.NotFound) return;

            if (itemId == NecklaceId) ApplySpeed();

            SlotChanged?.Invoke(result.SlotIndex, 0);
            Changed?.Invoke();
        }

        /// <summary>歩く速さを、持っている速さの首飾りの数に合わせる</summary>
        private void ApplySpeed()
        {
            _movement.Speed = PlayerMovement.BaseSpeed + Inventory.CountOf(NecklaceId) * NecklaceSpeed;
        }

        private int MaxStockOf(string itemId)
        {
            ItemDefinition item = _items.Items.Find(itemId);

            return item == null ? 0 : item.MaxStock;
        }
    }
}
