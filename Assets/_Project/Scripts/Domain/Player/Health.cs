namespace MemorialFloor.Domain
{
    /// <summary>体力。表示ではなくここが正典</summary>
    public sealed class Health
    {
        /// <summary>上限。0 のうちは未設定</summary>
        public int Max { get; private set; }

        /// <summary>いまの体力。0 と Max の間に収まる</summary>
        public int Current { get; private set; }

        public bool IsDead
        {
            get { return Current <= 0; }
        }

        /// <summary>上限が決まっているか。初期化を1度だけにするために見る</summary>
        public bool IsReady
        {
            get { return Max > 0; }
        }

        /// <summary>上限を決める。現在値も上限へ切り詰める。負と0は1にする</summary>
        public void SetMax(int max)
        {
            Max = max < 1 ? 1 : max;
            Current = Clamp(Current);
        }

        /// <summary>読み込みと初期化。0 と Max の間に収める</summary>
        public void SetCurrent(int value)
        {
            Current = Clamp(value);
        }

        /// <summary>受けた損害のぶん減らす。0 より下にはならない。負の量は何もしない</summary>
        public void Take(int damage)
        {
            if (damage <= 0) return;

            Current = Clamp(Current - damage);
        }

        /// <summary>回復する。上限で頭打ち。死んでいるなら何もしない。負の量も何もしない</summary>
        public void Heal(int amount)
        {
            if (amount <= 0 || IsDead) return;

            Current = Clamp(Current + amount);
        }

        private int Clamp(int value)
        {
            if (value < 0) return 0;

            return value > Max ? Max : value;
        }
    }
}
