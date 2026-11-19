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
}
