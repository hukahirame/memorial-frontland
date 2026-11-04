using System;
using System.Collections.Generic;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>地図の記号 1 つ。Inspector で編集する</summary>
    [Serializable]
    public class FieldPaletteEntry
    {
        [Tooltip("地図に書く 1 文字。. は何も置かない印なので使えない")]
        public string letter;

        [Tooltip("何を表すか。地図を書く人が読む")]
        public string description;

        public Material material;

        [Tooltip("当たりの中心。Cube の一辺を 1 とした値")]
        public Vector3 colliderCenter = Vector3.zero;

        [Tooltip("当たりの大きさ。Cube の一辺を 1 とした値")]
        public Vector3 colliderSize = Vector3.one;
    }

    /// <summary>
    /// 地図の記号と、それが置く Cube の素材と当たり。Project ウィンドウの Create メニューから作る。
    /// 実行中に書き換えないこと（ScriptableObject への変更はエディタ上で永続化される）。
    /// </summary>
    [CreateAssetMenu(fileName = "FieldPalette", menuName = "MemorialFloor/Field Palette")]
    public class FieldPalette : ScriptableObject
    {
        public List<FieldPaletteEntry> entries = new List<FieldPaletteEntry>();

        /// <summary>その記号の行。無ければ null</summary>
        public FieldPaletteEntry Find(char letter)
        {
            foreach (FieldPaletteEntry entry in entries)
            {
                if (entry != null && entry.letter != null && entry.letter.Length == 1 && entry.letter[0] == letter)
                    return entry;
            }

            return null;
        }
    }
}
