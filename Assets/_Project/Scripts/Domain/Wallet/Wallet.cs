using System;

namespace MemorialFloor.Domain
{
    /// <summary>所持金。表示ではなくここが正典</summary>
    public sealed class Wallet
    {
        /// <summary>いま持っている額。負にはならない</summary>
        public int Amount { get; private set; }

        /// <summary>額が変わった。表示はこれを受けて書き直す</summary>
        public event Action Changed;

        /// <summary>受け取る。負の量は何もしない</summary>
        public void Add(int amount)
        {
            if (amount <= 0) return;

            Set(Amount + amount);
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

            Set(Amount - amount);
            return true;
        }

        /// <summary>初期化と読み込み。負の値は0にする</summary>
        public void SetAmount(int amount)
        {
            Set(amount < 0 ? 0 : amount);
        }

        private void Set(int amount)
        {
            if (Amount == amount) return;

            Amount = amount;
            Changed?.Invoke();
        }
    }
}
