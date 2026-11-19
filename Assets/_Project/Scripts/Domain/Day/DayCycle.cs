namespace MemorialFloor.Domain
{
    /// <summary>
    /// 1日の経過。零時からの秒だけを持つ。
    /// 1日の長さは Inspector で変えるため、ここには持たず呼ぶ側から渡す。
    /// </summary>
    public sealed class DayCycle
    {
        /// <summary>零時からの経過秒</summary>
        public int ElapsedSeconds { get; private set; }

        /// <summary>ひと刻み前の経過秒。境界を跨いだかの判定に使う</summary>
        public int PreviousSeconds { get; private set; }

        /// <summary>1秒進める</summary>
        public void Advance()
        {
            PreviousSeconds = ElapsedSeconds;
            ElapsedSeconds++;
        }

        /// <summary>零時に戻す</summary>
        public void Reset()
        {
            PreviousSeconds = ElapsedSeconds;
            ElapsedSeconds = 0;
        }

        /// <summary>読み込みなど、途中の値をそのまま入れるとき。負の値は0にする</summary>
        public void SetElapsed(int seconds)
        {
            PreviousSeconds = seconds < 0 ? 0 : seconds;
            ElapsedSeconds = PreviousSeconds;
        }

        public float HourAt(float secondsPerDay)
        {
            return DayClock.HourAt(ElapsedSeconds, secondsPerDay);
        }

        public bool IsDayOver(float secondsPerDay)
        {
            return DayClock.IsDayOver(ElapsedSeconds, secondsPerDay);
        }

        /// <summary>直前の Advance でその時刻の境界を跨いだか</summary>
        public bool Entered(int hour, float secondsPerDay)
        {
            return DayClock.Entered(hour, PreviousSeconds, ElapsedSeconds, secondsPerDay);
        }
    }
}
