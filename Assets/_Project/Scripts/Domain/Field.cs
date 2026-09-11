using System;

namespace MemorialFloor.Domain
{
    /// <summary>場の外へ出たときに押し戻す向き。大きさは持たず、符号だけが意味を持つ</summary>
    public readonly struct Nudge
    {
        public Nudge(float x, float z)
        {
            X = x;
            Z = z;
        }

        public float X { get; }

        public float Z { get; }

        public bool None
        {
            get { return X == 0f && Z == 0f; }
        }
    }

    /// <summary>
    /// 場の範囲。シーンは Vector4 に XL, XS, ZL, ZS の順で詰めており、
    /// 成分の意味が呼ぶ側に散らばっていた。名前を付けるためにこの型を通す。
    /// </summary>
    public sealed class FieldBounds
    {
        /// <summary>押し戻しでは間に合わない距離。これだけ外へ出たら連れ戻す</summary>
        public const float EscapeMargin = 0.4f;

        /// <summary>上下にこれだけ離れたら連れ戻す</summary>
        public const float EscapeHeight = 4f;

        public FieldBounds(float maxX, float minX, float maxZ, float minZ)
        {
            MaxX = maxX;
            MinX = minX;
            MaxZ = maxZ;
            MinZ = minZ;
        }

        public float MaxX { get; }

        public float MinX { get; }

        public float MaxZ { get; }

        public float MinZ { get; }

        public bool Contains(float x, float z)
        {
            return x <= MaxX && x >= MinX && z <= MaxZ && z >= MinZ;
        }

        public float ClampX(float x)
        {
            return x > MaxX ? MaxX : x < MinX ? MinX : x;
        }

        public float ClampZ(float z)
        {
            return z > MaxZ ? MaxZ : z < MinZ ? MinZ : z;
        }

        /// <summary>外へ出ている分を戻す向き。中に居れば None</summary>
        public Nudge PushBack(float x, float z)
        {
            float toward = x > MaxX ? -1f : x < MinX ? 1f : 0f;
            float depth = z > MaxZ ? -1f : z < MinZ ? 1f : 0f;

            return new Nudge(toward, depth);
        }

        /// <summary>押し戻しでは戻らないほど離れたか。高さも見る</summary>
        public bool HasEscaped(float x, float y, float z)
        {
            if (Math.Abs(y) > EscapeHeight) return true;

            return x > MaxX + EscapeMargin || x < MinX - EscapeMargin
                || z > MaxZ + EscapeMargin || z < MinZ - EscapeMargin;
        }
    }
}
