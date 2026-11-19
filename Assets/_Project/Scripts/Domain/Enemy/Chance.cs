using System;

namespace MemorialFloor.Domain
{
    /// <summary>確率の判定</summary>
    public static class Chance
    {
        /// <summary>percent% で真。0 以下は常に偽、100 以上は常に真</summary>
        public static bool Roll(int percent, IRandom random)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;

            return random.Next(0, 100) < percent;
        }
    }
}
