using MemorialFloor.Domain;

namespace MemorialFloor.Game
{
    /// <summary>UnityEngine.Random を Domain の口に合わせる</summary>
    public sealed class UnityRandom : IRandom
    {
        /// <summary>使い回す実体。乱数に状態を持たせないので共有してよい</summary>
        public static readonly UnityRandom Shared = new UnityRandom();

        public int Next(int minInclusive, int maxExclusive)
        {
            return UnityEngine.Random.Range(minInclusive, maxExclusive);
        }

        public float NextFloat(float min, float max)
        {
            return UnityEngine.Random.Range(min, max);
        }
    }
}
