using System;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 乱数の口。UnityEngine.Random を直に呼ぶと規則を試験できないため、
    /// 差し替えられる形にする。
    /// </summary>
    public interface IRandom
    {
        /// <summary>min 以上 max 未満の整数</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>min 以上 max 以下の小数。整数側と違い上限を含む</summary>
        float NextFloat(float min, float max);
    }
}
