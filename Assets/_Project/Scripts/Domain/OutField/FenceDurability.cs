using System;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 木の柵の耐久。削られると減り、しばらく削られなければ時間で戻る。尽きると倒れて通れるようになり、
    /// 上限の RiseRatio を超えるまで戻ると起き上がる。倒れているあいだは削られない
    /// </summary>
    public sealed class FenceDurability
    {
        /// <summary>最後に削られてから、戻り始めるまでの秒数</summary>
        public const float RepairDelay = 3f;

        /// <summary>1 秒に戻る耐久</summary>
        public const float RepairPerSecond = 1f;

        /// <summary>倒れた柵が起き上がる耐久。上限に対する割合で、これを超えると起き上がる</summary>
        public const float RiseRatio = 0.25f;

        private float _current;
        private float _struckAt = float.NegativeInfinity;

        /// <summary>満タンで始める。max が 1 より小さければ 1 にする</summary>
        public FenceDurability(int max)
        {
            Max = max < 1 ? 1 : max;
            _current = Max;
        }

        public int Max { get; }

        /// <summary>残りの耐久。戻りかけの端数は切り捨てる</summary>
        public int Current
        {
            get { return (int)_current; }
        }

        /// <summary>残りの割合。0〜1</summary>
        public float Ratio
        {
            get { return _current / Max; }
        }

        /// <summary>尽きて倒れ、まだ起き上がっていない</summary>
        public bool IsFallen { get; private set; }

        /// <summary>
        /// 控えから戻す。awaySeconds は控えてから戻すまでに経った秒数で、そのぶん戻した耐久で始める
        /// （控えた時点で削られてから RepairDelay 経っていないとみなす）。倒れていた柵は、起き上がる耐久を超えていれば立って始まる
        /// </summary>
        public void Restore(int current, bool fallen, float awaySeconds)
        {
            float repaired = RepairPerSecond * Math.Max(0f, awaySeconds - RepairDelay);
            _current = Math.Max(0f, Math.Min(Max, current + repaired));
            _struckAt = float.NegativeInfinity;
            IsFallen = _current <= 0f || (fallen && !IsRisen());
        }

        /// <summary>now に amount 削る。倒れているあいだと、0 以下の量は何もしない。尽きて倒れたら true</summary>
        public bool Strike(int amount, float now)
        {
            if (IsFallen || amount <= 0) return false;

            _current = Math.Max(0f, _current - amount);
            _struckAt = now;
            if (_current > 0f) return false;

            IsFallen = true;
            return true;
        }

        /// <summary>
        /// 直前から seconds 経って now になった。最後に削られてから RepairDelay 経っていれば、そのぶん戻す。
        /// 倒れていて起き上がる耐久を超えるまで戻ったら起き上がって true
        /// </summary>
        public bool Repair(float now, float seconds)
        {
            if (_current >= Max || seconds <= 0f || now - _struckAt < RepairDelay) return false;

            _current = Math.Min(Max, _current + RepairPerSecond * seconds);
            if (!IsFallen || !IsRisen()) return false;

            IsFallen = false;
            return true;
        }

        private bool IsRisen()
        {
            return _current > Max * RiseRatio;
        }
    }
}
