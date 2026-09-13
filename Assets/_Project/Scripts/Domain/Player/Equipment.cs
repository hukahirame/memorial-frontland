namespace MemorialFloor.Domain
{
    /// <summary>
    /// 装備している武器。攻撃力と吹き飛ばしと耐久を持つ。表示ではなくここが正典。
    /// 素性は原簿から取る。装備の切り替えはこの1つを差し替えて表す。
    /// </summary>
    public sealed class Equipment
    {
        /// <summary>
        /// 何も装備していないときの攻撃力。
        /// Weapon.power の static 初期値 40 がそのまま効いていた値
        /// </summary>
        public const int UnarmedAttack = 40;

        /// <summary>装備しているものの ID。素手なら空文字</summary>
        public string ItemId { get; private set; }

        /// <summary>いま振っているものの攻撃力</summary>
        public int Attack { get; private set; }

        /// <summary>当たった相手を吹き飛ばす強さ。素手では 0</summary>
        public float Knockback { get; private set; }

        /// <summary>耐久。耐久の概念を持たない武器と素手では null</summary>
        public Health Durability { get; private set; }

        public Equipment()
        {
            Unequip();
        }

        public bool IsUnarmed
        {
            get { return string.IsNullOrEmpty(ItemId); }
        }

        /// <summary>耐久が尽きたか。耐久を持たないものは壊れない</summary>
        public bool IsBroken
        {
            get { return Durability != null && Durability.IsDead; }
        }

        /// <summary>
        /// 装備する。使い道が装備でないものは装備できず、何も起きない。
        /// 最大耐久値が 0 の武器は耐久を持たず、振っても減らない
        /// </summary>
        public bool Equip(ItemDefinition item)
        {
            if (item == null || item.Use != ItemUse.Equip) return false;

            ItemId = item.ItemId;
            Attack = item.Attack;
            Knockback = item.Knockback;
            Durability = null;

            if (item.MaxDurability > 0)
            {
                Durability = new Health();
                Durability.SetMax(item.MaxDurability);
                Durability.SetCurrent(item.MaxDurability);
            }

            return true;
        }

        /// <summary>素手に戻す</summary>
        public void Unequip()
        {
            ItemId = string.Empty;
            Attack = UnarmedAttack;
            Knockback = 0f;
            Durability = null;
        }

        /// <summary>
        /// 1回ぶん使う。耐久を持たないものは減らない。
        /// この一撃で尽きたときだけ真を返す
        /// </summary>
        public bool Wear(int amount)
        {
            if (Durability == null || Durability.IsDead) return false;

            Durability.Take(amount);

            return Durability.IsDead;
        }
    }
}
