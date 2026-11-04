using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 地図の置き場と、場所のシーンとの対応。地図は場所のシーンと同じ名前の .txt で、
    /// 記号の意味は 1 つのパレットにまとめる
    /// </summary>
    public static class FieldMaps
    {
        public const string Folder = "Assets/_Project/Data/Fields";

        public const string PalettePath = Folder + "/FieldPalette.asset";

        private const string SceneFolder = "Assets/_Project/Scenes";

        /// <summary>地図の道。名前の順</summary>
        public static IEnumerable<string> All()
        {
            if (!Directory.Exists(Folder)) return Enumerable.Empty<string>();

            return Directory.GetFiles(Folder, "*.txt")
                            .Select(path => path.Replace('\\', '/'))
                            .OrderBy(path => path, StringComparer.Ordinal);
        }

        /// <summary>その地図が地形を持つ場所のシーン</summary>
        public static string SceneOf(string mapPath)
        {
            return SceneFolder + "/" + Path.GetFileNameWithoutExtension(mapPath) + ".unity";
        }

        public static FieldMap Load(string mapPath)
        {
            return FieldMap.Parse(File.ReadAllText(mapPath));
        }

        public static FieldPalette LoadPalette()
        {
            FieldPalette palette = AssetDatabase.LoadAssetAtPath<FieldPalette>(PalettePath);
            if (palette == null) throw new InvalidOperationException(PalettePath + " が無い");

            return palette;
        }

        /// <summary>
        /// シーンの地形。見えている組み込みの Cube をすべて数える。
        /// 地図は見えている地形だけを持つので、隠してあるものは数えない
        /// </summary>
        public static List<GameObject> Cubes(Scene scene)
        {
            Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
            List<GameObject> found = new List<GameObject>();

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                // 止めたルートから探すと、includeInactive が false でもその下の物が返る
                foreach (MeshFilter filter in root.GetComponentsInChildren<MeshFilter>(false))
                {
                    if (filter.sharedMesh == cube && filter.gameObject.activeInHierarchy) found.Add(filter.gameObject);
                }
            }

            return found;
        }
    }
}
