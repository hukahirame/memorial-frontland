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
        public static FieldBounds ToBounds(Vector4 exposition)
        {
            return new FieldBounds(exposition.x, exposition.y, exposition.z, exposition.w);
        }
    }
}
