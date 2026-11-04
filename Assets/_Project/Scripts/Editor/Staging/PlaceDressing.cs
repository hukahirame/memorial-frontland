using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 根源1・2 の飾り。奥の壁の上に岩・崩れた石・低い柱を並べ、壁の向こうの遠くに柱や塔の影を立てて、平らな地平を崩す。
    /// どれも遊ぶ場の外に置き、当たりを外し、まとめて描く印（static batching）を付ける。遊ぶ場の地面には草と小石を散らす。
    /// 根源2（水）は、水の面を軽い水の素材に替え、水に蓮の葉を浮かべ、水辺に葦と水草を生やす。
    /// 並べ方は場所ごとの種で決まり、組み直しても同じになる。前に並べた飾りは消してから並べ直すので、何度呼んでも増えない
    /// </summary>
    public static class PlaceDressing
    {
        private const string Prefabs = "Assets/Dark_Fantasy/Assets/Prefabs_Grp/BASIC/";

        /// <summary>水の飾りの素材集（Assets/ 直下）。根源3の滝も同じ素材集から取っている</summary>
        private const string Forest = "Assets/TriForge Assets/Fantasy Forest Environment/Prefabs/";

        /// <summary>
        /// 根源2の水の面。もとの水の素材は画面の写し（GrabPass）と深さのテクスチャを使い、スマホでは重いうえ、
        /// カメラが深さを書かないので泡も深さの色も出ず、平らな青緑だった。根源3の水と同じ軽い作り（波の凹凸を持つ半透明の面）にする
        /// </summary>
        private const string WaterPlane = "Water_Plane";

        private const string WaterMaterial = "Assets/_Project/Art/Places/Root2Water.mat";

        private const string Ripple = "Assets/_Project/Art/FloodedMall/MallWaterNormal.png";

        /// <summary>水の色。底の土が透けすぎないよう、根源3の水より濃くする。明るいと青い空を映して水色に浮くので、暗めの青緑にする</summary>
        private static readonly Color WaterTint = new Color(0.12f, 0.32f, 0.34f, 0.82f);

        /// <summary>水の面は 50 ほどの広さがある。波の模様が根源3と同じくらいの細かさになる繰り返しの数</summary>
        private const float RippleTiling = 25f;

        /// <summary>水と水辺を見分ける格子の間隔。地面が水の面より低い所を水、その隣の床を水辺とみなす</summary>
        private const float WetStep = 0.5f;

        /// <summary>水の格子のうち蓮の葉を浮かべる割合と、水辺の格子のうち葦や水草を生やす割合</summary>
        private const float PadShare = 0.03f;

        private const float ReedShare = 0.15f;

        private static readonly string[] Pads =
        {
            "P_FFE_Lily_Pad_Group_1", "P_FFE_Lily_Pad_Group_2", "P_FFE_Lily_Pad_Group_3", "P_FFE_Lily_Pad_Group_4",
            "P_FFE_Lily_Pad_Group_5", "P_FFE_Lily_Pad_Group_6", "P_FFE_Lily_Pad_Flower_1", "P_FFE_Lily_Pad_Flower_2",
        };

        private static readonly string[] Reeds = { "Ground Foliage/P_FFE_Reeds_Small", "Ground Foliage/P_FFE_Reeds_Large", "P_FFE_Waterplants" };

        private const string Root1Scene = "Assets/_Project/Scenes/Root1.unity";

        private const string Root2Scene = "Assets/_Project/Scenes/Root2.unity";

        /// <summary>飾りをまとめて入れる、シーンのルートの物の名前</summary>
        private const string Holder = "Dressing";

        /// <summary>奥の壁の真ん中の奥行きと、上の面の高さ。根源1・2 とも、3 の立方体（Cube_E）を z 2.5 に並べた壁</summary>
        private const float WallZ = 2.5f;

        private const float WallTop = 1.5f;

        /// <summary>壁の上に置く物のあいだ（x）。この幅ごとに 1 つ、位置を少し揺らして置く</summary>
        private const float RidgeStep = 3.2f;

        /// <summary>
        /// 壁の上に置く物の高さの範囲と、横幅の上限。素材集の物は元の大きさがまちまちなので、置くときに測ってこの大きさへ縮める。
        /// 人の背より低くし、壁の奥行き（3）からはみ出さない
        /// </summary>
        private const float RidgeLow = 0.6f;

        private const float RidgeHigh = 1.2f;

        private const float RidgeWidth = 2.4f;

        /// <summary>遠くの森は、場の左右の端よりこのぶん外まで並べる。カメラが端に寄っても途切れない</summary>
        private const float FarOverhang = 8f;

        /// <summary>出口の前後このぶんは壁の上と地面を空ける。看板と出口が飾りに埋もれないように</summary>
        private const float ExitClear = 3f;

        /// <summary>遊ぶ場の地面に散らす絵。木や草と同じ「村の小物」の絵（Assets/ 直下の素材集）から、草の株と小石を選ぶ</summary>
        private const string Sheet = "Assets/Resources/TX Village Props 1.png";

        private const string SheetPrefix = "TX Village Props 1_";

        private static readonly string[] Grass = { "58", "59", "60", "61", "62", "67", "68" };

        private static readonly string[] Pebbles = { "50", "52" };

        /// <summary>草むら。場にもとからある草（Weed）と同じ絵</summary>
        private static readonly string[] Bushes = { "53", "54", "55" };

        /// <summary>地面に散らす固まりの数。地面の無い所・水・坂・出口の前は外れるので、置く数はこれより少ない</summary>
        private const int ScatterTries = 110;

        /// <summary>1 つの固まりに置く数の上限と、固まりの広がり（半径）。1 つずつ離して置くより、寄せた方が生えた草に見える</summary>
        private const int ClumpMax = 3;

        private const float ClumpSpread = 0.7f;

        /// <summary>散らす物のうち小石と草むらの割合。残りは草の株</summary>
        private const float PebbleShare = 0.25f;

        private const float BushShare = 0.1f;

        /// <summary>遊ぶ場の床の高さの範囲。上の段や壁の上には散らさない</summary>
        private const float FloorLow = -0.6f;

        private const float FloorHigh = 0.6f;

        /// <summary>壁の上に置く物。人の背より低い</summary>
        private static readonly string[] Ridge =
        {
            "Rock/Floor_Rock_Pile_01A_Lod0", "Rock/Floor_Rock_Pile_02A_Lod0", "Rock/Floor_Rock_Pile_03A_Lod0",
            "Rock/Break_Stone_02Z", "Rock/Break_Stone_03Z", "Rock/Break_Stone_04Z",
        };

        /// <summary>
        /// 遠くの森。板の木（カメラを向く 1 枚の絵の木）を 2 列に並べる。前は素材集の塔や柱を混ぜて散らしていたが、まとまりが無く
        /// 「適当」に見えた（ユーザーの指摘）。板の木の素材は GPU インスタンシングが効くので、同じ種類なら何本でも 1 回で描ける。
        /// 種類を 2〜3 に絞る。根源1は白樺の多い明るい森、根源2は針葉樹の暗い森
        /// </summary>
        private const string Billboards = "Assets/TriForge Assets/Fantasy Forest Environment/Prefabs/Trees/Billboards/";

        private static readonly string[] Root1Woods = { "P_FFE_Birch_1_Billboard", "P_FFE_Birch_2_Billboard", "P_FFE_Spruce_A1_Billboard" };

        private static readonly string[] Root2Woods = { "P_FFE_Spruce_A1_Billboard", "P_FFE_Spruce_A2_Billboard", "P_FFE_Birch_1_Billboard" };

        /// <summary>
        /// 地面に散らす絵の色。絵は光の当たり方を受けないので、場所の光（PlaceLooks）に合わせて沈める。
        /// 合わせないと、青く暗い根源2の地面で小石が白く浮く
        /// </summary>
        private static readonly Color Root1Tint = new Color(0.9f, 0.92f, 0.85f);

        private static readonly Color Root2Tint = new Color(0.68f, 0.76f, 0.8f);

        [MenuItem("Tools/Places/Dress Roots")]
        public static void DressRoots()
        {
            Dress(Root1Scene, 1, Root1Tint, Root1Woods, Root1Terrain, false);
            Dress(Root2Scene, 2, Root2Tint, Root2Woods, Root2Terrain, true);
            PlaceLooks.ShootAll();
        }

        private static void Dress(string scenePath, int seed, Color tint, string[] woods, string[] terrain, bool wet)
        {
            PlaceLooks.Edit(scenePath, scene =>
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.name == Holder) Object.DestroyImmediate(root);
                }

                Area area = Area.In(scene);
                if (area == null) throw new System.InvalidOperationException(scenePath + " に Area が無い");

                FieldBounds bounds = Exposition.ToBounds(area.Exposition);
                List<float> exits = Exits(scene);
                Random random = new Random(seed);

                // 新しく作った物は有効なシーン（書き換え中の場所のシーン）に入る
                Transform holder = new GameObject(Holder).transform;

                for (float x = bounds.MinX; x <= bounds.MaxX; x += RidgeStep)
                {
                    float at = x + Spread(random, RidgeStep * 0.3f);
                    if (NearExit(at, exits)) continue;

                    Vector3 position = new Vector3(at, WallTop, WallZ + Spread(random, 0.5f));
                    Put(holder, Load(Prefabs + Pick(random, Ridge) + ".prefab"), position, Between(random, RidgeLow, RidgeHigh), RidgeWidth, random);
                }

                Settle(holder);

                // 板の木はカメラを向く作りで、まとめて描くと向きが崩れる（素材が batching を断る）。印を付ける Settle のあとで足す
                Woods(holder, bounds, woods, random);

                // 水の飾りと壁の根元の草木は揺れる素材なので、まとめて描く印を付けない（Settle のあとで足す）
                if (wet) Wet(scene, holder, bounds, random);
                Hedge(holder, bounds, exits, random);

                Paint(scene, terrain);
                Scatter(scene, holder, bounds, exits, random, tint);
            });
        }

        /// <summary>
        /// 地形の塗り。塊を並べた地形（Cube_・Slope・Upper_Area）は、上の面も横の面も同じ茶色の絵で、奥の壁は縦の草の絵、
        /// 横の壁は城の壁の絵だった。面の向きで塗り分けるシェーダー（上は地面、横は崖。世界の座標から貼る）の素材 1 つに揃える。
        /// 絵は背景の森と同じ描き方の、森の素材集の地形の絵
        /// </summary>
        private const string TerrainShader = "MemorialFloor/TerrainBlock";

        private const string TerrainTextures = "Assets/TriForge Assets/Fantasy Forest Environment/Textures/Terrain/";

        private const string TerrainFolder = "Assets/_Project/Art/Places/";

        /// <summary>地面と崖の絵が 1 回繰り返す長さ。塊（3）と同じにし、人（1.5 ほど）に対して草の目が細かすぎないようにする</summary>
        private const float TerrainTile = 3f;

        /// <summary>場所ごとの地面と崖の絵。根源1は土の混じった草地と土の目地の岩、根源2は暗い草地と石</summary>
        private static readonly string[] Root1Terrain = { "Root1Terrain", "T_FFE_Grass02.tga", "T_FFE_Rock02.tga" };

        private static readonly string[] Root2Terrain = { "Root2Terrain", "T_FFE_Grass01_B.tga", "T_FFE_Rock01.tga" };

        /// <summary>
        /// 地形の塊に塗りを当てる。水の面（Water_Plane）があれば、その下は上の面も崖の絵にする（水の底が草にならない）
        /// </summary>
        private static void Paint(Scene scene, string[] terrain)
        {
            float underwater = -1000f;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == WaterPlane) underwater = root.transform.position.y - 0.05f;
            }

            Material ground = Terrain(terrain[0], terrain[1], terrain[2], underwater);

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == Holder) continue;

                foreach (MeshRenderer shown in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    string name = shown.gameObject.name;
                    if (!name.StartsWith("Cube") && !name.StartsWith("Slope") && !name.StartsWith("Upper_Area")) continue;

                    Material[] all = shown.sharedMaterials;
                    for (int i = 0; i < all.Length; i++) all[i] = ground;
                    shown.sharedMaterials = all;
                }
            }
        }

        /// <summary>地形の塗りの素材。在れば書き換える。作り直すと GUID が変わり、シーンからの参照が切れる</summary>
        private static Material Terrain(string name, string top, string side, float underwater)
        {
            Shader shader = Shader.Find(TerrainShader);
            if (shader == null) throw new System.InvalidOperationException(TerrainShader + " のシェーダーが無い（Art/Places/TerrainBlock.shader）");

            string path = TerrainFolder + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }

            material.shader = shader;
            material.SetTexture("_TopTex", Texture(top));
            material.SetTexture("_SideTex", Texture(side));
            material.SetFloat("_TopScale", TerrainTile);
            material.SetFloat("_SideScale", TerrainTile);
            material.SetFloat("_Underwater", underwater);
            material.SetFloat("_Glossiness", 0.08f);

            EditorUtility.SetDirty(material);
            AssetDatabase.SaveAssets();
            return material;
        }

        private static Texture2D Texture(string file)
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(TerrainTextures + file);
            if (texture == null) throw new System.InvalidOperationException(TerrainTextures + file + " が無い");

            return texture;
        }

        /// <summary>奥の壁の根元の草木。遊ぶ場の奥の縁と壁のあいだ（幅 0.4）に並べ、壁と地面の境の直線を隠す</summary>
        private static readonly string[] Hedges =
        {
            "Ground Foliage/P_FFE_Largebush", "Ground Foliage/P_FFE_Smallbush_2", "P_FFE_Bush_1", "P_FFE_Bush_2",
            "Ground Foliage/P_FFE_Grass_01", "Ground Foliage/P_FFE_Flowers_White",
        };

        /// <summary>根元の草木のあいだ（x）。揺れる素材はまとめて描けず 1 本ずつ描くので、数を抑える</summary>
        private const float HedgeStep = 2.4f;

        /// <summary>
        /// 奥の壁の根元に草木を並べる。出口の前は空ける。当たりは外し、影は落とさない（Grow）。
        /// 揺れる素材なので、まとめて描く印は付けない
        /// </summary>
        private static void Hedge(Transform holder, FieldBounds bounds, List<float> exits, Random random)
        {
            for (float x = bounds.MinX + 0.5f; x <= bounds.MaxX - 0.5f; x += HedgeStep)
            {
                float at = x + Spread(random, HedgeStep * 0.3f);
                string pick = Pick(random, Hedges);
                float height = Between(random, 0.6f, 1.2f);
                if (NearExit(at, exits)) continue;

                Grow(holder, Load(Forest + pick + ".Prefab"), new Vector3(at, 0f, bounds.MaxZ + 0.25f), height, 2f, random, false);
            }
        }

        /// <summary>遠くの森の 2 列の奥行き。手前の列は奥の壁（奥行き 1〜4）のすぐ向こう、奥の列はその後ろで、霧に薄れる</summary>
        private const float WoodNear = 6.5f;

        private const float WoodFar = 10.5f;

        /// <summary>森の木のあいだ（x）。奥の列は半分ずらし、手前の列の隙間を埋める</summary>
        private const float WoodStep = 2.6f;

        /// <summary>森の木の高さ。奥の列ほど高くし、手前の列の上に頭を出す。壁（高さ 1.5）の上にはっきり出る</summary>
        private const float WoodNearLow = 5f;

        private const float WoodNearHigh = 6.5f;

        private const float WoodFarLow = 6f;

        private const float WoodFarHigh = 8f;

        private const float WoodWidth = 4.5f;

        /// <summary>
        /// 遠くの森を 2 列に並べる。間隔はほぼ一定で、少しだけ揺らす。板の木は向きを持たないので回さない。
        /// 影は落とさない（遠くの絵の木の影は見えず、影を描くぶん重い）
        /// </summary>
        private static void Woods(Transform holder, FieldBounds bounds, string[] woods, Random random)
        {
            for (int row = 0; row < 2; row++)
            {
                bool far = row == 1;
                float offset = far ? WoodStep / 2f : 0f;

                for (float x = bounds.MinX - FarOverhang + offset; x <= bounds.MaxX + FarOverhang; x += WoodStep)
                {
                    float at = x + Spread(random, WoodStep * 0.25f);
                    float z = (far ? WoodFar : WoodNear) + Spread(random, 0.8f);
                    float height = far ? Between(random, WoodFarLow, WoodFarHigh) : Between(random, WoodNearLow, WoodNearHigh);
                    GameObject tree = Put(holder, Load(Billboards + Pick(random, woods) + ".prefab"), new Vector3(at, 0f, z), height, WoodWidth, random, false, false);

                    foreach (Renderer shown in tree.GetComponentsInChildren<Renderer>(true))
                    {
                        shown.shadowCastingMode = ShadowCastingMode.Off;
                        shown.receiveShadows = false;
                    }
                }
            }
        }

        /// <summary>
        /// 根源2の水。水の面を軽い水の素材に替え、格子で水と水辺を見分けて、水に蓮の葉、水辺に葦と水草を置く。
        /// 地面が水の面より低い所を水、水の隣の床（Floor が床とみなす所）を水辺とする
        /// </summary>
        private static void Wet(Scene scene, Transform holder, FieldBounds bounds, Random random)
        {
            GameObject plane = null;
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == WaterPlane) plane = root;
            }

            if (plane == null) throw new System.InvalidOperationException(scene.name + " に " + WaterPlane + " が無い");

            //水の面は床より低く、前に立つ物が無いので、根源3の水と同じく透ける物の誰よりも先に描く。草や木の絵に重ならない
            Renderer surface = plane.GetComponent<Renderer>();
            surface.sharedMaterial = Water();
            surface.sortingOrder = short.MinValue;
            PrefabUtility.RecordPrefabInstancePropertyModifications(surface);

            float level = plane.transform.position.y;
            Physics.SyncTransforms();

            int columns = Mathf.FloorToInt((bounds.MaxX - bounds.MinX) / WetStep) + 1;
            int rows = Mathf.FloorToInt((bounds.MaxZ - bounds.MinZ) / WetStep) + 1;
            bool[,] water = new bool[columns, rows];
            bool[,] land = new bool[columns, rows];

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    float x = bounds.MinX + i * WetStep;
                    float z = bounds.MinZ + j * WetStep;
                    water[i, j] = Ground(scene, x, z, out float ground) && ground < level - 0.1f;
                    land[i, j] = Floor(scene, x, z, out float floor);
                }
            }

            for (int i = 0; i < columns; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    double roll = random.NextDouble();
                    string pad = Pick(random, Pads);
                    string reed = Pick(random, Reeds);
                    float size = (float)random.NextDouble();

                    float x = bounds.MinX + i * WetStep;
                    float z = bounds.MinZ + j * WetStep;

                    if (water[i, j] && roll < PadShare)
                    {
                        Grow(holder, Load(Forest + pad + ".Prefab"), new Vector3(x, level + 0.01f, z), 1f, Mathf.Lerp(1.4f, 2.2f, size), random, true);
                    }
                    else if (land[i, j] && NextToWater(water, i, j) && roll < ReedShare)
                    {
                        Floor(scene, x, z, out float floor);
                        Grow(holder, Load(Forest + reed + ".Prefab"), new Vector3(x, floor, z), Mathf.Lerp(0.8f, 1.4f, size), 1.4f, random, false);
                    }
                }
            }
        }

        /// <summary>揺れる飾りを 1 つ置く。大きさは Put と同じく測って合わせ、当たりは外す。まとめて描く印は付けない（揺れが崩れる）</summary>
        private static void Grow(Transform holder, GameObject prefab, Vector3 at, float height, float width, Random random, bool byPivot)
        {
            GameObject piece = Put(holder, prefab, at, height, width, random, byPivot);
            foreach (Collider body in piece.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(body);
            foreach (Renderer shown in piece.GetComponentsInChildren<Renderer>(true)) shown.shadowCastingMode = ShadowCastingMode.Off;
        }

        private static bool NextToWater(bool[,] water, int i, int j)
        {
            for (int di = -1; di <= 1; di++)
            {
                for (int dj = -1; dj <= 1; dj++)
                {
                    int a = i + di;
                    int b = j + dj;
                    if (a < 0 || b < 0 || a >= water.GetLength(0) || b >= water.GetLength(1)) continue;
                    if (water[a, b]) return true;
                }
            }

            return false;
        }

        /// <summary>(x, z) の真下の地面の高さ。水の面は見ずに、その下の地面を見る。ほかに開いているシーンの物は見ない</summary>
        private static bool Ground(Scene scene, float x, float z, out float y)
        {
            y = 0f;
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 10f, z), Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                GameObject ground = hit.collider.gameObject;
                if (ground.scene != scene || ground.name == WaterPlane) continue;

                y = hit.point.y;
                return true;
            }

            return false;
        }

        /// <summary>根源2の水の素材。在れば書き換える。作り直すと GUID が変わり、シーンからの参照が切れる</summary>
        private static Material Water()
        {
            Material water = AssetDatabase.LoadAssetAtPath<Material>(WaterMaterial);
            if (water == null)
            {
                water = new Material(Shader.Find("Standard"));
                AssetDatabase.CreateAsset(water, WaterMaterial);
            }

            water.SetFloat("_Mode", 3f);
            water.SetInt("_SrcBlend", (int)BlendMode.One);
            water.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            water.SetInt("_ZWrite", 0);
            water.DisableKeyword("_ALPHATEST_ON");
            water.DisableKeyword("_ALPHABLEND_ON");
            water.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            water.renderQueue = (int)RenderQueue.Transparent;
            water.color = WaterTint;
            water.SetFloat("_Glossiness", 0.85f);
            water.SetFloat("_Metallic", 0f);

            Texture2D ripple = AssetDatabase.LoadAssetAtPath<Texture2D>(Ripple);
            if (ripple == null) throw new System.InvalidOperationException(Ripple + " が無い。見た目を試すシーンを組む（Tools/LookDev/Build Flooded Mall）");

            water.SetTexture("_BumpMap", ripple);
            water.SetFloat("_BumpScale", 0.4f);
            water.EnableKeyword("_NORMALMAP");
            water.mainTextureScale = new Vector2(RippleTiling, RippleTiling);

            EditorUtility.SetDirty(water);
            AssetDatabase.SaveAssets();
            return water;
        }

        /// <summary>
        /// 遊ぶ場の地面に草の株と小石を散らす。真上から地面を探し、そのシーンの床（Cube_ で始まる物）の平らな上面にだけ置く。
        /// 水（Cube_w）・坂・上の段・出口の前には置かない。絵は当たりを持たないので、歩くのも湧くのも妨げない。描く順は奥行きで決める。
        /// 試すたびに乱数を同じ数だけ引くので、置けない所があっても並びは組み直しで変わらない
        /// </summary>
        private static void Scatter(Scene scene, Transform holder, FieldBounds bounds, List<float> exits, Random random, Color tint)
        {
            Dictionary<string, Sprite> sheet = new Dictionary<string, Sprite>();
            foreach (Object part in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            {
                if (part is Sprite sprite) sheet[sprite.name] = sprite;
            }

            Physics.SyncTransforms();

            for (int i = 0; i < ScatterTries; i++)
            {
                float centerX = Between(random, bounds.MinX + 1f, bounds.MaxX - 1f);
                float centerZ = Between(random, bounds.MinZ + 0.5f, bounds.MaxZ - 0.3f);
                int count = 1 + random.Next(ClumpMax);

                for (int j = 0; j < ClumpMax; j++)
                {
                    float x = centerX + Spread(random, ClumpSpread);
                    float z = centerZ + Spread(random, ClumpSpread);
                    double kind = random.NextDouble();
                    string pick = Pick(random, kind < PebbleShare ? Pebbles : kind < PebbleShare + BushShare ? Bushes : Grass);
                    float size = (float)random.NextDouble();
                    bool flip = random.Next(2) == 0;

                    if (j >= count || NearExit(x, exits) || !Floor(scene, x, z, out float floor)) continue;

                    //草の株は小さな絵なので大きめに、草むらはもとの草と同じくらいに
                    float scale = kind < PebbleShare ? Mathf.Lerp(2f, 3f, size)
                                : kind < PebbleShare + BushShare ? Mathf.Lerp(0.9f, 1.3f, size)
                                : Mathf.Lerp(2.6f, 3.8f, size);
                    Tuft(holder, sheet, SheetPrefix + pick, new Vector3(x, floor, z), scale, flip, tint);
                }
            }
        }

        /// <summary>地面の絵を 1 つ置く。底を床に着け、描く順は奥行きで決める</summary>
        private static void Tuft(Transform holder, Dictionary<string, Sprite> sheet, string name, Vector3 at, float scale, bool flip, Color tint)
        {
            if (!sheet.TryGetValue(name, out Sprite sprite)) throw new System.InvalidOperationException(Sheet + " に " + name + " が無い");

            GameObject tuft = new GameObject(name);
            tuft.transform.SetParent(holder, false);
            tuft.transform.localScale = new Vector3(scale, scale, 1f);
            tuft.transform.position = new Vector3(at.x, at.y - sprite.bounds.min.y * scale, at.z);

            SpriteRenderer shown = tuft.AddComponent<SpriteRenderer>();
            shown.sprite = sprite;
            shown.flipX = flip;
            shown.color = tint;
            shown.sortingOrder = Mathf.RoundToInt(at.z * FloodedMallBuilder.OrderPerDepth);
        }

        /// <summary>(x, z) の真下の床の高さ。いちばん上で当たったそのシーンの物が、平らな床でなければ false。ほかに開いているシーンの物は見ない</summary>
        private static bool Floor(Scene scene, float x, float z, out float y)
        {
            y = 0f;
            RaycastHit[] hits = Physics.RaycastAll(new Vector3(x, 10f, z), Vector3.down, 20f, ~0, QueryTriggerInteraction.Ignore);
            System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            foreach (RaycastHit hit in hits)
            {
                GameObject ground = hit.collider.gameObject;
                if (ground.scene != scene) continue;

                bool floor = ground.name.StartsWith("Cube") && !ground.name.StartsWith("Cube_w")
                             && hit.normal.y > 0.95f && hit.point.y > FloorLow && hit.point.y < FloorHigh;
                y = hit.point.y;
                return floor;
            }

            return false;
        }

        /// <summary>出口（ST_ で始まる物）の x。壁の上のその前後は空ける</summary>
        private static List<float> Exits(Scene scene)
        {
            List<float> exits = new List<float>();
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name.StartsWith("ST_")) exits.Add(root.transform.position.x);
            }

            return exits;
        }

        private static bool NearExit(float x, List<float> exits)
        {
            foreach (float exit in exits)
            {
                if (Mathf.Abs(x - exit) < ExitClear) return true;
            }

            return false;
        }

        /// <summary>
        /// 飾りを 1 つ置く。高さ height に合わせて縮め（横は width まで）、底を at の高さに、真ん中を at に着ける。
        /// 素材集との繋がりは切る（当たりを外し、まとめて描く印を付けるため）
        /// </summary>
        /// <param name="byPivot">大きさだけ合わせ、物の基点を at に置く。水面に浮かべる物（蓮の葉）は、水中の茎まで含めた底ではなく、水面に置く基点で合わせる</param>
        /// <param name="turn">向きを揺らす。カメラを向く板の木は回さない（乱数は同じだけ引く）</param>
        private static GameObject Put(Transform holder, GameObject prefab, Vector3 at, float height, float width, Random random,
                                      bool byPivot = false, bool turn = true)
        {
            GameObject piece = (GameObject)PrefabUtility.InstantiatePrefab(prefab, holder.gameObject.scene);
            PrefabUtility.UnpackPrefabInstance(piece, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

            float yaw = Between(random, 0f, 360f);
            piece.transform.SetParent(holder, false);
            piece.transform.SetPositionAndRotation(at, Quaternion.Euler(0f, turn ? yaw : 0f, 0f));
            piece.transform.localScale = Vector3.one;

            Bounds size = Size(piece);
            float scale = Mathf.Min(height / size.size.y, width / Mathf.Max(size.size.x, size.size.z));
            piece.transform.localScale = Vector3.one * scale;

            if (byPivot) return piece;

            size = Size(piece);
            piece.transform.position += new Vector3(at.x - size.center.x, at.y - size.min.y, at.z - size.center.z);
            return piece;
        }

        private static GameObject Load(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (prefab == null) throw new System.InvalidOperationException(path + " が無い");

            return prefab;
        }

        /// <summary>見える部分すべてを囲む箱</summary>
        private static Bounds Size(GameObject piece)
        {
            Renderer[] shown = piece.GetComponentsInChildren<Renderer>(true);
            if (shown.Length == 0) throw new System.InvalidOperationException(piece.name + " に見える部分が無い");

            Bounds bounds = shown[0].bounds;
            foreach (Renderer part in shown) bounds.Encapsulate(part.bounds);

            return bounds;
        }

        /// <summary>飾りの当たりを外し、動かない物としてまとめて描く印を付ける。飾りは遊ぶ場の外なので、当たりは要らない</summary>
        private static void Settle(Transform holder)
        {
            foreach (Collider body in holder.GetComponentsInChildren<Collider>(true)) Object.DestroyImmediate(body);

            // 壁の上の小さな岩の影はほとんど見えず、影を描くぶん重い
            foreach (Renderer shown in holder.GetComponentsInChildren<Renderer>(true)) shown.shadowCastingMode = ShadowCastingMode.Off;

            foreach (Transform part in holder.GetComponentsInChildren<Transform>(true))
            {
                GameObject piece = part.gameObject;
                GameObjectUtility.SetStaticEditorFlags(piece, GameObjectUtility.GetStaticEditorFlags(piece) | StaticEditorFlags.BatchingStatic);
            }
        }

        private const string MainSiteScene = "Assets/_Project/Scenes/MainSite.unity";

        /// <summary>
        /// 拠点と根源1・2 の動かない物（地面の塊・坂・壁・石垣・飾りの岩など）に、まとめて描く印（static batching）を付ける。根源3と同じ軽くし方（[D-024]）。
        /// 動く物には付けない: スクリプト・Animator・Rigidbody を自分か親に持つ物、核の候補（像。壊れると膨らむ）、まとめると崩れる素材
        /// （カメラを向く板の木、風で揺れる草木。シェーダーが DisableBatching を宣言する）。
        /// 核の候補に前から付いていた印（素材集の像の prefab が持つ）は外す。付いたままだと、壊れて膨らむ動きが画面に出ない
        /// </summary>
        [MenuItem("Tools/Places/Lighten Places")]
        public static void LightenPlaces()
        {
            foreach (string place in new[] { MainSiteScene, Root1Scene, Root2Scene }) PlaceLooks.Edit(place, Lighten);
        }

        private static void Lighten(Scene scene)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                foreach (MeshRenderer shown in root.GetComponentsInChildren<MeshRenderer>(true))
                {
                    GameObject piece = shown.gameObject;
                    if (Moves(piece.transform) || Refuses(shown)) continue;

                    GameObjectUtility.SetStaticEditorFlags(piece, GameObjectUtility.GetStaticEditorFlags(piece) | StaticEditorFlags.BatchingStatic);
                }

                foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
                {
                    if (!Candidate(part)) continue;

                    foreach (Transform piece in part.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(piece.gameObject, 0);
                }
            }
        }

        /// <summary>自分か親が、核の候補か、スクリプト・Animator・Rigidbody を持つ</summary>
        private static bool Moves(Transform part)
        {
            for (Transform at = part; at != null; at = at.parent)
            {
                if (Candidate(at)) return true;
                if (at.GetComponent<MonoBehaviour>() != null || at.GetComponent<Animator>() != null || at.GetComponent<Rigidbody>() != null) return true;
            }

            return false;
        }

        /// <summary>核の候補か。印（タグ）か部品のどちらかで見分ける。部品の型は Assembly-CSharp にあり、このアセンブリから参照しないので名前で引く</summary>
        private static bool Candidate(Transform part)
        {
            return part.CompareTag("SpawnerCandidate") || part.GetComponent("SpawnerCandidate") != null;
        }

        /// <summary>まとめて描くと崩れる素材か。カメラを向く板の木や風で揺れる草木は、シェーダーが DisableBatching を宣言している</summary>
        private static bool Refuses(Renderer shown)
        {
            foreach (Material material in shown.sharedMaterials)
            {
                if (material != null && material.GetTag("DisableBatching", false, "False") != "False") return true;
            }

            return false;
        }

        private static string Pick(Random random, string[] from)
        {
            return from[random.Next(from.Length)];
        }

        private static float Spread(Random random, float half)
        {
            return Between(random, -half, half);
        }

        private static float Between(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
