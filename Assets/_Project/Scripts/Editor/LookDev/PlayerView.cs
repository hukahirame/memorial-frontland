using UnityEngine;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 遊ぶときのカメラの位置関係。プレイヤーの prefab のカメラとそろえる。
    /// 撮る道具が、遊ぶときと同じ見え方で撮るために使う
    /// </summary>
    public static class PlayerView
    {
        /// <summary>見下ろす角度（度）</summary>
        public const float Pitch = 18f;

        public const float FieldOfView = 60f;

        /// <summary>プレイヤーが立つ位置からカメラまでの差</summary>
        public static readonly Vector3 Offset = new Vector3(0f, 1.35f, -2.7f);
    }
}
