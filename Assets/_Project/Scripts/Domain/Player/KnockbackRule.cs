using System;

namespace MemorialFloor.Domain
{
    /// <summary>当てた相手を飛ばす速さ</summary>
    public readonly struct Push
    {
        public Push(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        /// <summary>浮かせる上向きの成分</summary>
        public float Y { get; }

        public float Z { get; }
    }

    /// <summary>吹き飛ばし。当てた側から相手へ向かう水平の向きに飛ばし、少し浮かせる</summary>
    public static class KnockbackRule
    {
        /// <summary>浮かせる強さ。水平の強さに対する割合</summary>
        public const float LiftRatio = 1f / 3f;

        /// <summary>これより近いと向きが決まらない</summary>
        private const float MinDistance = 0.0001f;

        /// <summary>
        /// strength の強さで飛ばす速さ。重なっていて向きが決まらないときは、当てた側の向きへ飛ばす。
        /// facing は当てた側の向き。正なら右、負なら左
        /// </summary>
        public static Push Velocity(float fromX, float fromZ, float toX, float toZ, float strength, float facing)
        {
            if (strength <= 0f) return new Push(0f, 0f, 0f);

            float dx = toX - fromX;
            float dz = toZ - fromZ;
            float length = (float)Math.Sqrt(dx * dx + dz * dz);

            if (length < MinDistance)
            {
                dx = facing < 0f ? -1f : 1f;
                dz = 0f;
                length = 1f;
            }

            return new Push(dx / length * strength, strength * LiftRatio, dz / length * strength);
        }
    }
}
