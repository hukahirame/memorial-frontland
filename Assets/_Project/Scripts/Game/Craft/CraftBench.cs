using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>作業台。作った品と買った品は、いま居る場所の作業台の上に落ちる（Workshop）</summary>
    public sealed class CraftBench : MonoBehaviour
    {
        /// <summary>できた品を落とす、作業台からの高さ</summary>
        private const float DropHeight = 0.5f;

        /// <summary>できた品を落とす位置</summary>
        public Vector3 DropPoint => transform.position + Vector3.up * DropHeight;
    }
}
