using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 1日の時刻の規則。経過秒と1日の長さだけで決まる。
    /// 区間で続くもの（明るさの補間）と、境界で1度だけ起きるもの（Entered）を分けている。
    /// </summary>
    public static class DayClock
    {
        public const int HoursPerDay = 24;

        /// <summary>夜明けの開始。ここから MorningHour まで明るさが上がる</summary>
        public const int SunriseHour = 4;

        /// <summary>夜明けの終了。デイリー更新はこの境界で1度だけ</summary>
        public const int MorningHour = 5;

        /// <summary>日没の開始。ここから NightHour まで明るさが下がる</summary>
        public const int SunsetHour = 16;

        /// <summary>日没の終了</summary>
        public const int NightHour = 17;

        /// <summary>いま何時か。0〜24 の実数。0除算を避けるため長さ0以下は0時を返す</summary>
        public static float HourAt(int elapsedSeconds, float secondsPerDay)
        {
            if (secondsPerDay <= 0f) return 0f;

            return elapsedSeconds / (secondsPerDay / HoursPerDay);
        }

        /// <summary>零時を回ったか</summary>
        public static bool IsDayOver(int elapsedSeconds, float secondsPerDay)
        {
            return elapsedSeconds >= secondsPerDay;
        }

        /// <summary>
        /// その時刻の境界を今のひと刻みで跨いだか。1日に1度だけ真になる。
        /// 「その時刻台にいるか」ではない。1時間は複数の刻みにまたがる
        /// </summary>
        public static bool Entered(int hour, int previousSeconds, int currentSeconds, float secondsPerDay)
        {
            return HourAt(previousSeconds, secondsPerDay) < hour
                && HourAt(currentSeconds, secondsPerDay) >= hour;
        }

        /// <summary>その時刻の明るさ。夜明けと日没は区間の中で線形に動く</summary>
        public static float LightIntensity(float hour, float min, float max)
        {
            if (hour < SunriseHour) return min;

            if (hour < MorningHour) return min + (max - min) * (hour - SunriseHour);

            if (hour < SunsetHour) return max;

            if (hour < NightHour) return max - (max - min) * (hour - SunsetHour);

            return min;
        }
    }

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

    /// <summary>
    /// 調査クエストの発行計画。根源ごとに、1度保留してから発行する。
    /// 保留を挟むぶん、発行は次の呼び出しまで遅れる。
    /// </summary>
    public sealed class DayPlan
    {
        private readonly List<string> _pending = new List<string>();

        /// <summary>発行を待っている根源 Id</summary>
        public IReadOnlyList<string> Pending
        {
            get { return _pending; }
        }

        public int Count
        {
            get { return _pending.Count; }
        }

        /// <summary>
        /// いま発行してよいか。予約済みなら予約を外して true。
        /// 未予約なら予約だけして false を返す
        /// </summary>
        public bool ShouldIssue(string rootId)
        {
            if (string.IsNullOrEmpty(rootId)) return false;

            if (_pending.Remove(rootId)) return true;

            _pending.Add(rootId);
            return false;
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _pending.Clear();
        }
    }
}
