using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class EquipmentTests
    {
        private static ItemDefinition Sword(int attack, float knockback = 0f, int maxDurability = 0)
        {
            return new ItemDefinition(itemId: "Ironsword", name: "鉄の剣", maxStock: 1,
                                      maxDurability: maxDurability, installable: true,
                                      attack: attack, knockback: knockback,
                                      use: ItemUse.Equip, heal: 0, description: "");
        }

        private static ItemDefinition Jelly()
        {
            return new ItemDefinition(itemId: "Slimejelly", name: "スライムゼリー", maxStock: 100,
                                      maxDurability: 0, installable: true,
                                      attack: 0, knockback: 0f,
                                      use: ItemUse.Eat, heal: 5, description: "");
        }

        [Test]
        public void 素手の攻撃力は装備前の初期値と同じ()
        {
            Equipment equipment = new Equipment();

            Assert.IsTrue(equipment.IsUnarmed);
            Assert.AreEqual(Equipment.UnarmedAttack, equipment.Attack);
            Assert.AreEqual(0f, equipment.Knockback);
        }

        [Test]
        public void 装備すると原簿の攻撃力になる()
        {
            Equipment equipment = new Equipment();

            Assert.IsTrue(equipment.Equip(Sword(attack: 999, knockback: 7f)));
            Assert.AreEqual("Ironsword", equipment.ItemId);
            Assert.AreEqual(999, equipment.Attack);
            Assert.AreEqual(7f, equipment.Knockback);
            Assert.IsFalse(equipment.IsUnarmed);
        }

        [Test]
        public void 使い道が装備でないものは装備できない()
        {
            Equipment equipment = new Equipment();

            Assert.IsFalse(equipment.Equip(Jelly()));
            Assert.IsFalse(equipment.Equip(null));
            Assert.IsTrue(equipment.IsUnarmed);
            Assert.AreEqual(Equipment.UnarmedAttack, equipment.Attack);
        }

        [Test]
        public void 外すと素手に戻る()
        {
            Equipment equipment = new Equipment();
            equipment.Equip(Sword(attack: 999, knockback: 7f, maxDurability: 3));

            equipment.Unequip();

            Assert.IsTrue(equipment.IsUnarmed);
            Assert.AreEqual(Equipment.UnarmedAttack, equipment.Attack);
            Assert.AreEqual(0f, equipment.Knockback);
            Assert.IsNull(equipment.Durability);
        }

        [Test]
        public void 最大耐久値が0なら耐久を持たず振っても減らない()
        {
            Equipment equipment = new Equipment();
            equipment.Equip(Sword(attack: 40));

            Assert.IsNull(equipment.Durability);
            Assert.IsFalse(equipment.Wear(1));
            Assert.IsFalse(equipment.IsBroken);
        }

        [Test]
        public void 振るたび耐久が減り尽きた一撃だけ壊れたと言う()
        {
            Equipment equipment = new Equipment();
            equipment.Equip(Sword(attack: 40, maxDurability: 2));

            Assert.IsFalse(equipment.Wear(1));
            Assert.AreEqual(1, equipment.Durability.Current);
            Assert.IsFalse(equipment.IsBroken);

            Assert.IsTrue(equipment.Wear(1));
            Assert.IsTrue(equipment.IsBroken);

            Assert.IsFalse(equipment.Wear(1), "壊れたあとに二度目の報せは出ない");
        }

        [Test]
        public void 素手では耐久を消費しない()
        {
            Equipment equipment = new Equipment();

            Assert.IsFalse(equipment.Wear(1));
            Assert.IsFalse(equipment.IsBroken);
        }

        [Test]
        public void 装備を替えると耐久も新しいものになる()
        {
            Equipment equipment = new Equipment();
            equipment.Equip(Sword(attack: 40, maxDurability: 2));
            equipment.Wear(2);

            equipment.Equip(Sword(attack: 999));

            Assert.IsFalse(equipment.IsBroken);
            Assert.IsNull(equipment.Durability);
            Assert.AreEqual(999, equipment.Attack);
        }
    }
}
