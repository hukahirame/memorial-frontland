using System;
using System.Collections.Generic;
using MemorialFloor.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 場所を 1 つの世界の座標に並べ、隣どうしを地続きにする（[D-42]）。
    /// 場所は手前から奥へ横一列に並べ、隣の場の範囲の端どうしを合わせる。場所の物はまとめてずらし、ずらした量を Area の原点に書く。
    /// 境目の向こうへはみ出した物は片付ける。床や壁の箱は境目で切り、飾りは境目を越えた分が多ければ消す。
    /// 後ろの地形は場所ごとに写しを作り、境目の向こうに穴を開ける。何度回しても同じ結果になる
    /// </summary>
    public static class WorldLayout
    {
        private const string SceneFolder = "Assets/_Project/Scenes/";

        /// <summary>場所の並び。手前から奥へ。拠点、根源の順で、RootsManager の根源の並びと同じ</summary>
        private static readonly string[] Line = { "MainSite", "Root1", "Root2", "Root3" };

        /// <summary>後ろの地形の写しを置く所</summary>
        private const string TerrainFolder = "Assets/_Project/Art/Places";

        /// <summary>飾りが境目の向こうへこれより大きくはみ出したら消す。隣の場所の物に食い込んで見える</summary>
        private const float Overhang = 0.3f;

        /// <summary>境目の判定の遊び。端がちょうど境目にある物を、はみ出しとみなさない</summary>
        private const float Slack = 0.01f;

        /// <summary>片付けてよい部品。これ以外の部品（遊ぶための部品や素材集のスクリプト）を持つ物には手を付けない</summary>
        private static readonly Type[] Plain =
        {
            typeof(Transform), typeof(MeshFilter), typeof(MeshRenderer), typeof(SpriteRenderer), typeof(BillboardRenderer),
            typeof(BoxCollider), typeof(MeshCollider), typeof(SphereCollider), typeof(CapsuleCollider), typeof(LODGroup),
        };

        /// <summary>境目で切ってよい形。Unity の基本の箱と板</summary>
        private static readonly string[] Boxes = { "Cube", "Plane", "Quad" };

        [MenuItem("Tools/World/Lay Out Places")]
        public static void LayOutAll()
        {
            for (int i = 0; i < Line.Length; i++) LayOut(i);
        }

        /// <summary>1 つの場所だけ並べ直す。場所を組む道具が、組み直したあとに呼ぶ。手前の場所は並べてある前提</summary>
        public static void LayOut(string sceneName)
        {
            int at = Array.IndexOf(Line, sceneName);
            if (at < 0) throw new ArgumentException(sceneName + " は場所の並びに無い", nameof(sceneName));

            LayOut(at);
        }

        private static void LayOut(int at)
        {
            Vector3 origin = OriginOf(at);
            bool hasPrevious = at > 0;
            bool hasNext = at < Line.Length - 1;

            PlaceLooks.Edit(PathOf(Line[at]), scene =>
            {
                Area area = AreaOf(scene);
                Shift(scene, area, origin);

                Vector4 bounds = area.Exposition;
                float? left = hasPrevious ? bounds.y : (float?)null;
                float? right = hasNext ? bounds.x : (float?)null;

                Physics.SyncTransforms();
                Trim(scene, left, right);
                Terrains(scene, left, right);

                Debug.Log(scene.name + " を " + origin + " に置いた。場の範囲 x " + bounds.y + "〜" + bounds.x);
            });
        }

        /// <summary>場所の原点。拠点は世界の原点に置き、奥の場所は、場の範囲の手前の端を手前の場所の奥の端に合わせる</summary>
        private static Vector3 OriginOf(int at)
        {
            Vector3 origin = Vector3.zero;

            for (int i = 1; i <= at; i++)
            {
                Vector4 previous = LocalExposition(Line[i - 1]);
                Vector4 next = LocalExposition(Line[i]);
                origin.x += previous.x - next.y;
            }

            return origin;
        }

        /// <summary>場の範囲を、場所の原点からの位置で読む。シーンは開かずに読む</summary>
        private static Vector4 LocalExposition(string sceneName)
        {
            Scene scene = EditorSceneManager.OpenPreviewScene(PathOf(sceneName));

            try
            {
                return new SerializedObject(AreaOf(scene)).FindProperty("exposition").vector4Value;
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        /// <summary>場所の物をすべて、今の原点から新しい原点へずらす</summary>
        private static void Shift(Scene scene, Area area, Vector3 origin)
        {
            Vector3 delta = origin - area.Origin;
            if (delta == Vector3.zero) return;

            foreach (GameObject root in scene.GetRootGameObjects()) root.transform.position += delta;

            SerializedObject place = new SerializedObject(area);
            place.FindProperty("origin").vector3Value = origin;
            place.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>境目（left より手前、right より奥）の向こうへはみ出した物を片付ける。深い物から順に見る</summary>
        private static void Trim(Scene scene, float? left, float? right)
        {
            if (left == null && right == null) return;

            List<Transform> all = new List<Transform>();
            foreach (GameObject root in scene.GetRootGameObjects()) all.AddRange(root.GetComponentsInChildren<Transform>(false));
            all.Reverse();

            int removed = 0, cut = 0;
            foreach (Transform shown in all)
            {
                if (shown == null || !IsPlain(shown.gameObject)) continue;

                Bounds? own = OwnBounds(shown.gameObject);
                if (own == null) continue;

                Bounds bounds = own.Value;
                float keepFrom = left ?? float.NegativeInfinity;
                float keepTo = right ?? float.PositiveInfinity;

                if (bounds.min.x >= keepFrom - Slack && bounds.max.x <= keepTo + Slack) continue;

                bool beyond = bounds.max.x <= keepFrom + Slack || bounds.min.x >= keepTo - Slack;
                if (!beyond && IsBox(shown))
                {
                    Cut(shown, bounds, keepFrom, keepTo);
                    cut++;
                    continue;
                }

                float overhang = Mathf.Max(keepFrom - bounds.min.x, bounds.max.x - keepTo);
                if (!beyond && overhang <= Overhang) continue;

                Remove(shown.gameObject);
                removed++;
            }

            Debug.Log(scene.name + ": 境目の向こうの物を " + removed + " 個片付け、" + cut + " 個を境目で切った");
        }

        /// <summary>片付けてよい物か。遊ぶための部品や、素材集のスクリプトを持つ物は残す</summary>
        private static bool IsPlain(GameObject shown)
        {
            foreach (Component part in shown.GetComponents<Component>())
            {
                if (part == null) return false;
                if (Array.IndexOf(Plain, part.GetType()) < 0) return false;
            }

            return true;
        }

        /// <summary>その物そのものの見た目と当たりの範囲。子は含めない。どちらも無ければ null</summary>
        private static Bounds? OwnBounds(GameObject shown)
        {
            Bounds? bounds = null;

            foreach (Renderer renderer in shown.GetComponents<Renderer>())
            {
                if (!renderer.enabled) continue;
                bounds = Grow(bounds, renderer.bounds);
            }

            foreach (Collider collider in shown.GetComponents<Collider>())
            {
                if (!collider.enabled) continue;
                bounds = Grow(bounds, collider.bounds);
            }

            return bounds;
        }

        private static Bounds Grow(Bounds? bounds, Bounds more)
        {
            if (bounds == null) return more;

            Bounds grown = bounds.Value;
            grown.Encapsulate(more);
            return grown;
        }

        /// <summary>境目で切れる箱か。基本の箱か板で、子を持たず、向きが世界の軸にそろっている</summary>
        private static bool IsBox(Transform shown)
        {
            if (shown.childCount > 0) return false;

            MeshFilter mesh = shown.GetComponent<MeshFilter>();
            bool primitive = mesh != null && mesh.sharedMesh != null && Array.IndexOf(Boxes, mesh.sharedMesh.name) >= 0;
            bool bareBox = mesh == null && shown.GetComponent<BoxCollider>() != null;
            if (!primitive && !bareBox) return false;

            Vector3 euler = shown.rotation.eulerAngles;
            return IsRight(euler.x) && IsRight(euler.y) && IsRight(euler.z);
        }

        private static bool IsRight(float angle)
        {
            float rest = Mathf.Repeat(angle, 90f);
            return rest < 0.5f || rest > 89.5f;
        }

        /// <summary>箱を、横（x）の keepFrom から keepTo の間に収まるよう縮めて寄せる</summary>
        private static void Cut(Transform box, Bounds bounds, float keepFrom, float keepTo)
        {
            float from = Mathf.Max(bounds.min.x, keepFrom);
            float to = Mathf.Min(bounds.max.x, keepTo);

            // 世界の x に沿う箱の軸を伸び縮みさせる
            Vector3 axis = box.InverseTransformDirection(Vector3.right);
            int index = Mathf.Abs(axis.x) >= Mathf.Abs(axis.y) && Mathf.Abs(axis.x) >= Mathf.Abs(axis.z) ? 0 : Mathf.Abs(axis.y) >= Mathf.Abs(axis.z) ? 1 : 2;
            Vector3 scale = box.localScale;
            scale[index] *= (to - from) / bounds.size.x;
            box.localScale = scale;

            Physics.SyncTransforms();
            Bounds shrunk = OwnBounds(box.gameObject) ?? bounds;
            box.position += Vector3.right * ((from + to) / 2f - shrunk.center.x);
        }

        /// <summary>物を片付ける。子に残す物があれば、その物の見た目と当たりだけを外す</summary>
        private static void Remove(GameObject shown)
        {
            if (shown.transform.childCount == 0)
            {
                Object.DestroyImmediate(shown);
                return;
            }

            foreach (Collider collider in shown.GetComponents<Collider>()) Object.DestroyImmediate(collider);
            foreach (Renderer renderer in shown.GetComponents<Renderer>()) Object.DestroyImmediate(renderer);
            MeshFilter mesh = shown.GetComponent<MeshFilter>();
            if (mesh != null) Object.DestroyImmediate(mesh);
        }

        /// <summary>
        /// 後ろの地形。地形の形は素材集の 1 つを場所どうしで使い回しているので、場所ごとに写しを作り、境目の向こうに穴を開ける。
        /// 素材集の元は書き換えない
        /// </summary>
        private static void Terrains(Scene scene, float? left, float? right)
        {
            int count = 0;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (Terrain terrain in root.GetComponentsInChildren<Terrain>(false))
                {
                    count++;
                    TerrainData data = OwnTerrain(terrain, scene.name + "Terrain" + (count == 1 ? "" : count.ToString()));
                    Holes(terrain, data, left ?? float.NegativeInfinity, right ?? float.PositiveInfinity);
                }
            }
        }

        /// <summary>この地形だけの形。まだ写していなければ写して付け替える</summary>
        private static TerrainData OwnTerrain(Terrain terrain, string name)
        {
            string path = TerrainFolder + "/" + name + ".asset";
            TerrainData data = terrain.terrainData;

            if (AssetDatabase.GetAssetPath(data) != path)
            {
                if (AssetDatabase.LoadAssetAtPath<TerrainData>(path) == null && !AssetDatabase.CopyAsset(AssetDatabase.GetAssetPath(data), path))
                    throw new InvalidOperationException(path + " へ地形を写せない");

                data = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                terrain.terrainData = data;

                TerrainCollider collider = terrain.GetComponent<TerrainCollider>();
                if (collider != null) collider.terrainData = data;
            }

            return data;
        }

        /// <summary>地形の、横（x）が keepFrom から keepTo の外に穴を開ける。中は塞ぐ</summary>
        private static void Holes(Terrain terrain, TerrainData data, float keepFrom, float keepTo)
        {
            int size = data.holesResolution;
            bool[,] surface = new bool[size, size];
            float x0 = terrain.transform.position.x;

            for (int column = 0; column < size; column++)
            {
                float x = x0 + (column + 0.5f) / size * data.size.x;
                bool keep = x >= keepFrom && x <= keepTo;
                for (int row = 0; row < size; row++) surface[row, column] = keep;
            }

            data.SetHoles(0, 0, surface);
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
        }

        /// <summary>シーンのルートに置かれた場所。プレビューで開いたシーンからも引く</summary>
        private static Area AreaOf(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Area area = root.GetComponent<Area>();
                if (area != null) return area;
            }

            throw new InvalidOperationException(scene.name + " に Area が無い");
        }

        private static string PathOf(string sceneName)
        {
            return SceneFolder + sceneName + ".unity";
        }
    }
}
