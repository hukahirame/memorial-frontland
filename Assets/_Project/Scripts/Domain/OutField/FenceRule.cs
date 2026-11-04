using System;

namespace MemorialFloor.Domain
{
    /// <summary>柵を立てる所。真ん中の位置と、y 軸まわりの向き（度）</summary>
    public readonly struct FenceSpot
    {
        public FenceSpot(float x, float z, float yaw)
        {
            X = x;
            Z = z;
            Yaw = yaw;
        }

        public float X { get; }

        public float Z { get; }

        /// <summary>0 なら x に沿って立ち、90 なら z に沿って立つ</summary>
        public float Yaw { get; }
    }

    /// <summary>
    /// 柵を立てる所の決め方。1 マスの格子の線の上に、2 マスの長さで、いつも奥行きに沿って立てる。
    /// 向いている横の側で、立っている所から clearance より離れた最初の線に立てる。
    /// 端が格子の点にそろうので、並べると端がつながる
    /// </summary>
    public static class FenceRule
    {
        /// <summary>格子の 1 マス。柵の prefab（`Resources/Fence_obj.prefab`）の当たりの長さと絵の大きさは、これに合わせる</summary>
        public const float Cell = 0.75f;

        /// <summary>柵 1 枚の長さ。真ん中の格子の点から両側へ 1 マスずつ伸びる</summary>
        public const float Length = 2f * Cell;

        /// <param name="side">向いている横の側。左なら負、ほかは右</param>
        /// <param name="clearance">立っている所から柵の線まで、最低限あける距離</param>
        public static FenceSpot Spot(float x, float z, float side, float clearance)
        {
            return new FenceSpot(Line(x, side < 0f ? -1f : 1f, clearance), Nearest(z), 90f);
        }

        /// <summary>at から toward の向きへ、clearance より離れた最初の格子の線</summary>
        private static float Line(float at, float toward, float clearance)
        {
            return toward > 0f
                ? (float)Math.Ceiling((at + clearance) / Cell) * Cell
                : (float)Math.Floor((at - clearance) / Cell) * Cell;
        }

        /// <summary>at に最も近い格子の点。ちょうど真ん中なら大きい側</summary>
        private static float Nearest(float at)
        {
            return (float)Math.Floor(at / Cell + 0.5f) * Cell;
        }
    }
}
