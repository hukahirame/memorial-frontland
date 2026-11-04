using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Editor
{
    /// <summary>地図から地形の Cube を組む</summary>
    public static class FieldBuilder
    {
        /// <summary>
        /// 地図の Cube をすべて parent の子として作る。地図の座標は parent から見た位置なので、
        /// 世界の座標どおりに置くなら parent を原点に置き、回さず、大きさ 1 のままにする。
        /// 記号がパレットに無ければ、何も作らずに InvalidOperationException を投げる
        /// </summary>
        public static List<GameObject> Build(FieldMap map, FieldPalette palette, Transform parent)
        {
            List<FieldPaletteEntry> entries = new List<FieldPaletteEntry>();

            foreach (FieldBlock block in map.Blocks)
            {
                FieldPaletteEntry entry = palette.Find(block.Letter);
                if (entry == null || entry.material == null)
                    throw new InvalidOperationException(
                        block.Line + " 行目: 記号 " + block.Letter + " がパレットに無いか、素材が空");

                entries.Add(entry);
            }

            List<GameObject> built = new List<GameObject>();

            for (int i = 0; i < map.Blocks.Count; i++)
            {
                built.Add(Place(map.Blocks[i], entries[i], map.Cell, parent));
            }

            return built;
        }

        private static GameObject Place(FieldBlock block, FieldPaletteEntry entry, float cell, Transform parent)
        {
            // 作った直後は開いているシーンに居る。組む先のシーンへ移してから親に付ける
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            SceneManager.MoveGameObjectToScene(cube, parent.gameObject.scene);
            cube.transform.SetParent(parent, false);

            // 視界確保（ViewSecurer）は名前に Cube を含むものだけを薄くする
            cube.name = Name(block);

            cube.transform.localPosition = new Vector3(block.X, block.Y, block.Z);
            cube.transform.localRotation = Quaternion.Euler(block.RotationX, block.RotationY, block.RotationZ);
            cube.transform.localScale = Vector3.one * cell;

            cube.GetComponent<MeshRenderer>().sharedMaterial = entry.material;

            BoxCollider box = cube.GetComponent<BoxCollider>();
            box.center = entry.colliderCenter;
            box.size = entry.colliderSize;

            return cube;
        }

        /// <summary>地図のどこに書いた Cube かが分かる名前。マスなら行と列、マスに乗らなければ地図の行</summary>
        private static string Name(FieldBlock block)
        {
            return block.Row < 0
                ? "Cube_" + block.Letter + " line " + block.Line
                : "Cube_" + block.Letter + " " + block.Row + "-" + block.Column;
        }
    }
}
