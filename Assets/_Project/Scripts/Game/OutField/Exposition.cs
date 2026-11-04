using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// シーンは場の範囲を Vector4 に XL, XS, ZL, ZS の順で詰めて持っている。
    /// authoring 済みのデータを触らずに済ませるため、読むときにここで名前へ移す。
    /// </summary>
    public static class Exposition
    {
        /// <param name="ceiling">これより高く上がったら場の外とみなす高さ。場所（Area）が持つ</param>
        public static FieldBounds ToBounds(Vector4 exposition, float ceiling = FieldBounds.EscapeHeight)
        {
            return new FieldBounds(exposition.x, exposition.y, exposition.z, exposition.w, ceiling);
        }
    }
}
