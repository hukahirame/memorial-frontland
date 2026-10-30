using System;

namespace MemorialFloor.Domain
{
    /// <summary>進む向き。長さは 1 か 0</summary>
    public readonly struct Heading
    {
        public Heading(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }

        public bool IsStill
        {
            get { return X == 0f && Z == 0f; }
        }
    }

    /// <summary>移動スティックの倒し方から進む向きを決める</summary>
    public static class MoveRule
    {
        /// <summary>これより小さく倒したときは、倒していないとみなす</summary>
        public const float DeadZone = 0.2f;

        /// <summary>遊びを越えて倒していれば、倒した向きへ全速。越えていなければ止まる</summary>
        public static Heading Direction(float horizontal, float vertical)
        {
            float length = (float)Math.Sqrt(horizontal * horizontal + vertical * vertical);
            if (length < DeadZone) return new Heading(0f, 0f);

            return new Heading(horizontal / length, vertical / length);
        }
    }
}
