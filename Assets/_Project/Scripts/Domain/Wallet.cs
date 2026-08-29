namespace MemorialFloor.Domain
{
    /// <summary>所持金。表示ではなくここが正典</summary>
    public sealed class Wallet
    {
        /// <summary>いま持っている額。負にはならない</summary>
        public int Amount { get; private set; }

        /// <summary>受け取る。負の量は何もしない</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;

            Amount += amount;
        }

        /// <summary>払えるか。ちょうど同額なら払える</summary>
        public bool CanAfford(int amount)
        {
            return amount >= 0 && Amount >= amount;
        }

        /// <summary>払う。足りなければ何もせず false。半端に減った状態は作らない</summary>
        public bool Spend(int amount)
        {
            if (!CanAfford(amount)) return false;

            Amount -= amount;
            return true;
        }

        /// <summary>初期化と読み込み。負の値は0にする</summary>
        public void SetAmount(int amount)
        {
            Amount = amount < 0 ? 0 : amount;
        }
    }
}
