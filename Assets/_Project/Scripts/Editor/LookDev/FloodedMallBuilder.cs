using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 水没したモールの見た目を試すシーンを組む。素材と部品の prefab を書き直し、部品を並べたシーンを作り直す。
    /// 開いているシーンの並びと、どれが有効かは、終わったら元に戻す。このシーンが開いていれば、閉じずに中身を入れ替える。
    /// 左右の通路は吹き抜けの水で分かれ、奥の通路だけでつながる。上の階が同じ形で重なり、その縁から吹き抜けへ滝が落ちる。
    /// 上の階へは、奥の通路から吹き抜けの左の縁に沿って上る、止まったエスカレーターで上がる
    /// </summary>
    public static class FloodedMallBuilder
    {
        public const string ScenePath = "Assets/_Project/Scenes/LookDev/FloodedMall.unity";

        private const string ArtFolder = "Assets/_Project/Art/FloodedMall";

        /// <summary>ほかの場所と同じ土。Root1 の地面の素材</summary>
        private const string DirtMaterial = "Assets/TriForge Assets/Fantasy Forest Environment/Materials/M_FFE_Dirt02.mat";

        /// <summary>土と同じ森の素材集の prefab</summary>
        private const string ForestPrefabs = "Assets/TriForge Assets/Fantasy Forest Environment/Prefabs/";

        private static readonly string[] Rocks = { "P_FFE_Rock1.Prefab", "P_FFE_Rock2.Prefab", "P_FFE_Rock3.Prefab" };

        /// <summary>
        /// MainSite と同じ滝の形。奥（+z）の縁から手前へ落ちる形で、高さは約 6.3。
        /// 素材集の絵は白い霧が 3 割ほど透けるだけで、クリーム色の壁の前では見えないため、絵は自作の物に替える
        /// </summary>
        private const string Waterfall = "P_FFE_Waterfall_Small.Prefab";

        /// <summary>Root1 と MainSite の木と草の絵をまとめた 1 枚</summary>
        private const string PropsSheetGuid = "7a6d5b44f9f87fd47880b760dae33c49";

        /// <summary>日差しの筋の板。両面が見え、後ろを明るくするだけの透け方</summary>
        private const string AdditiveShader = "Legacy Shaders/Particles/Additive";

        /// <summary>後処理の範囲を置くレイヤー。カメラはこのレイヤーの範囲だけを見る</summary>
        private const string PostLayer = "Post Processing";

        private const string PostProfile = ArtFolder + "/MallPost.asset";

        /// <summary>後処理の層が使うシェーダーとテクスチャの一覧。スクリプトで層を足したときは自分で渡す</summary>
        private const string PostResources = "Packages/com.unity.postprocessing/PostProcessing/PostProcessResources.asset";

        /// <summary>焼いた映り込みの置き場。シーンの名前のフォルダ</summary>
        private const string ReflectionFolder = "Assets/_Project/Scenes/LookDev/FloodedMall";

        /// <summary>ゲームの場所として組むときの後処理。物の際を暗くする効果（AO）は Android で重いので使わない</summary>
        private const string PlacePostProfile = ArtFolder + "/MallPlacePost.asset";

        /// <summary>常駐シーンのカメラにも全体に効く範囲があり、その優先度は 0。場所の範囲はそれより上に置き、場所の設定を勝たせる</summary>
        private const float PlacePriority = 1f;

        /// <summary>奥行き（z）1 あたりの描く順。プレイヤーと敵の順を奥行きから決める DynamicLayer と同じ</summary>
        internal const float OrderPerDepth = -100f;

        /// <summary>水面の描く順。床の高さにあって前に立つ物が無いので、誰よりも先に描く</summary>
        private const int SurfaceOrder = short.MinValue;

        /// <summary>
        /// 湧く敵の地面を探す Raycast（Seeker）が素通りするレイヤー。既定の Raycast はこのレイヤーを見ない。
        /// 吹き抜けの底・手すり・見えない壁・柱・木・岩・エスカレーターに付け、敵を通路の床にだけ湧かせる。当たり判定はそのまま効く。
        /// プレイヤーの接地の判定も既定の Raycast なので、このレイヤーの物の上では跳べない
        /// </summary>
        internal const int SeekThrough = 2;

        /// <summary>垂れるツタの絵の種類。MallIvy0.png から順に並ぶ</summary>
        private const int IvyKinds = 3;

        /// <summary>ツタの葉の絵を抜く境目</summary>
        private const float IvyCutoff = 0.5f;

        /// <summary>縁から垂れるツタの面。縁の面の表（1.51）と帯（1.53）から離し、遠くでちらつかないようにする</summary>
        private const float IvyLip = 1.58f;

        private const long TreeSprite = 4866695361572111326;

        /// <summary>部品の一辺。場所のシーンの Cube と同じマス</summary>
        private const float Cell = 3f;

        /// <summary>真ん中から左右の壁までのマス</summary>
        private const int Half = 5;

        private const int FrontRow = -1;

        private const int BackRow = 5;

        /// <summary>吹き抜けは真ん中から左右にこのマスまで</summary>
        private const int AtriumHalf = 2;

        /// <summary>吹き抜けはこの行まで。その奥の行が、左右の通路をつなぐ</summary>
        private const int AtriumBackRow = 3;

        /// <summary>通路に張った水の高さ</summary>
        private const float ShallowWater = 0.06f;

        /// <summary>吹き抜けの水面の高さ</summary>
        private const float PoolWater = -1.6f;

        /// <summary>吹き抜けの底の高さ。水を透かして床が見える深さ</summary>
        private const float PoolFloor = -5f;

        /// <summary>上の階の床の高さ。遊ぶときのカメラで、奥の上の階の手すりが画面の上端に収まる</summary>
        internal const float UpperFloor = 4f;

        /// <summary>上の階の縁から下へ垂れた面の深さ。深いと、下の階から吹き抜けが見えなくなる</summary>
        private const float UpperFace = 0.9f;

        /// <summary>柱の高さ。上に天窓の梁が載る</summary>
        private const float PillarHeight = 9f;

        /// <summary>折れた柱に残った高さ。上の階の床の下に収まる</summary>
        private const float BrokenPillarHeight = 3f;

        /// <summary>柱を立てる線。吹き抜けの縁から 1.1 外に回し、天窓はこの内側いっぱいに架ける</summary>
        private const float PillarX = 8.6f;

        private const float PillarFrontZ = -4.5f;

        private const float PillarBackZ = 11.6f;

        /// <summary>天窓の棟が、梁の上からどれだけ高いか</summary>
        private const float RidgeRise = 2.2f;

        /// <summary>滝の水が縁を越える所。縁の面の表（1.5）より少し内側</summary>
        private const float LipAt = 1.3f;

        private static readonly long[] WeedSprites = { -824228453194347739, -8518546357431239472, 5956658914707752938 };

        /// <summary>崩れた縁を置くマス（列, 行）。崩れた所から吹き抜けへ水がこぼれる</summary>
        private static readonly Vector2Int[] BrokenLedges = { new Vector2Int(-3, 1), new Vector2Int(3, 2), new Vector2Int(0, 4) };

        /// <summary>
        /// 上の階から吹き抜けへ水があふれ落ちるマス（列, 行）。奥の 2 本は、遊ぶときのカメラの正面に入る。
        /// 左の縁はエスカレーターが沿うので落とさない。手前へずらすと、左の通路に立ったカメラのすぐ横をふさぐ
        /// </summary>
        private static readonly Vector2Int[] Spills = { new Vector2Int(-1, 4), new Vector2Int(2, 4), new Vector2Int(3, 0) };

        /// <summary>滝の幅の倍率。Spills と同じ並び</summary>
        private static readonly float[] SpillWidths = { 1f, 0.75f, 0.9f };

        /// <summary>日の向き。光と、天窓から差す日差しの筋が同じ向きを使う</summary>
        private static readonly Quaternion Sun = Quaternion.Euler(55f, 25f, 0f);

        /// <summary>
        /// 日差しの筋が天窓を抜ける所（x, z）。日の向きに伸び、吹き抜けの水に届く。
        /// 手前の物ほど吹き抜けのカメラに近く、太くぼやけて見えるので、z は -1 より奥にする
        /// </summary>
        private static readonly Vector2[] Shafts =
        {
            new Vector2(-5.5f, -0.5f), new Vector2(-2.5f, 1.5f), new Vector2(0.5f, -0.5f),
            new Vector2(3f, 1.5f), new Vector2(-4f, 3.5f), new Vector2(-0.5f, 4f),
        };

        /// <summary>日差しの筋の幅。Shafts と同じ並び</summary>
        private static readonly float[] ShaftWidths = { 1.6f, 2f, 1.4f, 1.8f, 1.3f, 1.7f };

        /// <summary>
        /// 映り込みを焼く箱。左・右・奥の通路は上の階の床の下まで、吹き抜けは水面から天窓まで。
        /// 箱に合わせて映すので、屋根の下の浅い水は空ではなく天井を映す
        /// </summary>
        private static readonly Bounds[] Mirrors =
        {
            new Bounds(new Vector3(-12f, 1.75f, 6f), new Vector3(9f, 3.5f, 21f)),
            new Bounds(new Vector3(12f, 1.75f, 6f), new Vector3(9f, 3.5f, 21f)),
            new Bounds(new Vector3(0f, 1.75f, 13.5f), new Vector3(15f, 3.5f, 6f)),
            new Bounds(new Vector3(0f, 4f, 3f), new Vector3(15f, 11f, 15f)),
        };

        /// <summary>映り込みの名前。Mirrors と同じ並び。焼いた絵のファイルの名前にもなる</summary>
        private static readonly string[] MirrorNames = { "Reflection Left", "Reflection Right", "Reflection Back", "Reflection Atrium" };

        /// <summary>
        /// 上の階の縁からツタが垂れるマス（列, 行）。滝の落ちるマスと床の抜けたマスは避ける。
        /// エスカレーターが沿う左の縁と、その下端の上は、垂れたツタがエスカレーターに刺さるので避ける
        /// </summary>
        private static readonly Vector2Int[] UpperIvy =
        {
            new Vector2Int(-3, -1), new Vector2Int(3, -1), new Vector2Int(3, 1), new Vector2Int(3, 3),
            new Vector2Int(0, 4), new Vector2Int(1, 4),
        };

        /// <summary>下の階の縁から吹き抜けの水へツタが垂れるマス（列, 行）</summary>
        private static readonly Vector2Int[] LowerIvy =
        {
            new Vector2Int(-3, -1), new Vector2Int(3, 1), new Vector2Int(3, 3), new Vector2Int(1, 4),
        };

        /// <summary>止まったエスカレーターの傾き（度）。上の階まで、横に約 6.9 進む</summary>
        private const float EscalatorPitch = 30f;

        /// <summary>エスカレーターの幅の半分。段の両側に手すりのガラスが立つ</summary>
        private const float EscalatorHalfWidth = 0.8f;

        /// <summary>エスカレーターの真ん中の x。左の上の階の縁の面（-7.49）に触れないよう、少し吹き抜け側へ寄せる</summary>
        private const float EscalatorX = -6.6f;

        /// <summary>上の踊り場の長さ。ここから横の上の階へ降りる</summary>
        private const float EscalatorHeadLength = 1.5f;

        private const int EscalatorSteps = 20;

        /// <summary>崩れて抜けた段。この段から BrokenSteps 枚で、板が渡してある</summary>
        private const int BrokenStep = 11;

        private const int BrokenSteps = 2;

        /// <summary>見えない壁の高さ。跳んでも越えられない</summary>
        private const float BarrierHeight = 2.5f;

        /// <summary>エスカレーターの下端が載る、奥の通路の縁のマス（列, 行）。手すりを開ける</summary>
        private static readonly Vector2Int EscalatorFoot = new Vector2Int(-2, 4);

        /// <summary>エスカレーターの上の踊り場から降りる、左の上の階の縁のマス（列, 行）。手すりを開ける</summary>
        private static readonly Vector2Int EscalatorHead = new Vector2Int(-3, 1);

        /// <summary>エスカレーターの下端の z。奥の通路の、吹き抜けに面した縁</summary>
        private static float EscalatorFootZ => (AtriumBackRow + 0.5f) * Cell;

        /// <summary>エスカレーターが上の階まで横に進む長さ</summary>
        private static float EscalatorRun => UpperFloor / Mathf.Tan(EscalatorPitch * Mathf.Deg2Rad);

        /// <summary>天窓の奥の梁から垂れるツタの位置（x）。奥の柱（±4.5）は避ける</summary>
        private static readonly float[] BackBeamIvy = { -6.5f, -1.5f, 2.5f };

        /// <summary>天窓の左右の梁から垂れるツタ（左なら -1、右なら 1, z）。手前の梁は吹き抜けのカメラの後ろなので使わない</summary>
        private static readonly Vector2[] SideBeamIvy = { new Vector2(-1f, 0.5f), new Vector2(-1f, 6f), new Vector2(1f, 3f) };

        /// <summary>奥の壁の上端から、上の階の店の上へ垂れるツタの位置（x）</summary>
        private static readonly float[] WallIvy = { -12f, -6.5f, 1.5f, 9f, 13.5f };

        /// <summary>根元からツタが巻きつく柱。Pillars の中から選ぶ</summary>
        private static readonly Vector3[] ClimbingIvy =
        {
            new Vector3(-PillarX, 0f, 7.5f), new Vector3(4.5f, 0f, PillarBackZ), new Vector3(PillarX, 0f, PillarFrontZ),
        };

        /// <summary>柱に巻きつく高さ。ClimbingIvy と同じ並び</summary>
        private static readonly float[] ClimbHeights = { 3.2f, 2.6f, 4.5f };

        /// <summary>上の階の床が抜け落ちたマス。下の階の折れた柱の隣</summary>
        private static readonly Vector2Int[] UpperHoles = { new Vector2Int(3, 2) };

        private static readonly Vector3[] Pillars =
        {
            new Vector3(-PillarX, 0f, PillarFrontZ), new Vector3(-PillarX, 0f, 1.5f), new Vector3(-PillarX, 0f, 7.5f),
            new Vector3(PillarX, 0f, PillarFrontZ), new Vector3(PillarX, 0f, 1.5f),
            new Vector3(-4.5f, 0f, PillarBackZ), new Vector3(4.5f, 0f, PillarBackZ),
        };

        private static readonly Vector3[] BrokenPillars = { new Vector3(PillarX, 0f, 7.5f) };

        /// <summary>土の盛り上がり。下の階の壁際と角に寄せる。上の階に置くと、土の底が下の階の天井から突き出る</summary>
        private static readonly Vector3[] Mounds =
        {
            new Vector3(-14f, 0f, 14.6f), new Vector3(13.6f, 0f, 15f), new Vector3(-15f, 0f, 2f),
            new Vector3(15f, 0f, -1f), new Vector3(-2.5f, 0f, 14.8f),
        };

        private static readonly Vector3[] Trees =
        {
            new Vector3(-14.2f, 0f, 15.2f), new Vector3(15.2f, 0f, -0.6f),
            new Vector3(14.2f, UpperFloor, 15.2f), new Vector3(-15.4f, UpperFloor, 4f),
        };

        private static readonly Vector3[] Weeds =
        {
            new Vector3(-13.5f, UpperFloor, 12.5f), new Vector3(12.8f, UpperFloor, 3.5f), new Vector3(-6f, UpperFloor, 15.6f),
            new Vector3(5.5f, UpperFloor, 15.4f), new Vector3(-15.8f, 0f, 9f), new Vector3(11f, 0f, 15.8f),
        };

        private static readonly Vector3[] RubblePiles =
        {
            new Vector3(-13.5f, 0f, 8.5f), new Vector3(12.5f, 0f, 10.5f), new Vector3(6f, 0f, 15f),
            new Vector3(-11.5f, UpperFloor, 15.2f), new Vector3(14.6f, UpperFloor, 8.5f),
        };

        /// <summary>吹き抜けの水から頭を出した石</summary>
        private static readonly Vector3[] Stones =
        {
            new Vector3(-6.2f, PoolWater, 3.6f), new Vector3(5.8f, PoolWater, 6.4f), new Vector3(0.8f, PoolWater, 9.6f),
        };

        /// <summary>プレイヤーが立つ位置。左と右の通路の手前</summary>
        private static readonly Vector3 LeftStand = new Vector3(-10.5f, 0.6f, -1.5f);

        private static readonly Vector3 RightStand = new Vector3(10.5f, 0.6f, -1.5f);

        /// <summary>上の階に立つ位置。左の上の階の、エスカレーターから降りた所の手前</summary>
        private static readonly Vector3 UpperStand = new Vector3(-10.5f, UpperFloor + 0.6f, 0f);

        private static readonly Color ConcreteTint = new Color(1f, 0.97f, 0.93f);

        /// <summary>床のタイル。白く飛ばないよう、くすませる</summary>
        private static readonly Color TileTint = new Color(0.86f, 0.8f, 0.72f);

        private static readonly Color Trim = new Color(0.5f, 0.37f, 0.27f);

        private static readonly Color Steel = new Color(0.56f, 0.52f, 0.48f);

        /// <summary>店のガラスに当たった光の返り。暗く抑え、中の棚は ShopGlow で見せる</summary>
        private static readonly Color ShopGlass = new Color(0.2f, 0.2f, 0.2f);

        /// <summary>店の中の明るさ。外の日の当たり方に関係なく、ガラス越しに棚が見える</summary>
        private static readonly Color ShopGlow = new Color(0.5f, 0.5f, 0.5f);

        /// <summary>
        /// ツタの葉が自ら光る強さ。日の当たらない向きでも葉が沈まない程度。強いと、日なたで光を受けた分と足されて蛍光色に見える
        /// </summary>
        private static readonly Color IvyGlow = new Color(0.12f, 0.12f, 0.12f);

        /// <summary>ツタの葉の色。日なたで木の絵より明るく飛ばないよう、絵の色を少し落とす</summary>
        private static readonly Color IvyTint = new Color(0.82f, 0.82f, 0.8f);

        /// <summary>日差しの筋。板が 2 枚重なる真ん中で、後ろが 2 割ほど明るくなる</summary>
        private static readonly Color ShaftTint = new Color(0.5f, 0.44f, 0.33f, 0.1f);

        private static readonly Color Glass = new Color(0.7f, 0.85f, 0.85f, 0.22f);

        private static readonly Color Water = new Color(0.3f, 0.62f, 0.66f, 0.45f);

        /// <summary>通路に張った水。床のタイルが透けて見える</summary>
        private static readonly Color Shallow = new Color(0.6f, 0.78f, 0.76f, 0.12f);

        /// <summary>吹き抜けの底のタイル。水を通して見たときの青み</summary>
        private static readonly Color DeepTile = new Color(0.5f, 0.76f, 0.78f);

        private static readonly Color FoamTint = new Color(0.95f, 0.98f, 0.97f, 0.85f);

        private static readonly Color Sunlight = new Color(1f, 0.88f, 0.7f);

        /// <summary>霞。日差しの色に寄せ、遠くの壁が明るく溶ける</summary>
        private static readonly Color Haze = new Color(0.77f, 0.76f, 0.71f);

        [MenuItem("Tools/LookDev/Build Flooded Mall")]
        public static void BuildAndShoot()
        {
            Build();

            foreach (string path in LookDevShots.Shoot(ScenePath))
            {
                Debug.Log("撮った: " + path);
            }
        }

        /// <summary>
        /// 部品の prefab を作り直すと子の番号が変わり、根源3が部品の子に持たせた上書き（static batching の印と描く順）の多くが
        /// 行き先を失う。これを組み直したら、根源3も組み直す（Tools/Roots/Build Root3）
        /// </summary>
        public static void Build()
        {
            // 開いていれば中身を入れ替える。保存していない手直しは消さない
            Scene open = SceneManager.GetSceneByPath(ScenePath);
            bool reuse = open.isLoaded;
            if (reuse && open.isDirty)
                throw new InvalidOperationException(ScenePath + " に保存していない変更がある。保存するか捨ててから組み直す");

            Folder("Assets/_Project/Art", "FloodedMall");
            Folder("Assets/_Project/Scenes", "LookDev");
            Folder("Assets/_Project/Scenes/LookDev", "FloodedMall");
            AssetDatabase.Refresh();

            MakeMaterials();

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = reuse ? open : EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);

            try
            {
                foreach (GameObject root in scene.GetRootGameObjects()) Object.DestroyImmediate(root);

                // 作った物と光の設定は有効なシーンに入る。組むあいだだけこのシーンを有効にする
                SceneManager.SetActiveScene(scene);

                MakePieces();
                Lay(false);
                Lighting();
                PostProcessing(PostProfile, 0f, false);
                Cameras();

                // 焼くと、ほかに開いているシーンの物まで映り込む
                Reflections(ReflectionFolder, SceneManager.sceneCount == 1);

                EditorSceneManager.SaveScene(scene, ScenePath);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (!reuse) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>
        /// 同じ組み方で、ゲームの場所のシーンを作る。見るためのカメラは置かず、後処理は場所の優先度と軽い設定にする。
        /// 映り込みにほかのシーンの物が写らないよう、ほかのシーンを閉じた中で組み、終わったら開いていたシーンの並びを戻す。
        /// 組んだ物は全部 "Mall" の下に入れて offset だけずらし、play に遊ぶための部品を足させる
        /// </summary>
        public static void BuildPlace(string scenePath, Vector3 offset, Action<Scene, Transform> play)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene open = SceneManager.GetSceneAt(i);
                if (open.isDirty) throw new InvalidOperationException(open.path + " に保存していない変更がある。保存するか捨ててから組む");
            }

            SceneSetup[] setup = EditorSceneManager.GetSceneManagerSetup();
            string reflections = Path.ChangeExtension(scenePath, null);

            Folder("Assets/_Project/Art", "FloodedMall");
            Folder(Path.GetDirectoryName(scenePath).Replace('\\', '/'), Path.GetFileName(reflections));
            AssetDatabase.Refresh();

            MakeMaterials();
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            try
            {
                MakePieces();
                Lay(true);
                Lighting();
                PostProcessing(PlacePostProfile, PlacePriority, true);
                Reflections(reflections, true);

                // 映り込みは、組んだ物ごと動かしても箱の中の見え方は変わらない
                Transform mall = new GameObject("Mall").transform;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    if (root.transform != mall) root.transform.SetParent(mall, true);
                }
                mall.position = offset;

                Batch(mall);
                Sort(mall);
                play(scene, mall);
                EditorSceneManager.SaveScene(scene, scenePath);
            }
            finally
            {
                if (setup.Length > 0) EditorSceneManager.RestoreSceneManagerSetup(setup);
            }
        }

        /// <summary>
        /// 動かない物に static batching の印を付け、同じ素材の物をまとめて描かせる。部品が千ほどあり、そのままでは
        /// 描く回数が千を超えて Android で重い。日差しの筋は濃さを板ごとに上書きするので（ShaftDimmer）まとめない
        /// </summary>
        private static void Batch(Transform mall)
        {
            Transform beams = mall.Find("Sunbeams");

            foreach (MeshRenderer shown in mall.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (beams != null && shown.transform.IsChildOf(beams)) continue;

                GameObject part = shown.gameObject;
                GameObjectUtility.SetStaticEditorFlags(part, GameObjectUtility.GetStaticEditorFlags(part) | StaticEditorFlags.BatchingStatic);
            }
        }

        /// <summary>
        /// 透ける物の描く順を、プレイヤーと敵と同じく奥行きから決める。順が 0 のままだと、奥に居るプレイヤーの前に木やガラスや滝が重なり、
        /// 水面もプレイヤーの上に重なる。順は部品ごとに、その透ける物の真ん中の奥行きで決める。
        /// 滝と泡としぶきは同じ順になり、どれが先かは素材の描く並び（renderQueue）で決まる
        /// </summary>
        private static void Sort(Transform mall)
        {
            Material[] surfaces = { Mat("MallWaterSurface"), Mat("MallShallowSurface") };
            Dictionary<GameObject, List<Renderer>> pieces = new Dictionary<GameObject, List<Renderer>>();

            foreach (Renderer shown in mall.GetComponentsInChildren<Renderer>(true))
            {
                Material material = shown.sharedMaterial;
                if (material == null || material.renderQueue <= (int)RenderQueue.GeometryLast) continue;

                if (Array.IndexOf(surfaces, material) >= 0)
                {
                    Order(shown, SurfaceOrder);
                    continue;
                }

                GameObject piece = PrefabUtility.GetOutermostPrefabInstanceRoot(shown.gameObject);
                if (piece == null) piece = shown.gameObject;

                List<Renderer> parts;
                if (!pieces.TryGetValue(piece, out parts)) pieces[piece] = parts = new List<Renderer>();
                parts.Add(shown);
            }

            foreach (List<Renderer> parts in pieces.Values)
            {
                Bounds bounds = parts[0].bounds;
                foreach (Renderer part in parts) bounds.Encapsulate(part.bounds);

                int order = Mathf.RoundToInt(bounds.center.z * OrderPerDepth);
                foreach (Renderer part in parts) Order(part, order);
            }
        }

        /// <summary>部品の描く順を変える。スクリプトで変えた値は、記録しないとシーンの上書きに残らないことがある</summary>
        private static void Order(Renderer shown, int order)
        {
            shown.sortingOrder = order;
            PrefabUtility.RecordPrefabInstancePropertyModifications(shown);
        }

        private static void MakeMaterials()
        {
            Texture2D tile = Texture(ArtFolder + "/MallFloorTile.png", false);
            Texture2D concrete = Texture(ArtFolder + "/MallConcrete.png", false);
            Texture2D ripple = Texture(ArtFolder + "/MallWaterNormal.png", true);
            Texture2D foam = Texture(ArtFolder + "/MallFoam.png", false);
            Texture2D falling = Texture(ArtFolder + "/MallWaterfall.png", false);
            Texture2D mist = Texture(ArtFolder + "/MallMist.png", false);
            Texture2D shaft = Texture(ArtFolder + "/MallShaft.png", false);
            Texture2D shop = Texture(ArtFolder + "/MallShopInterior.png", false);

            Textured("MallTile", tile, TileTint, 0.35f);
            Textured("MallTileDeep", tile, DeepTile, 0.45f);
            Textured("MallConcrete", concrete, ConcreteTint, 0.15f);
            Textured("MallTrim", concrete, Trim, 0.2f);

            Opaque("MallSteel", Steel, 0.45f, 0.7f);
            Transparent("MallGlass", Glass, 0.9f);

            // 場所の出口の扉。奥の見えない暗がり
            Opaque("MallDoor", new Color(0.05f, 0.045f, 0.04f), 0.1f, 0f);

            // 店の中は外の光の当たり方に関係なく見えるよう、絵を自ら光らせる。ガラスの照り返しは滑らかさで出す
            Material shopGlass = Opaque("MallShopGlass", ShopGlass, 0.88f, 0f);
            shopGlass.mainTexture = shop;
            Glow(shopGlass, shop, ShopGlow);

            Additive("MallShaft", shaft, ShaftTint);
            Sky("MallSky");

            // 垂れるツタは板の上下左右で絵が切れる。柱に巻くツタは左右がつながり、柱をおよそ 2 周する
            for (int i = 0; i < IvyKinds; i++)
                Leafy("MallIvy" + i, LeafTexture(ArtFolder + "/MallIvy" + i + ".png", TextureWrapMode.Clamp));
            Leafy("MallIvyClimb", LeafTexture(ArtFolder + "/MallIvyClimb.png", TextureWrapMode.Repeat)).mainTextureScale = new Vector2(2f, 1f);

            // 水面は床の高さより下にあり、手すりのガラス・滝・しぶきより奥になる。透ける物の中で先に描いても前後は入れ替わらない。
            // 先に描くと同じ水の板が続けて描かれ、まとめて描ける（static batching）。吹き抜けの水は通路の水より奥なので、さらに先
            WaterMaterial("MallWaterSurface", Water, ripple, 0.35f).renderQueue = (int)RenderQueue.Transparent - 2;
            WaterMaterial("MallShallowSurface", Shallow, ripple, 0.25f).renderQueue = (int)RenderQueue.Transparent - 1;

            Fade("MallWaterfall", falling, Color.white, 0.7f);

            // 水面と同じ並びで描くと、どちらが先かが見る向きで入れ替わり、泡が水に隠れる。しぶきは滝の根元を覆うので、さらに後
            Fade("MallFoam", foam, FoamTint, 0.3f).renderQueue = (int)RenderQueue.Transparent + 1;
            Material spray = Fade("MallMist", mist, FoamTint, 0f);
            spray.renderQueue = (int)RenderQueue.Transparent + 2;

            // 日の当たらない向きの板も白く見えるようにする
            Glow(spray, null, new Color(0.35f, 0.37f, 0.37f));

            AssetDatabase.SaveAssets();
        }

        /// <summary>光の当たり方に関係なく、少し自ら光らせる。map が無ければ一様に光る。GI の印を外さないと、検証で光りが消される</summary>
        private static void Glow(Material material, Texture2D map, Color color)
        {
            material.EnableKeyword("_EMISSION");
            material.SetTexture("_EmissionMap", map);
            material.SetColor("_EmissionColor", color);
            material.globalIlluminationFlags = MaterialGlobalIlluminationFlags.None;
            EditorUtility.SetDirty(material);
        }

        /// <summary>葉の透け方。絵の外を抜いて描き、影も葉の形に落ちる。日の当たらない向きでも沈まないよう、少し自ら光らせる</summary>
        private static Material Leafy(string name, Texture2D texture)
        {
            Material material = MaterialAsset(name);
            material.SetFloat("_Mode", 1f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.EnableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.AlphaTest;
            material.mainTexture = texture;
            material.color = IvyTint;
            material.SetFloat("_Cutoff", IvyCutoff);
            material.SetFloat("_Glossiness", 0.1f);
            material.SetFloat("_Metallic", 0f);
            Glow(material, texture, IvyGlow);

            return material;
        }

        /// <summary>
        /// 葉の絵。縦は端で切る（上端の茂みが下端ににじまない）。across は横のつなぎ方。
        /// 抜いて描くので、遠くで縮んだときに葉が痩せて消えないよう、縮小の段ごとに覆う割合を保つ
        /// </summary>
        private static Texture2D LeafTexture(string path, TextureWrapMode across)
        {
            Texture(path, false);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

            if (!importer.mipMapsPreserveCoverage || importer.alphaTestReferenceValue != IvyCutoff
                || importer.wrapModeU != across || importer.wrapModeV != TextureWrapMode.Clamp)
            {
                importer.mipMapsPreserveCoverage = true;
                importer.alphaTestReferenceValue = IvyCutoff;
                importer.wrapModeU = across;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>後ろを明るくするだけの透け方。両面とも見える。日差しの筋のように、空気が光って見える物に使う</summary>
        private static void Additive(string name, Texture2D texture, Color tint)
        {
            Shader shader = Shader.Find(AdditiveShader);
            if (shader == null) throw new InvalidOperationException(AdditiveShader + " が無い");

            Material material = MaterialAsset(name);
            material.shader = shader;
            material.mainTexture = texture;
            material.SetColor("_TintColor", tint);
            EditorUtility.SetDirty(material);
        }

        /// <summary>天窓から見える空。厚めの大気で、霞んだ明るい空にする</summary>
        private static void Sky(string name)
        {
            Material material = MaterialAsset(name);
            material.shader = Shader.Find("Skybox/Procedural");
            material.SetFloat("_AtmosphereThickness", 1.3f);
            material.SetColor("_SkyTint", new Color(0.55f, 0.6f, 0.62f));
            material.SetColor("_GroundColor", new Color(0.42f, 0.4f, 0.37f));
            material.SetFloat("_Exposure", 1.25f);
            EditorUtility.SetDirty(material);
        }

        /// <summary>水面。さざ波の法線を 1 マスに 2 回並べる</summary>
        private static Material WaterMaterial(string name, Color color, Texture2D ripple, float bump)
        {
            Material water = Transparent(name, color, 0.95f);
            water.SetTexture("_BumpMap", ripple);
            water.SetFloat("_BumpScale", bump);
            water.EnableKeyword("_NORMALMAP");
            water.mainTextureScale = new Vector2(2f, 2f);

            return water;
        }

        private static void MakePieces()
        {
            GameObject waterfall = AssetDatabase.LoadAssetAtPath<GameObject>(ForestPrefabs + Waterfall);
            if (waterfall == null) throw new InvalidOperationException(ForestPrefabs + Waterfall + " が無い。森の素材集を入れる");

            Piece("MallFloor", root => Slab(root, "MallTile"));
            Piece("MallPoolFloor", root => Slab(root, "MallTileDeep"), true);

            // 奥（+z）が吹き抜けに面した通路。縁は水面より下まで下ろす
            Piece("MallLedge", root =>
            {
                Slab(root, "MallTile");
                Face(root, 2f, -0.9f);
                Railing(root);
                Barrier(root);
            });

            // エスカレーターの下端が載る縁。下端の幅だけ手すりと見えない壁を開ける。縁は -z を向くので、部品の x はマスの x の逆になる。
            // 開けた所の両脇は、エスカレーターの見えない壁がふさぐ
            Piece("MallLedgeOpen", root =>
            {
                Slab(root, "MallTile");
                Face(root, 2f, -0.9f);
                float open = EscalatorFoot.x * Cell - (EscalatorX + EscalatorHalfWidth);
                Railing(root, -Cell / 2f, open - 0.05f);
                Barrier(root, -Cell / 2f, open - 0.05f);
            });

            // 縁の真ん中が欠けて、床の端が水へ傾いている。手すりは片側しか残っておらず、欠けた所から通路の水がこぼれる。
            // 欠けた所に落ちた床の欠片には当たりを付け、欠けた所から水へ落ちないようにする（見えない壁は縁にしか無い）
            Piece("MallLedgeBroken", root =>
            {
                Box(root, "Cube_Floor", new Vector3(0f, -0.25f, -0.45f), new Vector3(Cell, 0.5f, 2.1f), "MallTile", true);
                Box(root, "Cube_Floor", new Vector3(-1f, -0.25f, 1.05f), new Vector3(1f, 0.5f, 0.9f), "MallTile", true);
                Box(root, "Cube_Floor", new Vector3(1f, -0.25f, 1.05f), new Vector3(1f, 0.5f, 0.9f), "MallTile", true);
                Box(root, "Cube_Fallen", new Vector3(0.05f, -0.6f, 1.05f), new Vector3(0.95f, 0.45f, 0.9f), "MallTile", true)
                    .transform.localRotation = Quaternion.Euler(25f, 0f, -8f);

                Box(root, "Cube_Face", new Vector3(-1f, -1f, 1.36f), new Vector3(1f, 2f, 0.3f), "MallConcrete", true);
                Box(root, "Cube_Face", new Vector3(0f, -1.375f, 1.36f), new Vector3(1f, 1.25f, 0.3f), "MallConcrete", true);
                Box(root, "Cube_Face", new Vector3(1f, -1f, 1.36f), new Vector3(1f, 2f, 0.3f), "MallConcrete", true);
                Box(root, "Cube_Band", new Vector3(-1f, -0.9f, 1.37f), new Vector3(1f, 0.22f, 0.32f), "MallTrim", false);
                Box(root, "Cube_Band", new Vector3(1f, -0.9f, 1.37f), new Vector3(1f, 0.22f, 0.32f), "MallTrim", false);

                RailGlass(root, new Vector3(-1f, 0.475f, 1.3f), 1f);
                Rod(root, "Railing_Handrail", new Vector3(-0.95f, 0.95f, 1.3f), new Vector3(0f, 0f, 90f), 0.05f, 1.1f, "MallSteel");
                Rod(root, "Railing_Handrail", new Vector3(-0.2f, 0.75f, 1.32f), new Vector3(0f, 0f, 60f), 0.05f, 0.6f, "MallSteel");
                Rod(root, "Railing_Handrail", new Vector3(1f, 0.85f, 1.3f), new Vector3(0f, 0f, 80f), 0.05f, 1f, "MallSteel");
                Rod(root, "Railing_Post", new Vector3(-Cell / 2f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
                Rod(root, "Railing_Post", new Vector3(Cell / 2f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
                Rod(root, "Railing_Post", new Vector3(0.6f, 0.05f, 0.9f), new Vector3(90f, 30f, 0f), 0.04f, 1f, "MallSteel");

                Rubble(root, new Vector3(0f, 0f, 0.3f), 0.6f, 5, 11, 0.15f, 0.35f);
                Rubble(root, new Vector3(0f, PoolWater, 2.2f), 0.8f, 4, 12, 0.3f, 0.6f);

                Fall(root, waterfall, -0.72f, -0.72f - PoolWater + 0.15f, 0.32f, 0.35f);
                Foam(root, new Vector3(0f, PoolWater + 0.02f, 1.8f), new Vector2(1.4f, 1f));
                Mist(root, new Vector3(0f, PoolWater, 1.75f), new Vector3(1.5f, 1f, 1f));
                Barrier(root);
            });

            // 上の階の床と縁。縁から垂れた面は浅く、下の階から吹き抜けが見える。上の階も歩け、縁には下の階と同じく見えない壁を立てる
            Piece("MallUpperFloor", root => UpperSlab(root));

            Piece("MallUpperLedge", root =>
            {
                UpperSlab(root);
                Face(root, UpperFace, -UpperFace / 2f);
                Railing(root);
                Barrier(root);
            });

            // エスカレーターの上の踊り場から降りる縁。踊り場の長さだけ手すりと見えない壁を開ける。縁は +x を向くので、部品の x はマスの z の逆になる
            Piece("MallUpperLanding", root =>
            {
                UpperSlab(root);
                Face(root, UpperFace, -UpperFace / 2f);
                float near = EscalatorHead.y * Cell - (EscalatorFootZ - EscalatorRun);
                Railing(root, -Cell / 2f, near - 0.05f);
                Railing(root, near + EscalatorHeadLength + 0.05f, Cell / 2f);
                Barrier(root, -Cell / 2f, near - 0.05f);
                Barrier(root, near + EscalatorHeadLength + 0.05f, Cell / 2f);
            });

            // 水があふれ落ちる縁。手すりは流され、両端の柱と曲がった握りだけが残る
            Piece("MallUpperSpill", root =>
            {
                UpperSlab(root);
                Face(root, UpperFace, -UpperFace / 2f);
                Rod(root, "Railing_Post", new Vector3(-Cell / 2f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
                Rod(root, "Railing_Post", new Vector3(Cell / 2f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
                Rod(root, "Railing_Handrail", new Vector3(-1.2f, 0.85f, 1.32f), new Vector3(0f, 0f, 75f), 0.05f, 0.7f, "MallSteel");
                Rod(root, "Railing_Handrail", new Vector3(1.25f, 0.9f, 1.3f), new Vector3(0f, 0f, 95f), 0.05f, 0.55f, "MallSteel");
                Barrier(root);
            });

            // 柱が折れて、上の階の床が抜け落ちた。壁側に床の縁と鉄筋が残り、落ちた床の板が下の階に傾いている。
            // 穴は囲わない。落ちると下の階の崩れた縁に降りる
            Piece("MallUpperHole", root =>
            {
                Box(root, "Cube_Floor", new Vector3(0f, -0.25f, -1.15f), new Vector3(Cell, 0.5f, 0.7f), "MallConcrete", true);
                Box(root, "Cube_Floor", new Vector3(-1.05f, -0.25f, -0.6f), new Vector3(0.9f, 0.5f, 0.6f), "MallConcrete", true)
                    .transform.localRotation = Quaternion.Euler(0f, 18f, 0f);
                Box(root, "Cube_Face", new Vector3(-1.25f, -UpperFace / 2f, 1.36f), new Vector3(0.5f, UpperFace, 0.3f), "MallConcrete", true);
                Box(root, "Cube_Band", new Vector3(-1.25f, -UpperFace / 2f, 1.37f), new Vector3(0.5f, 0.22f, 0.32f), "MallTrim", false);
                Box(root, "Cube_Face", new Vector3(1.3f, -0.6f, 1.36f), new Vector3(0.4f, 0.6f, 0.3f), "MallConcrete", true);

                for (int i = 0; i < 5; i++)
                {
                    float droop = 100f + (i * 37 % 5) * 6f;
                    Rod(root, "Rebar", new Vector3(-1.2f + i * 0.6f, -0.38f, -0.44f), new Vector3(droop, 0f, (i - 2) * 5f), 0.03f, 0.8f, "MallSteel");
                }

                Box(root, "Slab_Fallen", new Vector3(-0.4f, 1.5f - UpperFloor, 0.3f), new Vector3(1.8f, 0.4f, 3.4f), "MallConcrete", false)
                    .transform.localRotation = Quaternion.Euler(58f, 6f, 4f);
                Rod(root, "Railing_Handrail", new Vector3(-0.6f, -0.5f, 1.4f), new Vector3(0f, 0f, 50f), 0.05f, 1.6f, "MallSteel");
                Rubble(root, new Vector3(-0.2f, -UpperFloor, -0.2f), 1f, 7, 41, 0.25f, 0.6f);
            });

            // 上の階の縁から、吹き抜けの水面の下まで落ちる滝。落ちた所に泡としぶきが立つ
            Piece("MallWaterfall", root =>
            {
                Fall(root, waterfall, 0.03f, UpperFloor - PoolWater + 0.2f, 1f, 1f);
                Foam(root, new Vector3(0f, PoolWater - UpperFloor + 0.02f, 2.2f), new Vector2(3.6f, 2.4f));
                Mist(root, new Vector3(0f, PoolWater - UpperFloor, 2.4f), new Vector3(4f, 2.2f, 2f));
            });

            // 柱は湧く敵の地面にしない。折れた柱の上に湧かせない
            Piece("MallPillar", root => Pillar(root, PillarHeight), true);

            // 途中で折れた柱。上の欠片が残り、折れた先は足元に転がっている
            Piece("MallPillarBroken", root =>
            {
                Pillar(root, BrokenPillarHeight);
                Box(root, "Pillar_Top", new Vector3(0.15f, BrokenPillarHeight + 0.1f, 0f), new Vector3(0.6f, 0.5f, 0.5f), "MallConcrete", false)
                    .transform.localRotation = Quaternion.Euler(12f, 20f, -25f);
                Box(root, "Pillar_Top", new Vector3(-0.2f, BrokenPillarHeight + 0.05f, 0.1f), new Vector3(0.45f, 0.35f, 0.4f), "MallConcrete", false)
                    .transform.localRotation = Quaternion.Euler(-15f, 45f, 10f);
                Rod(root, "Pillar_Fallen", new Vector3(1.6f, 0.45f, -0.4f), new Vector3(90f, 0f, 70f), 0.95f, 2.2f, "MallConcrete");
                Rubble(root, new Vector3(0.9f, 0f, 0.6f), 0.7f, 6, 21, 0.2f, 0.5f);
            }, true);

            Piece("MallSkylight", Skylight);

            // 上れるが、敵は段の上に湧かせない
            Piece("MallEscalator", Escalator, true);

            // 垂れるツタ。1×1 の板を表裏 2 枚。原点は上端の真ん中で、表は +z を向く。置く側で幅と長さに伸ばす
            for (int i = 0; i < IvyKinds; i++)
            {
                string leaves = "MallIvy" + i;
                Piece(leaves, root =>
                {
                    foreach (float turn in new[] { 180f, 0f })
                    {
                        GameObject card = Part(PrimitiveType.Quad, root, "Ivy", Mat(leaves), false);
                        card.transform.SetLocalPositionAndRotation(new Vector3(0f, -0.5f, 0f), Quaternion.Euler(0f, turn, 0f));
                    }
                });
            }

            // 柱に巻きつくツタ。柱より少し太い筒で、原点は根元、高さ 1。置く側で巻きつく高さに伸ばす
            Piece("MallIvyClimb", root => Rod(root, "Ivy", new Vector3(0f, 0.5f, 0f), Vector3.zero, 1.06f, 1f, "MallIvyClimb"));

            // 天窓から差す日差しの筋。1×1 の板を 2 枚、筋の軸（y）のまわりに直角に組み、どこから見ても厚みが出るようにする。
            // 原点は筋の真ん中で、上（+y）が日の来る側。置く側で太さと長さに伸ばす
            Piece("MallShaft", root =>
            {
                foreach (float turn in new[] { 45f, -45f })
                {
                    GameObject card = Part(PrimitiveType.Quad, root, "Shaft", Mat("MallShaft"), false);
                    card.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
                    See(card);
                }
            });

            Piece("MallWater", root => WaterPlane(root, "MallWaterSurface"));
            Piece("MallShallow", root => WaterPlane(root, "MallShallowSurface"));

            // 手前（-z）に店のガラスを向けた壁。上の階にも店が並ぶ
            Piece("MallWall", root =>
            {
                Box(root, "Cube_Wall", new Vector3(0f, PillarHeight / 2f, 0.2f), new Vector3(Cell, PillarHeight, 0.4f), "MallConcrete", true);
                Shopfront(root, 0f);
                Shopfront(root, UpperFloor);
            });

            // 下の階に、手前の場所へ戻る扉を開けた壁。扉の奥は暗がり
            Piece("MallWallDoor", root =>
            {
                Box(root, "Cube_Wall", new Vector3(0f, PillarHeight / 2f, 0.2f), new Vector3(Cell, PillarHeight, 0.4f), "MallConcrete", true);
                Box(root, "Door", new Vector3(0f, 1.4f, -0.02f), new Vector3(2.2f, 2.8f, 0.04f), "MallDoor", false);
                Box(root, "Door_Frame", new Vector3(0f, 2.9f, -0.04f), new Vector3(2.6f, 0.22f, 0.08f), "MallTrim", false);
                Box(root, "Door_Frame", new Vector3(-1.2f, 1.4f, -0.04f), new Vector3(0.2f, 2.8f, 0.08f), "MallTrim", false);
                Box(root, "Door_Frame", new Vector3(1.2f, 1.4f, -0.04f), new Vector3(0.2f, 2.8f, 0.08f), "MallTrim", false);
                Shopfront(root, UpperFloor);
            });

            Piece("MallRubble", root => Rubble(root, Vector3.zero, 0.9f, 9, 3, 0.2f, 0.7f));

            Material dirt = AssetDatabase.LoadAssetAtPath<Material>(DirtMaterial);

            // 床に積もった土と、そこから生えた草
            Piece("MallDirtMound", root =>
            {
                Lump(root, new Vector3(0f, -0.25f, 0f), new Vector3(2.8f, 1.7f, 2.2f), dirt);
                Lump(root, new Vector3(0.8f, -0.15f, 0.5f), new Vector3(1.8f, 1.4f, 1.5f), dirt);
                Lump(root, new Vector3(-0.9f, -0.2f, -0.4f), new Vector3(1.6f, 1.1f, 1.3f), dirt);
                Lump(root, new Vector3(1.5f, -0.2f, -0.6f), new Vector3(1.1f, 0.7f, 1f), dirt);
                Weed(root, 0, new Vector3(-0.4f, 0.4f, -0.6f), 1.4f);
                Weed(root, 1, new Vector3(0.6f, 0.5f, -0.2f), 1.1f);
                Weed(root, 2, new Vector3(1.4f, 0.1f, -1.1f), 1.8f);
                Weed(root, 0, new Vector3(-1.5f, 0f, -1f), 1f);
            });

            // 床の隅に生えた草
            Piece("MallWeeds", root =>
            {
                Weed(root, 1, new Vector3(-0.3f, 0f, 0f), 1.2f);
                Weed(root, 2, new Vector3(0.4f, 0f, 0.2f), 1.6f);
                Weed(root, 0, new Vector3(0.1f, 0f, -0.3f), 1f);
            });

            // 視界確保（ViewSecurer）は名前に tree を含む物を薄くする
            Piece("MallTree", root =>
            {
                Sprite sprite = PropSprite(TreeSprite);
                const float scale = 4f;

                // 当たりはトリガーだが、湧く敵の地面を探す Raycast はトリガーにも当たる
                GameObject tree = new GameObject("tree");
                tree.layer = SeekThrough;
                tree.transform.SetParent(root, false);
                tree.transform.localScale = new Vector3(scale, scale, 1f);
                tree.transform.localPosition = new Vector3(0f, -sprite.bounds.min.y * scale, 0f);
                tree.AddComponent<SpriteRenderer>().sprite = sprite;

                BoxCollider box = tree.AddComponent<BoxCollider>();
                box.isTrigger = true;
                box.center = sprite.bounds.center;
                box.size = new Vector3(sprite.bounds.size.x, sprite.bounds.size.y, 0.2f);
            });
        }

        /// <summary>
        /// 吹き抜けを覆う、切妻のガラス屋根。柱の上に梁を回し、棟へ垂木を架ける。ガラスは所々割れて抜けている。
        /// 原点は手前と奥の柱の真ん中の、柱の上端
        /// </summary>
        private static void Skylight(Transform root)
        {
            const int bays = 8;
            float length = PillarBackZ - PillarFrontZ;
            float bay = length / bays;
            float slope = Mathf.Sqrt(PillarX * PillarX + RidgeRise * RidgeRise);
            float pitch = Mathf.Atan2(RidgeRise, PillarX) * Mathf.Rad2Deg;
            const float eave = 0.5f;

            Box(root, "Beam", new Vector3(-PillarX, eave / 2f, 0f), new Vector3(0.6f, eave, length + 0.6f), "MallConcrete", false);
            Box(root, "Beam", new Vector3(PillarX, eave / 2f, 0f), new Vector3(0.6f, eave, length + 0.6f), "MallConcrete", false);
            Box(root, "Beam", new Vector3(0f, eave / 2f, -length / 2f), new Vector3(PillarX * 2f, eave, 0.6f), "MallConcrete", false);
            Box(root, "Beam", new Vector3(0f, eave / 2f, length / 2f), new Vector3(PillarX * 2f, eave, 0.6f), "MallConcrete", false);
            Box(root, "Ridge", new Vector3(0f, eave + RidgeRise, 0f), new Vector3(0.25f, 0.3f, length), "MallSteel", false);

            Random random = new Random(7);

            for (int side = -1; side <= 1; side += 2)
            {
                Quaternion tilt = Quaternion.Euler(0f, 0f, -side * pitch);

                for (int i = 0; i <= bays; i++)
                {
                    Vector3 middle = new Vector3(side * PillarX / 2f, eave + RidgeRise / 2f, -length / 2f + i * bay);
                    Box(root, "Rafter", middle, new Vector3(slope, 0.16f, 0.12f), "MallSteel", false).transform.localRotation = tilt;
                }

                Vector3 purlin = new Vector3(side * PillarX / 2f, eave + RidgeRise / 2f, 0f);
                Box(root, "Purlin", purlin, new Vector3(0.1f, 0.12f, length), "MallSteel", false).transform.localRotation = tilt;

                // 軒から棟へ 2 枚ずつ。割れて抜けた枠を残す
                for (int i = 0; i < bays; i++)
                {
                    for (int step = 0; step < 2; step++)
                    {
                        if (random.NextDouble() < 0.3) continue;

                        float along = 0.25f + step * 0.5f;
                        Vector3 at = new Vector3(side * PillarX * (1f - along), eave + RidgeRise * along, -length / 2f + (i + 0.5f) * bay);
                        GameObject pane = Box(root, "Skylight_Glass", at, new Vector3(slope / 2f - 0.08f, 0.02f, bay - 0.1f), "MallGlass", false);
                        pane.transform.localRotation = tilt;
                        See(pane);
                    }
                }
            }
        }

        /// <summary>
        /// 左右の通路と、それをつなぐ奥の通路。真ん中の手前は吹き抜けの水で、渡るには奥を回る。
        /// 上の階も同じ形で重なり、その縁のいくつかから吹き抜けへ滝が落ちる。
        /// まわりを店の壁で囲い、柱で天窓を支え、瓦礫と土と草を置く
        /// </summary>
        /// <param name="place">ゲームの場所として組む。左の壁の手前の 1 枚を、手前の場所へ戻る扉にする</param>
        private static void Lay(bool place)
        {
            GameObject floor = Prefab("MallFloor");
            GameObject ledge = Prefab("MallLedge");
            GameObject ledgeBroken = Prefab("MallLedgeBroken");
            GameObject ledgeOpen = Prefab("MallLedgeOpen");
            GameObject upperFloor = Prefab("MallUpperFloor");
            GameObject upperLedge = Prefab("MallUpperLedge");
            GameObject upperLanding = Prefab("MallUpperLanding");
            GameObject upperSpill = Prefab("MallUpperSpill");
            GameObject upperHole = Prefab("MallUpperHole");
            GameObject waterfall = Prefab("MallWaterfall");
            GameObject poolFloor = Prefab("MallPoolFloor");
            GameObject water = Prefab("MallWater");
            GameObject shallow = Prefab("MallShallow");
            GameObject wall = Prefab("MallWall");
            GameObject pillar = Prefab("MallPillar");
            GameObject pillarBroken = Prefab("MallPillarBroken");
            GameObject skylight = Prefab("MallSkylight");
            GameObject rubble = Prefab("MallRubble");
            GameObject mound = Prefab("MallDirtMound");
            GameObject weeds = Prefab("MallWeeds");
            GameObject tree = Prefab("MallTree");
            GameObject shaft = Prefab("MallShaft");

            Transform walkways = new GameObject("Walkways").transform;
            Transform upper = new GameObject("Upper Walkways").transform;
            Transform atrium = new GameObject("Atrium").transform;
            Transform falls = new GameObject("Waterfalls").transform;
            Transform walls = new GameObject("Walls").transform;
            Transform ruins = new GameObject("Ruins").transform;
            Transform nature = new GameObject("Nature").transform;

            for (int column = -Half; column <= Half; column++)
            {
                for (int row = FrontRow; row <= BackRow; row++)
                {
                    Vector2Int cell = new Vector2Int(column, row);
                    Vector3 at = new Vector3(column * Cell, 0f, row * Cell);
                    Vector3 above = at + Vector3.up * UpperFloor;

                    if (InAtrium(column, row))
                    {
                        Place(poolFloor, atrium, at + Vector3.up * PoolFloor, 0f);
                        Place(water, atrium, at + Vector3.up * PoolWater, 0f);
                        continue;
                    }

                    // 同じ床が並んで見えないよう、マスごとに 90 度ずつ回す
                    float turn = 90f * ((column * 7 + row * 13) & 3);
                    float edge;
                    bool facing = FacesAtrium(column, row, out edge);

                    if (!facing) Place(floor, walkways, at, turn);
                    else if (cell == EscalatorFoot) Place(ledgeOpen, walkways, at, edge);
                    else Place(Array.IndexOf(BrokenLedges, cell) >= 0 ? ledgeBroken : ledge, walkways, at, edge);

                    Place(shallow, walkways, at + Vector3.up * ShallowWater, 0f);

                    if (Array.IndexOf(UpperHoles, cell) >= 0)
                    {
                        Place(upperHole, upper, above, edge);
                        continue;
                    }

                    int spill = Array.IndexOf(Spills, cell);

                    if (!facing) Place(upperFloor, upper, above, turn);
                    else if (cell == EscalatorHead) Place(upperLanding, upper, above, edge);
                    else if (spill < 0) Place(upperLedge, upper, above, edge);
                    else
                    {
                        Place(upperSpill, upper, above, edge);
                        Place(waterfall, falls, above, edge).transform.localScale = new Vector3(SpillWidths[spill], 1f, 1f);
                    }

                    Place(shallow, upper, above + Vector3.up * ShallowWater, 0f);
                }
            }

            for (int column = -Half; column <= Half; column++)
            {
                Place(wall, walls, new Vector3(column * Cell, 0f, (BackRow + 0.5f) * Cell), 0f);
            }

            for (int row = FrontRow; row <= BackRow; row++)
            {
                GameObject left = place && row == FrontRow ? Prefab("MallWallDoor") : wall;
                Place(left, walls, new Vector3(-(Half + 0.5f) * Cell, 0f, row * Cell), -90f);
                Place(wall, walls, new Vector3((Half + 0.5f) * Cell, 0f, row * Cell), 90f);
            }

            // 手前は壁が無く、左右の通路は上下の階とも端で床が切れる。場所では敵が落ちないよう、端に見えない壁を立てる
            if (place) FrontBarriers(walls);

            // 奥の通路の縁から、吹き抜けの左の縁に沿って手前へ上る
            Place(Prefab("MallEscalator"), walkways, new Vector3(EscalatorX, 0f, EscalatorFootZ), 180f);

            foreach (Vector3 at in Pillars) Place(pillar, ruins, at, 0f);
            foreach (Vector3 at in BrokenPillars) Place(pillarBroken, ruins, at, 0f);
            foreach (Vector3 at in RubblePiles) Place(rubble, ruins, at, 0f);
            Place(skylight, ruins, new Vector3(0f, PillarHeight, (PillarFrontZ + PillarBackZ) / 2f), 0f);

            for (int i = 0; i < Stones.Length; i++)
            {
                GameObject rock = AssetDatabase.LoadAssetAtPath<GameObject>(ForestPrefabs + Rocks[i % Rocks.Length]);
                SeekThroughAll(Place(rock, ruins, Stones[i], 70f * i));
            }

            foreach (Vector3 at in Mounds) Place(mound, nature, at, 0f);
            foreach (Vector3 at in Weeds) Place(weeds, nature, at, 0f);
            foreach (Vector3 at in Trees) Place(tree, nature, at, 0f);

            Sunbeams(shaft);
            Overgrowth();
        }

        /// <summary>
        /// ツタ。上の階と下の階の縁、天窓の梁、奥の壁の上端から垂らし、柱には根元から巻きつける。
        /// 絵の種類と幅と長さは番号から決め、1 枚おきに左右を返して、同じ形が並んで見えないようにする
        /// </summary>
        private static void Overgrowth()
        {
            Transform ivy = new GameObject("Ivy").transform;
            GameObject[] kinds = new GameObject[IvyKinds];
            for (int i = 0; i < IvyKinds; i++) kinds[i] = Prefab("MallIvy" + i);

            int n = 0;
            foreach (Vector2Int cell in UpperIvy) HangFromEdge(ivy, kinds, n++, cell, UpperFloor + 0.12f, 1.8f, 3.4f);
            foreach (Vector2Int cell in LowerIvy) HangFromEdge(ivy, kinds, n++, cell, 0.08f, 1.2f, 1.7f);

            // 梁は柱の上端から 0.5 の高さ。その内側の面に沿わせる
            float beamTop = PillarHeight + 0.5f;
            foreach (float x in BackBeamIvy) Hang(ivy, kinds, n++, new Vector3(x, beamTop, PillarBackZ - 0.33f), 180f, 2f, 3.4f);
            foreach (Vector2 at in SideBeamIvy)
                Hang(ivy, kinds, n++, new Vector3(at.x * (PillarX - 0.33f), beamTop, at.y), -at.x * 90f, 2f, 3.4f);

            // 奥の壁の表は店のガラスと看板の帯が 1〜2cm 前にある。その前へ離す
            float wallFace = (BackRow + 0.5f) * Cell - 0.06f;
            foreach (float x in WallIvy) Hang(ivy, kinds, n++, new Vector3(x, PillarHeight, wallFace), 180f, 1.5f, 3f);

            // エスカレーターの上の踊り場の、吹き抜けの側から。エスカレーターは向きを返して置くので、部品の -x が +x になる
            float landing = EscalatorFootZ - EscalatorRun - EscalatorHeadLength / 2f;
            Hang(ivy, kinds, n++, new Vector3(EscalatorX + EscalatorHalfWidth + 0.03f, UpperFloor + 0.05f, landing), 90f, 1.8f, 2.6f);

            GameObject climb = Prefab("MallIvyClimb");
            for (int i = 0; i < ClimbingIvy.Length; i++)
                Place(climb, ivy, ClimbingIvy[i], 73f * i).transform.localScale = new Vector3(1f, ClimbHeights[i], 1f);
        }

        /// <summary>吹き抜けに面したマスの縁から垂らす。top はツタの上端の高さ</summary>
        private static void HangFromEdge(Transform parent, GameObject[] kinds, int index, Vector2Int cell, float top, float shortest, float longest)
        {
            float edge;
            if (!FacesAtrium(cell.x, cell.y, out edge)) throw new InvalidOperationException(cell + " は吹き抜けに面していない");

            Vector3 lip = Quaternion.Euler(0f, edge, 0f) * new Vector3(0f, 0f, IvyLip);
            Hang(parent, kinds, index, new Vector3(cell.x * Cell, top, cell.y * Cell) + lip, edge, shortest, longest);
        }

        /// <summary>垂れるツタを 1 枚。top は上端の真ん中、turn は表の向き（度）</summary>
        private static void Hang(Transform parent, GameObject[] kinds, int index, Vector3 top, float turn, float shortest, float longest)
        {
            Random random = new Random(100 + index);
            float width = Range(random, 1.4f, 2.6f) * (index % 2 == 0 ? 1f : -1f);
            float length = Range(random, shortest, longest);
            Place(kinds[index % kinds.Length], parent, top, turn).transform.localScale = new Vector3(width, length, 1f);
        }

        /// <summary>天窓のガラスのすぐ下から、日の向きに吹き抜けの水面まで筋を伸ばす</summary>
        private static void Sunbeams(GameObject shaft)
        {
            Transform beams = new GameObject("Sunbeams").transform;
            Vector3 down = Sun * Vector3.forward;
            Vector3 up = -down;

            // 板の面は、筋の軸を保ったまま手前（-z）のカメラへ向ける
            Quaternion along = Quaternion.LookRotation(Vector3.ProjectOnPlane(Vector3.back, up).normalized, up);

            for (int i = 0; i < Shafts.Length; i++)
            {
                Vector2 at = Shafts[i];
                float roof = PillarHeight + 0.1f + RidgeRise * (1f - Mathf.Abs(at.x) / PillarX);
                float length = (roof - PoolWater) / up.y;

                GameObject beam = (GameObject)PrefabUtility.InstantiatePrefab(shaft, beams);
                beam.transform.SetPositionAndRotation(new Vector3(at.x, roof, at.y) + down * (length / 2f), along);
                beam.transform.localScale = new Vector3(ShaftWidths[i], length, ShaftWidths[i]);
            }
        }

        private static bool InAtrium(int column, int row)
        {
            return Math.Abs(column) <= AtriumHalf && row <= AtriumBackRow;
        }

        /// <summary>吹き抜けに面したマスか。面していれば、縁を吹き抜けへ向ける回転（度）</summary>
        private static bool FacesAtrium(int column, int row, out float turn)
        {
            turn = 0f;

            if (row <= AtriumBackRow && column == -AtriumHalf - 1) turn = 90f;
            else if (row <= AtriumBackRow && column == AtriumHalf + 1) turn = -90f;
            else if (row == AtriumBackRow + 1 && Math.Abs(column) <= AtriumHalf) turn = 180f;
            else return false;

            return true;
        }

        /// <summary>天窓から差す日差し。空と霧はこのシーンが持つ</summary>
        private static void Lighting()
        {
            GameObject sun = new GameObject("Sun");
            sun.transform.rotation = Sun;

            Light light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = Sunlight;
            light.intensity = 1.45f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.85f;

            RenderSettings.sun = light;
            RenderSettings.skybox = Mat("MallSky");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.55f, 0.62f, 0.68f);
            RenderSettings.ambientEquatorColor = new Color(0.45f, 0.42f, 0.38f);

            // 上の階の床の裏は下しか向かない。日の当たった床と水からの照り返しの代わりに、下からの明かりを強めにする
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.33f, 0.29f);
            // 空気の厚み。吹き抜けの差し渡し（約 30）で 3 割ほど霞む
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = Haze;
            RenderSettings.fogDensity = 0.012f;
        }

        /// <summary>
        /// 後処理。色を暖かく締め、明るい水と日差しをにじませ、物の際を暗くして奥行きを出し、四隅を落とす。
        /// 範囲は Post Processing のレイヤーに置き、カメラ（Eye）はそのレイヤーだけを見る。
        /// light なら Android でも重くならないよう、物の際を暗くする効果を切り、にじみを軽い作りにする
        /// </summary>
        private static void PostProcessing(string profilePath, float priority, bool light)
        {
            int layer = LayerMask.NameToLayer(PostLayer);
            if (layer < 0) throw new InvalidOperationException(PostLayer + " のレイヤーが無い");

            PostProcessProfile profile = PostProfiles.Load(profilePath);

            ColorGrading grading = PostProfiles.Effect<ColorGrading>(profile);
            grading.gradingMode.Override(GradingMode.HighDefinitionRange);
            grading.tonemapper.Override(Tonemapper.ACES);
            grading.postExposure.Override(0.35f);
            grading.temperature.Override(5f);

            // 暖かい日差しと青緑の浅い水が重なると、床が緑がかる。少しだけ赤紫へ戻す
            grading.tint.Override(-4f);
            grading.saturation.Override(8f);
            grading.contrast.Override(12f);

            Bloom bloom = PostProfiles.Effect<Bloom>(profile);
            bloom.intensity.Override(1f);
            bloom.threshold.Override(1.05f);
            bloom.softKnee.Override(0.6f);
            bloom.diffusion.Override(7.5f);
            bloom.color.Override(new Color(1f, 0.95f, 0.85f));
            bloom.fastMode.Override(light);

            AmbientOcclusion occlusion = PostProfiles.Effect<AmbientOcclusion>(profile);
            occlusion.mode.Override(AmbientOcclusionMode.MultiScaleVolumetricObscurance);
            occlusion.intensity.Override(0.8f);
            occlusion.thicknessModifier.Override(1.2f);
            occlusion.enabled.Override(!light);

            Vignette vignette = PostProfiles.Effect<Vignette>(profile);
            vignette.intensity.Override(0.28f);
            vignette.smoothness.Override(0.45f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();

            GameObject volume = new GameObject("Post Processing");
            volume.layer = layer;
            PostProcessVolume range = volume.AddComponent<PostProcessVolume>();
            range.isGlobal = true;
            range.priority = priority;
            range.sharedProfile = profile;
        }

        /// <summary>
        /// 映り込みを箱ごとに置く。bake なら今のシーンから焼き直し、そうでなければ前に焼いた絵をそのまま使う。
        /// 焼いた絵は folder（シーンの名前のフォルダ。組む側が作っておく）に置く。
        /// 置いただけの物も写すよう、動く物も写す設定にする（焼く対象は本来、静的と印を付けた物だけ）
        /// </summary>
        private static void Reflections(string folder, bool bake)
        {
            Transform group = new GameObject("Reflections").transform;

            for (int i = 0; i < Mirrors.Length; i++)
            {
                GameObject holder = new GameObject(MirrorNames[i]);
                holder.transform.SetParent(group, false);
                holder.transform.position = Mirrors[i].center;

                ReflectionProbe probe = holder.AddComponent<ReflectionProbe>();
                probe.mode = ReflectionProbeMode.Custom;
                probe.boxProjection = true;
                probe.size = Mirrors[i].size;
                probe.resolution = 128;
                probe.hdr = true;
                probe.blendDistance = 1f;

                SerializedObject settings = new SerializedObject(probe);
                settings.FindProperty("m_RenderDynamicObjects").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();

                string path = folder + "/" + MirrorNames[i] + ".exr";
                if (bake && !Lightmapping.BakeReflectionProbe(probe, path))
                    throw new InvalidOperationException(path + " に映り込みを焼けなかった");

                TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
                if (importer == null)
                {
                    Debug.LogWarning(path + " が無い。そのシーンだけを開いて組み直すと焼ける");
                    continue;
                }

                if (importer.textureShape != TextureImporterShape.TextureCube)
                {
                    importer.textureShape = TextureImporterShape.TextureCube;
                    importer.SaveAndReimport();
                }

                probe.customBakedTexture = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            }
        }


        /// <summary>
        /// 左右の通路と左の上の階に立ったときの遊ぶときのカメラ、全体を見渡すカメラ、吹き抜けの手前から奥の滝と上の階を見るカメラ、
        /// 右の上の階から吹き抜け越しにエスカレーターの段を見下ろすカメラ。左のほかは止めておき、撮るときだけ使う
        /// </summary>
        private static void Cameras()
        {
            Quaternion look = Quaternion.Euler(PlayerView.Pitch, 0f, 0f);

            Camera left = Eye("Game Camera", LeftStand + PlayerView.Offset, look);
            left.gameObject.tag = "MainCamera";

            Eye("Game Camera Right", RightStand + PlayerView.Offset, look).enabled = false;
            Eye("Game Camera Upper", UpperStand + PlayerView.Offset, look).enabled = false;

            Vector3 overview = new Vector3(0f, 18f, -15f);
            Vector3 center = new Vector3(0f, PoolWater, (BackRow + FrontRow) * Cell / 2f);
            Eye("Overview Camera", overview, Quaternion.LookRotation(center - overview)).enabled = false;

            Vector3 eye = new Vector3(0f, 1.2f, -3f);
            Vector3 aim = new Vector3(0f, 3f, 12f);
            Eye("Atrium Camera", eye, Quaternion.LookRotation(aim - eye)).enabled = false;

            Vector3 across = new Vector3(11f, UpperFloor + 2f, 4f);
            Vector3 escalator = new Vector3(EscalatorX, UpperFloor / 2f, EscalatorFootZ - EscalatorRun / 2f);
            Eye("Escalator Camera", across, Quaternion.LookRotation(escalator - across)).enabled = false;
        }

        private static Camera Eye(string name, Vector3 position, Quaternion rotation)
        {
            GameObject eye = new GameObject(name);
            eye.transform.SetPositionAndRotation(position, rotation);

            Camera camera = eye.AddComponent<Camera>();
            camera.fieldOfView = PlayerView.FieldOfView;
            camera.nearClipPlane = 0.3f;
            camera.farClipPlane = 300f;
            camera.clearFlags = CameraClearFlags.Skybox;
            camera.allowHDR = true;

            PostProcessResources resources = AssetDatabase.LoadAssetAtPath<PostProcessResources>(PostResources);
            if (resources == null) throw new InvalidOperationException(PostResources + " が無い。PPv2 のパッケージを入れる");

            PostProcessLayer post = eye.AddComponent<PostProcessLayer>();
            post.Init(resources);
            post.volumeTrigger = eye.transform;
            post.volumeLayer = LayerMask.GetMask(PostLayer);
            post.antialiasingMode = PostProcessLayer.Antialiasing.SubpixelMorphologicalAntialiasing;
            post.subpixelMorphologicalAntialiasing.quality = SubpixelMorphologicalAntialiasing.Quality.High;

            return camera;
        }

        private static void Folder(string parent, string name)
        {
            if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
        }

        /// <summary>取り込み済みの絵。法線の絵なら、取り込みの設定を法線に直す</summary>
        private static Texture2D Texture(string path, bool normal)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new InvalidOperationException(path + " が無い。先に絵を置く");

            TextureImporterType type = normal ? TextureImporterType.NormalMap : TextureImporterType.Default;
            if (importer.textureType != type || importer.anisoLevel != 8)
            {
                importer.textureType = type;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Root1 と同じ絵から、木や草を 1 枚取り出す</summary>
        private static Sprite PropSprite(long localId)
        {
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GUIDToAssetPath(PropsSheetGuid)))
            {
                string guid;
                long id;
                if (asset is Sprite sprite && AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite, out guid, out id) && id == localId)
                    return sprite;
            }

            throw new InvalidOperationException("木と草の絵が見つからない: " + localId);
        }

        private static void Textured(string name, Texture2D texture, Color color, float smoothness)
        {
            Opaque(name, color, smoothness, 0f).mainTexture = texture;
        }

        private static Material Opaque(string name, Color color, float smoothness, float metallic)
        {
            Material material = MaterialAsset(name);
            material.SetFloat("_Mode", 0f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.Zero);
            material.SetInt("_ZWrite", 1);
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = -1;
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", metallic);
            EditorUtility.SetDirty(material);

            return material;
        }

        private static Material Transparent(string name, Color color, float smoothness)
        {
            Material material = MaterialAsset(name);
            material.SetFloat("_Mode", 3f);
            material.SetInt("_SrcBlend", (int)BlendMode.One);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.DisableKeyword("_ALPHABLEND_ON");
            material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// <summary>照り返しごと薄くなる透け方。滝や泡のように、形そのものが消えていく物に使う</summary>
        private static Material Fade(string name, Texture2D texture, Color color, float smoothness)
        {
            Material material = MaterialAsset(name);
            material.SetFloat("_Mode", 2f);
            material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            material.SetInt("_ZWrite", 0);
            material.DisableKeyword("_ALPHATEST_ON");
            material.EnableKeyword("_ALPHABLEND_ON");
            material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            material.renderQueue = (int)RenderQueue.Transparent;
            material.mainTexture = texture;
            material.color = color;
            material.SetFloat("_Glossiness", smoothness);
            material.SetFloat("_Metallic", 0f);
            EditorUtility.SetDirty(material);

            return material;
        }

        /// <summary>在れば書き換える。作り直すと GUID が変わり、部品とシーンからの参照が切れる</summary>
        private static Material MaterialAsset(string name)
        {
            string path = ArtFolder + "/" + name + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;

            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material, path);

            return material;
        }

        private static Material Mat(string name)
        {
            return AssetDatabase.LoadAssetAtPath<Material>(ArtFolder + "/" + name + ".mat");
        }

        private static GameObject Prefab(string name)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(ArtFolder + "/" + name + ".prefab");
        }

        /// <summary>
        /// 部品を組んで prefab に書く。上書きしても GUID は変わらない。
        /// seekThrough なら丸ごと、湧く敵の地面を探す Raycast が素通りするレイヤーにする（上の階や吹き抜けの底に湧かせない）
        /// </summary>
        private static void Piece(string name, Action<Transform> build, bool seekThrough = false)
        {
            GameObject root = new GameObject(name);

            try
            {
                build(root.transform);
                if (seekThrough) SeekThroughAll(root);
                PrefabUtility.SaveAsPrefabAsset(root, ArtFolder + "/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        /// <summary>丸ごと、湧く敵の地面を探す Raycast が素通りするレイヤーにする</summary>
        private static GameObject SeekThroughAll(GameObject root)
        {
            foreach (Transform part in root.GetComponentsInChildren<Transform>(true)) part.gameObject.layer = SeekThrough;

            return root;
        }

        /// <summary>
        /// 吹き抜けの縁に立てる見えない壁。手すりは跳べば越えられる高さで、崩れた縁や手すりの切れ目からは水へ落ちる。
        /// 落ちると場の外とみなされて連れ戻されるので、縁の内側で止める
        /// </summary>
        private static void Barrier(Transform root)
        {
            Barrier(root, -Cell / 2f, Cell / 2f);
        }

        /// <summary>縁の見えない壁の一部だけ。x が from から to まで。手すりを開けた所は開けたままにする</summary>
        private static void Barrier(Transform root, float from, float to)
        {
            InvisibleWall(root, new Vector3((from + to) / 2f, BarrierHeight / 2f, 1.45f), new Vector3(to - from, BarrierHeight, 0.1f));
        }

        /// <summary>左右の通路の手前の端に、上下の階とも立てる見えない壁。吹き抜けの手前へは、縁の見えない壁で通路から出られない</summary>
        private static void FrontBarriers(Transform parent)
        {
            foreach (float floor in new[] { 0f, UpperFloor })
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Vector3 center = new Vector3(side * (Half + AtriumHalf + 1) * Cell / 2f, floor + BarrierHeight / 2f, (FrontRow - 0.5f) * Cell - 0.05f);
                    InvisibleWall(parent, center, new Vector3((Half - AtriumHalf) * Cell, BarrierHeight, 0.1f));
                }
            }
        }

        /// <summary>見えない壁を 1 枚。湧く地面を探す Raycast は素通りさせ、壁の上に湧かせない</summary>
        private static GameObject InvisibleWall(Transform parent, Vector3 center, Vector3 size)
        {
            GameObject wall = new GameObject("Barrier");
            wall.transform.SetParent(parent, false);
            wall.transform.localPosition = center;
            wall.layer = SeekThrough;
            wall.AddComponent<BoxCollider>().size = size;

            return wall;
        }

        private static GameObject Place(GameObject prefab, Transform parent, Vector3 position, float turn)
        {
            GameObject placed = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            placed.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, turn, 0f));

            return placed;
        }

        /// <summary>上面が 0 の、1 マスの床</summary>
        private static void Slab(Transform root, string material)
        {
            Box(root, "Cube_Floor", new Vector3(0f, -0.25f, 0f), new Vector3(Cell, 0.5f, Cell), material, true);
        }

        /// <summary>上の階の床。上面はタイル、下の階から見える裏と厚みはコンクリート</summary>
        private static void UpperSlab(Transform root)
        {
            Box(root, "Cube_Floor", new Vector3(0f, -0.03f, 0f), new Vector3(Cell, 0.06f, Cell), "MallTile", true);
            Box(root, "Cube_Slab", new Vector3(0f, -0.28f, 0f), new Vector3(Cell, 0.44f, Cell), "MallConcrete", true);
        }

        /// <summary>
        /// 吹き抜けに向いた縁の面と、茶色の帯。床の端と面が同じ所にあるとちらつくので、面は床より 1cm、帯は面より 2cm 前に出す
        /// </summary>
        private static void Face(Transform root, float depth, float band)
        {
            Box(root, "Cube_Face", new Vector3(0f, -depth / 2f, 1.36f), new Vector3(Cell, depth, 0.3f), "MallConcrete", true);
            Box(root, "Cube_Band", new Vector3(0f, band, 1.37f), new Vector3(Cell, 0.22f, 0.32f), "MallTrim", false);
        }

        /// <summary>縁のガラスの手すり。隣のマスと柱を分け合うので、柱は片側と真ん中だけ</summary>
        private static void Railing(Transform root)
        {
            RailGlass(root, new Vector3(0f, 0.475f, 1.3f), Cell);
            Rod(root, "Railing_Handrail", new Vector3(0f, 0.95f, 1.3f), new Vector3(0f, 0f, 90f), 0.05f, Cell, "MallSteel");
            Rod(root, "Railing_Post", new Vector3(-Cell / 2f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
            Rod(root, "Railing_Post", new Vector3(0f, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
        }

        /// <summary>縁の手すりの一部だけ。x が from から to まで。両端に柱を立てる</summary>
        private static void Railing(Transform root, float from, float to)
        {
            float middle = (from + to) / 2f;
            float length = to - from;

            RailGlass(root, new Vector3(middle, 0.475f, 1.3f), length);
            Rod(root, "Railing_Handrail", new Vector3(middle, 0.95f, 1.3f), new Vector3(0f, 0f, 90f), 0.05f, length, "MallSteel");
            Rod(root, "Railing_Post", new Vector3(from, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
            Rod(root, "Railing_Post", new Vector3(to, 0.5f, 1.3f), Vector3.zero, 0.04f, 1f, "MallSteel");
        }

        /// <summary>手すりのガラス。当たりはあるが、湧く敵の地面を探す Raycast は素通りさせ、手すりの上に湧かせない</summary>
        private static void RailGlass(Transform root, Vector3 center, float length)
        {
            GameObject glass = Box(root, "Railing_Glass", center, new Vector3(length, 0.85f, 0.03f), "MallGlass", true);
            glass.layer = SeekThrough;
            See(glass);
        }

        /// <summary>
        /// 止まったエスカレーター。原点は下端の縁の真ん中で、+z へ上る。段は 2 枚抜けていて、板が渡してある。
        /// 下の踊り場は両側に、上の踊り場は吹き抜けの側（-x）にだけ手すりがあり、上の階の側（+x）へ降りられる。
        /// 段は見せるだけで、歩くのは段の上に張った見えない坂。手すりの外と、上の踊り場の吹き抜けの側と先に見えない壁を立てる。
        /// 骨組みの真ん中あたりを、吹き抜けの底から立てた脚が支える
        /// </summary>
        private static void Escalator(Transform root)
        {
            float rise = UpperFloor;
            float run = EscalatorRun;
            float slope = Mathf.Sqrt(run * run + rise * rise);
            float width = EscalatorHalfWidth * 2f;
            float rail = EscalatorHalfWidth - 0.2f;
            float stepRise = rise / EscalatorSteps;
            float stepRun = run / EscalatorSteps;
            Quaternion tilt = Quaternion.Euler(-EscalatorPitch, 0f, 0f);
            Vector3 normal = tilt * Vector3.up;
            Vector3 middle = new Vector3(0f, rise / 2f, run / 2f);

            // 踊り場の上面は床と浅い水よりわずかに高くし、重なってちらつかないようにする。上の踊り場は上の階の縁へ少しかける
            float lip = width / 2f + 0.15f;
            Box(root, "Landing_Foot", new Vector3(0f, 0.02f, -0.6f), new Vector3(width, 0.1f, 1.2f), "MallSteel", true);
            Box(root, "Landing_Head", new Vector3((lip - width / 2f) / 2f, rise - 0.03f, run + EscalatorHeadLength / 2f),
                new Vector3(width / 2f + lip, 0.1f, EscalatorHeadLength), "MallSteel", true);

            Box(root, "Truss", middle - normal * 0.45f, new Vector3(width, 0.7f, slope), "MallConcrete", true).transform.localRotation = tilt;
            Box(root, "Truss_Band", middle - normal * 0.45f, new Vector3(width + 0.02f, 0.18f, slope), "MallTrim", false).transform.localRotation = tilt;

            for (int i = 0; i < EscalatorSteps; i++)
            {
                if (i >= BrokenStep && i < BrokenStep + BrokenSteps) continue;

                Vector3 at = new Vector3(0f, (i + 0.5f) * stepRise, (i + 0.5f) * stepRun);
                Box(root, "Step", at, new Vector3(rail * 2f - 0.1f, stepRise, stepRun), "MallSteel", false);
            }

            // 歩く坂。段の踏み面の真ん中を結ぶ面で、段の角に足を取られない。下端は床に、上端は上の踊り場の高さに合わせる
            Vector3 foot = new Vector3(0f, 0f, -stepRun / 2f);
            Vector3 head = new Vector3(0f, rise + 0.02f, (rise + 0.02f) * run / rise - stepRun / 2f);
            GameObject ramp = new GameObject("Ramp");
            ramp.transform.SetParent(root, false);
            ramp.transform.SetLocalPositionAndRotation((foot + head) / 2f - normal * 0.1f, tilt);
            ramp.AddComponent<BoxCollider>().size = new Vector3(rail * 2f, 0.2f, Vector3.Distance(foot, head));

            // 抜けた段に渡した板。坂の面に載せ、少し斜めに置く
            float gap = (BrokenStep + BrokenSteps / 2f) * stepRun;
            Vector3 onRamp = new Vector3(0f, (gap + stepRun / 2f) * rise / run, gap);
            float span = (BrokenSteps + 1) * stepRun / Mathf.Cos(EscalatorPitch * Mathf.Deg2Rad);
            Box(root, "Plank", onRamp + normal * 0.03f, new Vector3(rail * 2f - 0.25f, 0.06f, span), "MallTrim", false)
                .transform.localRotation = tilt * Quaternion.Euler(0f, 4f, 0f);

            // 手すりの外。坂に沿って両側に立て、吹き抜けへ落ちないようにする
            float outside = EscalatorHalfWidth + 0.05f;
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 wall = middle + new Vector3(side * outside, 0f, 0f) + normal * (BarrierHeight / 2f);
                InvisibleWall(root, wall, new Vector3(0.1f, BarrierHeight, slope)).transform.localRotation = tilt;
            }

            // 上の踊り場の吹き抜けの側と、その先。坂の見えない壁は坂に直角に切れ、上端の上に隙間が残るので、坂の上のほうまで重ねる
            const float overlap = 2f;
            InvisibleWall(root, new Vector3(-outside, rise + 0.5f, run + (EscalatorHeadLength - overlap) / 2f),
                new Vector3(0.1f, BarrierHeight + 1.5f, EscalatorHeadLength + overlap));
            InvisibleWall(root, new Vector3((lip - outside) / 2f, rise + BarrierHeight / 2f, run + EscalatorHeadLength + 0.05f),
                new Vector3(lip + outside, BarrierHeight, 0.1f));

            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 along = middle + new Vector3(side * rail, 0f, 0f);
                GameObject glass = Box(root, "Balustrade_Glass", along + normal * 0.55f, new Vector3(0.03f, 0.85f, slope), "MallGlass", true);
                glass.transform.localRotation = tilt;
                See(glass);
                Rod(root, "Balustrade_Handrail", along + normal, new Vector3(90f - EscalatorPitch, 0f, 0f), 0.08f, slope, "MallSteel");

                EndRail(root, new Vector3(side * rail, 0f, -0.5f));
                if (side < 0f) EndRail(root, new Vector3(side * rail, rise, run + 0.5f));
            }

            // 骨組みの下面の真ん中あたりから、吹き抜けの底へ
            float pier = run * 0.45f;
            float top = pier * rise / run - 0.8f / Mathf.Cos(EscalatorPitch * Mathf.Deg2Rad);
            Rod(root, "Pier", new Vector3(0f, (top + PoolFloor) / 2f, pier), Vector3.zero, 0.6f, top - PoolFloor, "MallConcrete");
        }

        /// <summary>踊り場の上の、平らな手すり。floor は床の上の真ん中で、長さ 1 を z に沿わせる</summary>
        private static void EndRail(Transform root, Vector3 floor)
        {
            See(Box(root, "Balustrade_Glass", floor + new Vector3(0f, 0.475f, 0f), new Vector3(0.03f, 0.85f, 1f), "MallGlass", true));
            Rod(root, "Balustrade_Handrail", floor + new Vector3(0f, 0.95f, 0f), new Vector3(90f, 0f, 0f), 0.08f, 1f, "MallSteel");
        }

        /// <summary>店のガラスと、その上の看板の帯。floor はその階の床の高さ</summary>
        private static void Shopfront(Transform root, float floor)
        {
            Box(root, "Shop_Glass", new Vector3(0f, floor + 1.5f, -0.01f), new Vector3(2.6f, 2.6f, 0.02f), "MallShopGlass", false);
            Box(root, "Shop_Band", new Vector3(0f, floor + 3f, -0.02f), new Vector3(Cell, 0.3f, 0.04f), "MallTrim", false);
        }

        private static void Pillar(Transform root, float height)
        {
            GameObject shaft = Rod(root, "Pillar_Shaft", new Vector3(0f, height / 2f, 0f), Vector3.zero, 1f, height, "MallConcrete");
            shaft.AddComponent<CapsuleCollider>();
            Rod(root, "Pillar_Base", new Vector3(0f, 0.3f, 0f), Vector3.zero, 1.12f, 0.6f, "MallTrim");
        }

        /// <summary>
        /// 素材集の滝を 1 本置く。素材の滝は奥（+z）の縁から手前へ落ちる形なので、向きを返して吹き抜け（+z）へ落とす。
        /// 縁を越える所を LipAt に、上端を top に合わせ、そこから height だけ下へ伸ばす。width と depth は幅と張り出しの倍率
        /// </summary>
        private static void Fall(Transform root, GameObject source, float top, float height, float width, float depth)
        {
            GameObject fall = (GameObject)PrefabUtility.InstantiatePrefab(source, root);
            Bounds bounds = fall.GetComponent<MeshFilter>().sharedMesh.bounds;
            float stretch = height / bounds.size.y;

            fall.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            fall.transform.localScale = new Vector3(width, stretch, depth);
            fall.transform.localPosition = new Vector3(0f, top - bounds.max.y * stretch, LipAt + bounds.max.z * depth);

            MeshRenderer renderer = fall.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat("MallWaterfall");
            renderer.shadowCastingMode = ShadowCastingMode.Off;
        }

        /// <summary>水面に浮いた泡。当たりも影も無い</summary>
        private static void Foam(Transform root, Vector3 center, Vector2 size)
        {
            GameObject foam = Part(PrimitiveType.Quad, root, "Foam", Mat("MallFoam"), false);
            foam.transform.localPosition = center;
            foam.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            foam.transform.localScale = new Vector3(size.x, size.y, 1f);
            See(foam);
        }

        /// <summary>
        /// 落ちた所に立つしぶき。bottom は水面の高さ、size は幅・高さ・奥行き。
        /// 板は片面なので、吹き抜けへ向く 1 枚と、横へ向く表裏 2 枚を立て、どこから見ても 1 枚は表を向くようにする
        /// </summary>
        private static void Mist(Transform root, Vector3 bottom, Vector3 size)
        {
            foreach (float turn in new[] { 180f, 90f, -90f })
            {
                GameObject mist = Part(PrimitiveType.Quad, root, "Mist", Mat("MallMist"), false);
                mist.transform.localPosition = bottom + new Vector3(0f, size.y / 2f, 0f);
                mist.transform.localRotation = Quaternion.Euler(0f, turn, 0f);
                mist.transform.localScale = new Vector3(turn == 180f ? size.x : size.z, size.y, 1f);
                See(mist);
            }
        }

        /// <summary>崩れたコンクリートの塊を散らす。同じ種から同じ並びになる</summary>
        private static void Rubble(Transform root, Vector3 center, float spread, int count, int seed, float minSize, float maxSize)
        {
            Random random = new Random(seed);

            for (int i = 0; i < count; i++)
            {
                float size = Range(random, minSize, maxSize);
                Vector3 at = center + new Vector3(Range(random, -spread, spread), size * 0.3f, Range(random, -spread, spread));
                Vector3 shape = new Vector3(size, size * Range(random, 0.5f, 0.9f), size * Range(random, 0.6f, 1.1f));

                GameObject chunk = Box(root, "Rubble", at, shape, "MallConcrete", false);
                chunk.transform.localRotation = Quaternion.Euler(Range(random, -25f, 25f), Range(random, 0f, 360f), Range(random, -25f, 25f));
            }
        }

        /// <summary>半分埋まった土の塊</summary>
        private static void Lump(Transform root, Vector3 center, Vector3 size, Material dirt)
        {
            GameObject lump = Part(PrimitiveType.Sphere, root, "Dirt", dirt, false);
            lump.transform.localPosition = center;
            lump.transform.localScale = size;
        }

        /// <summary>草の絵を 1 枚、根元が rootAt の高さにくるよう立てる</summary>
        private static void Weed(Transform root, int kind, Vector3 rootAt, float scale)
        {
            Sprite sprite = PropSprite(WeedSprites[kind]);

            GameObject weed = new GameObject("Weed");
            weed.transform.SetParent(root, false);
            weed.transform.localScale = new Vector3(scale, scale, 1f);
            weed.transform.localPosition = rootAt - new Vector3(0f, sprite.bounds.min.y * scale, 0f);
            weed.AddComponent<SpriteRenderer>().sprite = sprite;
        }

        private static GameObject Box(Transform parent, string name, Vector3 center, Vector3 size, string material, bool solid)
        {
            GameObject box = Part(PrimitiveType.Cube, parent, name, Mat(material), solid);
            box.transform.localPosition = center;
            box.transform.localScale = size;

            return box;
        }

        /// <summary>円柱。当たりは付けない。要るなら呼んだ側で足す</summary>
        private static GameObject Rod(Transform parent, string name, Vector3 center, Vector3 euler,
                                      float diameter, float length, string material)
        {
            GameObject rod = Part(PrimitiveType.Cylinder, parent, name, Mat(material), false);
            rod.transform.localPosition = center;
            rod.transform.localRotation = Quaternion.Euler(euler);

            // 組み込みの円柱は高さ 2
            rod.transform.localScale = new Vector3(diameter, length / 2f, diameter);

            return rod;
        }

        private static GameObject Part(PrimitiveType shape, Transform parent, string name, Material material, bool solid)
        {
            GameObject part = GameObject.CreatePrimitive(shape);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.GetComponent<MeshRenderer>().sharedMaterial = material;

            if (!solid) Object.DestroyImmediate(part.GetComponent<Collider>());

            return part;
        }

        /// <summary>1 マスの水面。当たりは無く、上を歩ける</summary>
        private static void WaterPlane(Transform root, string material)
        {
            GameObject surface = Part(PrimitiveType.Quad, root, "Water", Mat(material), false);
            surface.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            surface.transform.localScale = new Vector3(Cell, Cell, 1f);
            See(surface);
        }

        /// <summary>透ける物。影を落とすと水面やガラスの形の影が床に出る</summary>
        private static void See(GameObject part)
        {
            part.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        private static float Range(Random random, float min, float max)
        {
            return min + (float)random.NextDouble() * (max - min);
        }
    }
}
