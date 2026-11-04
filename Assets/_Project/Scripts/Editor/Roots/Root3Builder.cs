using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Editor
{
    /// <summary>
    /// 根源3。水没したモールを、見た目を試すシーンと同じ組み方で場所のシーンにし、遊ぶための部品を足す。
    /// 上の階へは奥の通路のエスカレーターで上がれる。核の候補は上下の階に散らし、敵は核になった候補と同じ階に湧く。
    /// 手前の根源2には奥の出口を足し、Build Settings にも入れる。湧く敵は水色のスライム（ソーダスライム）
    /// </summary>
    public static class Root3Builder
    {
        public const string ScenePath = "Assets/_Project/Scenes/Root3.unity";

        private const string Root2Path = "Assets/_Project/Scenes/Root2.unity";

        private const string SlimePath = "Assets/_Project/Resources/Slime.prefab";

        /// <summary>根源3に湧く敵。Slime の Variant にし、絵・動き・名札を水色の物に、強さと落とす品を替える</summary>
        private const string SodaSlimePath = "Assets/_Project/Resources/SodaSlime.prefab";

        private const string SodaSprite = "Assets/_Project/Art/EnemyA/Enemyα-1-1.png";

        private const string SodaController = "Assets/_Project/Art/EnemyA/Enemyα.controller";

        /// <summary>ソーダスライムが必ず落とす品。スライムゼリーの代わりに落とす</summary>
        private const string SodaJelly = "SodaSlimejelly";

        /// <summary>ソーダスライムがスライムコアを落とす割合（%）。スライムより出やすくする</summary>
        private const int SodaCorePercent = 15;

        /// <summary>ソーダスライムの速さ・攻撃・体力を、スライムの何倍にするか</summary>
        private const float SodaStrength = 1.8f;

        /// <summary>核の候補。根源2と同じ素材集の像。当たりが無いので足す</summary>
        private const string CandidatePrefab = "Assets/Dark_Fantasy/Assets/Prefabs_Grp/BASIC/Statues/Statue_01A_Lod0.prefab";

        /// <summary>一日の長さ（秒）。拠点・根源1・根源2 と同じ</summary>
        private const float DayLength = 120f;

        /// <summary>像の大きさの倍率。広いモールでも核の候補が目に付くよう人より大きくし、上の階の床の下（高さ 3.5）に収める</summary>
        private const float CandidateScale = 1.2f;

        /// <summary>
        /// 核になった候補が湧く地面を探し始める、候補の立つ階の床からの高さ。
        /// 下の階では、上の階の縁から垂れた面（下端 3.1）より低くし、上の階の床に湧かせない
        /// </summary>
        private const float SeekRise = 2.5f;

        /// <summary>湧く地面を探す深さ。上の階の候補から、床の穴を抜けて下の階（4 下）に届かない</summary>
        private const float SeekDepth = 3.5f;

        /// <summary>上の階の床の高さ</summary>
        private const float Upper = FloodedMallBuilder.UpperFloor;

        /// <summary>
        /// モールを置く位置。場の外へ飛ばされたプレイヤーは、どの場所でも (-2, 0.65, -2) へ戻される（Player2）。
        /// そこが左の通路の床になるよう、モールを右へずらす
        /// </summary>
        private static readonly Vector3 Offset = new Vector3(12f, 0f, 0f);

        /// <summary>場の外へ飛ばされたプレイヤーが戻される地点（Player2 に書かれている）</summary>
        private static readonly Vector3 Rescue = new Vector3(-2f, 0.65f, -2f);

        /// <summary>根源2から来たとき、始めたとき、倒れたあとに立つ地点。左の壁の扉の右</summary>
        private static readonly Vector3 Arrival = new Vector3(-1.5f, 0.6f, -3f);

        /// <summary>根源2へ戻る出口。左の壁の扉の前</summary>
        private static readonly Vector3 ExitAt = new Vector3(-4f, 0.25f, -3f);

        /// <summary>出口に立てる看板の絵。拠点と根源1・2 の出口と同じ、村の小物の絵の矢印</summary>
        private const string SignSheet = "Assets/Resources/TX Village Props 1.png";

        private const string SignName = "TX Village Props 1_13";

        /// <summary>場の範囲。XL, XS, ZL, ZS の順。下の階の通路を囲む壁より少し内側。手前は開いている</summary>
        private static readonly Vector4 Exposition = new Vector4(27.9f, -3.9f, 15.9f, -4.2f);

        /// <summary>
        /// 核の候補（像）を置く所。下の階と上の階に 7 つずつ、左・右・奥の通路に散らす。
        /// 土の山・瓦礫・柱・木・エスカレーター・上の階の床の穴・滝の落ちる縁を避ける。手前の行はプレイヤーを隠すので置かない
        /// </summary>
        private static readonly Vector3[] Candidates =
        {
            new Vector3(0.5f, 0f, 5f), new Vector3(24.5f, 0f, 3.5f), new Vector3(14f, 0f, 13.5f),
            new Vector3(-2.5f, 0f, 11.5f), new Vector3(22.5f, 0f, 13f), new Vector3(26f, 0f, 8f), new Vector3(3f, 0f, 14f),
            new Vector3(0f, Upper, -1.5f), new Vector3(0.5f, Upper, 9f), new Vector3(12f, Upper, 14.5f),
            new Vector3(24f, Upper, 12f), new Vector3(25.5f, Upper, 0f), new Vector3(4f, Upper, 14.5f), new Vector3(27f, Upper, 5f),
        };

        /// <summary>湧きの起点を置いておく所。核が決まると核の子になるので、どこでもよい</summary>
        private static readonly Vector3 SeekerAt = new Vector3(12f, 0.2f, 13.5f);

        /// <summary>根源2の奥の出口。根源1の奥の出口と同じ形</summary>
        private static readonly Vector3 Root2ExitAt = new Vector3(23f, 0.25f, -1f);

        /// <summary>根源3から根源2へ戻ってきたときに立つ地点。根源2の場所（Area）からの相対で、根源1と同じ</summary>
        private static readonly Vector3 Root2FarEntrance = new Vector3(39f, 0f, 0f);

        [MenuItem("Tools/Roots/Build Root3")]
        public static void Build()
        {
            GameObject enemy = SodaSlime();
            FloodedMallBuilder.BuildPlace(ScenePath, Offset, (scene, mall) => Play(mall, enemy));
            WireRoot2();
            AddToBuild();

            foreach (string path in PlaceLooks.Shoot(ScenePath))
            {
                Debug.Log("撮った: " + path);
            }
        }

        /// <summary>
        /// 水色のスライム。Slime の Variant にし、絵・動き・名札・強さ・落とす品を替える。
        /// 討伐クエストではスライムとして数える
        /// </summary>
        private static GameObject SodaSlime()
        {
            GameObject source = Load<GameObject>(SlimePath);
            Scene preview = EditorSceneManager.NewPreviewScene();

            try
            {
                GameObject slime = (GameObject)PrefabUtility.InstantiatePrefab(source, preview);

                // スクリプトで変えた値は、記録しないと Variant の上書きに残らないことがある
                SpriteRenderer shown = slime.GetComponent<SpriteRenderer>();
                shown.sprite = Load<Sprite>(SodaSprite);
                PrefabUtility.RecordPrefabInstancePropertyModifications(shown);

                Animator moves = slime.GetComponent<Animator>();
                moves.runtimeAnimatorController = Load<RuntimeAnimatorController>(SodaController);
                PrefabUtility.RecordPrefabInstancePropertyModifications(moves);

                SerializedObject kind = new SerializedObject(slime.GetComponent(Legacy("Slime")));
                kind.FindProperty("questTarget").stringValue = "Slime";
                SodaDrops(kind);
                Strengthen(kind);
                kind.ApplyModifiedPropertiesWithoutUndo();

                // 頭の上の名札。UnityEngine.UI はこのアセンブリから参照していないので、型を名前で引く
                Type text = Type.GetType("UnityEngine.UI.Text, UnityEngine.UI");
                if (text == null) throw new InvalidOperationException("UnityEngine.UI.Text が無い");

                foreach (Component label in slime.GetComponentsInChildren(text, true))
                {
                    SerializedObject words = new SerializedObject(label);
                    if (words.FindProperty("m_Text").stringValue != "スライム") continue;

                    words.FindProperty("m_Text").stringValue = "ソーダスライム";
                    words.ApplyModifiedPropertiesWithoutUndo();
                }

                return PrefabUtility.SaveAsPrefabAsset(slime, SodaSlimePath);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(preview);
            }
        }

        /// <summary>スライムゼリーの代わりにソーダスライムゼリーを必ず落とし、スライムコアはスライムより出やすくする</summary>
        private static void SodaDrops(SerializedObject kind)
        {
            SerializedProperty always = kind.FindProperty("drops");
            always.arraySize = 1;
            always.GetArrayElementAtIndex(0).stringValue = SodaJelly;

            SerializedProperty chances = kind.FindProperty("chanceDrops");
            chances.arraySize = 1;
            SerializedProperty core = chances.GetArrayElementAtIndex(0);
            core.FindPropertyRelative("ItemId").stringValue = "Slimecore";
            core.FindPropertyRelative("Percent").intValue = SodaCorePercent;
        }

        /// <summary>速さ・攻撃・体力をスライムの SodaStrength 倍にする。体力の上限と初期値は頭の上の Slider が持つ（Slime）</summary>
        private static void Strengthen(SerializedObject kind)
        {
            kind.FindProperty("speed").floatValue *= SodaStrength;
            kind.FindProperty("power").floatValue *= SodaStrength;

            // Slider の型はこのアセンブリから参照していないので、値は SerializedObject で触る
            SerializedObject bar = new SerializedObject(kind.FindProperty("hp").objectReferenceValue);
            float max = Mathf.Round(bar.FindProperty("m_MaxValue").floatValue * SodaStrength);
            bar.FindProperty("m_MaxValue").floatValue = max;
            bar.FindProperty("m_Value").floatValue = max;
            bar.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>遊ぶための部品。場所・出口・湧きの起点・核の候補を足し、日の光を一日の巡りにつなぐ</summary>
        private static void Play(Transform mall, GameObject enemy)
        {
            // 場所はシーンのルートに 1 つ。ほかのルートは、常駐シーンが来るまで止められる
            GameObject area = new GameObject("Area");
            area.transform.position = Arrival;

            SerializedObject place = new SerializedObject(area.AddComponent<Area>());
            place.FindProperty("kind").enumValueIndex = (int)AreaKind.Root;
            place.FindProperty("exposition").vector4Value = Exposition;
            place.FindProperty("enemy").objectReferenceValue = enemy;

            // 上の階に立っても連れ戻さない。上の階の床から、ほかの場所と同じだけ上がったら連れ戻す
            place.FindProperty("ceiling").floatValue = Upper + FieldBounds.EscapeHeight;
            place.ApplyModifiedPropertiesWithoutUndo();

            // 名前は変えない。核を壊したとき、敵を出す目印としてこの名前で引かれる（OF_Spawner）
            // 湧く地面を探す Raycast はトリガーにも当たる。出口の上に湧かせない
            GameObject exit = new GameObject("ST_MainSite");
            exit.tag = "SceneTransition";
            exit.layer = FloodedMallBuilder.SeekThrough;
            exit.transform.position = ExitAt;
            Door(exit, new Vector3(1.5f, 0.5f, 3f), Vector3.zero, ExitKind.ToPrevious);
            Signpost(exit, ExitKind.ToPrevious);

            // 名前で引かれ、核の子になって、湧く地面を探す（OF_Spawner、Seeker）
            GameObject seeker = new GameObject("Seeker");
            seeker.transform.position = SeekerAt;
            seeker.AddComponent(Legacy("Seeker"));
            BoxCollider idle = seeker.AddComponent<BoxCollider>();
            idle.size = new Vector3(1f, 0.1f, 1f);
            idle.enabled = false;

            // 立つ所と、核の候補の下に床があるか。床が無いと、着いたとたんに落ちて場の外とみなされる。像を置く前に見る
            Physics.SyncTransforms();
            Floor(Arrival, 0f, "根源2から来たときに立つ地点");
            Floor(Rescue, 0f, "場の外から戻される地点");
            foreach (Vector3 at in Candidates) Floor(at, at.y, "核の候補");

            GameObject statue = Load<GameObject>(CandidatePrefab);
            foreach (Vector3 at in Candidates) Candidate(statue, at);

            Sun(mall);
        }

        /// <summary>出口の当たり。触れると、kind の向きの隣の場所へ移る（Player2、Exit）</summary>
        private static void Door(GameObject exit, Vector3 size, Vector3 center, ExitKind kind)
        {
            BoxCollider door = exit.AddComponent<BoxCollider>();
            door.isTrigger = true;
            door.size = size;
            door.center = center;

            SerializedObject way = new SerializedObject(exit.AddComponent<Exit>());
            way.FindProperty("kind").enumValueIndex = (int)kind;
            way.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>
        /// 出口の看板。拠点と根源1・2 の出口と同じ絵・大きさで出口に立て、矢印で行く向き（戻るなら左、進むなら右）を指す。
        /// どの場所でも矢印の看板を出口の目印にする。根源3の戻る扉は左の壁にあり、遊ぶときのカメラからは横から見えて暗い筋にしか見えない。
        /// 看板がもうあれば何もしない
        /// </summary>
        private static void Signpost(GameObject exit, ExitKind kind)
        {
            if (exit.GetComponentInChildren<SpriteRenderer>(true) != null) return;

            Sprite arrow = null;
            foreach (UnityEngine.Object part in AssetDatabase.LoadAllAssetsAtPath(SignSheet))
            {
                if (part is Sprite && part.name == SignName) arrow = (Sprite)part;
            }

            if (arrow == null) throw new InvalidOperationException(SignSheet + " に " + SignName + " が無い");

            GameObject sign = new GameObject(SignName);
            sign.transform.SetParent(exit.transform, false);
            sign.transform.localPosition = new Vector3(0f, 0.15f, 0f);
            sign.transform.localScale = new Vector3(2f, 2f, 1f);

            SpriteRenderer shown = sign.AddComponent<SpriteRenderer>();
            shown.sprite = arrow;
            shown.flipX = kind == ExitKind.ToPrevious;

            // 透ける物の描く順は、プレイヤーと敵と同じく奥行きで決める（FloodedMallBuilder.Sort）
            shown.sortingOrder = Mathf.RoundToInt(sign.transform.position.z * FloodedMallBuilder.OrderPerDepth);
        }

        /// <summary>
        /// 核の候補。像に印と当たりを付ける。入ったときに 1 つが選ばれて湧き口になり、ほかは叩くと壊れる
        /// （SceneStarter、Weapon）。印・当たり・部品は同じ GameObject に無いと、叩いても効かない
        /// </summary>
        private static void Candidate(GameObject statue, Vector3 at)
        {
            GameObject candidate = (GameObject)PrefabUtility.InstantiatePrefab(statue);
            candidate.transform.position = at;
            candidate.transform.localScale = Vector3.one * CandidateScale;
            candidate.tag = "SpawnerCandidate";

            // 素材集の像には static の印が付いている。まとめて描かれると、核が壊れて膨らむ動きが画面に出ない
            foreach (Transform part in candidate.GetComponentsInChildren<Transform>(true)) GameObjectUtility.SetStaticEditorFlags(part.gameObject, 0);

            // 像の上に敵を湧かせない。叩いたときの当たりは、レイヤーにかかわらず効く
            candidate.layer = FloodedMallBuilder.SeekThrough;

            // 核になったら、候補の立つ階の床に湧かせる
            SerializedObject seek = new SerializedObject(candidate.AddComponent(Legacy("SpawnerCandidate")));
            seek.FindProperty("seekFrom").floatValue = at.y + SeekRise;
            seek.FindProperty("seekDepth").floatValue = SeekDepth;
            seek.ApplyModifiedPropertiesWithoutUndo();

            Renderer[] shown = candidate.GetComponentsInChildren<Renderer>();
            if (shown.Length == 0) throw new InvalidOperationException(CandidatePrefab + " に描く物が無い");

            Bounds bounds = shown[0].bounds;
            foreach (Renderer part in shown) bounds.Encapsulate(part.bounds);

            // 像の原点は真ん中にあり、そのまま置くと下半分が床に埋まる。土台を床に載せる
            Vector3 lift = Vector3.up * (at.y - bounds.min.y);
            candidate.transform.position += lift;
            bounds.center += lift;

            BoxCollider box = candidate.AddComponent<BoxCollider>();
            box.center = candidate.transform.InverseTransformPoint(bounds.center);
            box.size = Vector3.Scale(bounds.size, Inverse(candidate.transform.lossyScale));
        }

        /// <summary>
        /// 日の光を一日の巡りにつなぐ。巡りは場所に置いた Sun2 が進めるので、無いと根源3に居るあいだ夜が来ない。
        /// 日差しの筋も日の光の強さに合わせ、組んだときの強さで素材どおりの濃さにする
        /// </summary>
        private static void Sun(Transform mall)
        {
            Light sun = null;
            foreach (Light light in mall.GetComponentsInChildren<Light>(true))
            {
                if (light.type == LightType.Directional) sun = light;
            }

            if (sun == null) throw new InvalidOperationException("モールに日の光が無い");

            SerializedObject cycle = new SerializedObject(sun.gameObject.AddComponent(Legacy("Sun2")));
            cycle.FindProperty("aroundtime").floatValue = DayLength;
            cycle.FindProperty("lighting").objectReferenceValue = sun;
            cycle.ApplyModifiedPropertiesWithoutUndo();

            Transform beams = mall.Find("Sunbeams");
            if (beams == null) throw new InvalidOperationException("モールに日差しの筋が無い");

            SerializedObject dimmer = new SerializedObject(beams.gameObject.AddComponent<ShaftDimmer>());
            dimmer.FindProperty("sun").objectReferenceValue = sun;
            dimmer.FindProperty("fullAt").floatValue = sun.intensity;
            dimmer.ApplyModifiedPropertiesWithoutUndo();
        }

        /// <summary>根源2に、根源3へ進む奥の出口とその看板、根源3から戻ってきたときに立つ地点を足す。何度呼んでも増えない</summary>
        private static void WireRoot2()
        {
            Scene open = SceneManager.GetSceneByPath(Root2Path);
            bool reuse = open.isLoaded;
            if (reuse && open.isDirty) throw new InvalidOperationException(Root2Path + " に保存していない変更がある。保存するか捨ててから組む");

            Scene previous = SceneManager.GetActiveScene();
            Scene scene = reuse ? open : EditorSceneManager.OpenScene(Root2Path, OpenSceneMode.Additive);

            try
            {
                // 新しく作った物は有効なシーンに入る。開いていたシーンに作って汚さないよう、根源2を有効にする
                SceneManager.SetActiveScene(scene);

                Area area = Area.In(scene);
                if (area == null) throw new InvalidOperationException(Root2Path + " に Area が無い");

                Transform far = area.transform.Find("FarEntrance");
                if (far == null)
                {
                    far = new GameObject("FarEntrance").transform;
                    far.SetParent(area.transform, false);
                    far.localPosition = Root2FarEntrance;
                }

                SerializedObject place = new SerializedObject(area);
                place.FindProperty("farEntrance").objectReferenceValue = far;
                place.ApplyModifiedPropertiesWithoutUndo();

                if (RootNamed(scene, "ST_Root3") == null)
                {
                    // 手前の出口と同じ大きさの当たりにする
                    GameObject near = RootNamed(scene, "ST_MainSite");
                    if (near == null) throw new InvalidOperationException(Root2Path + " に ST_MainSite が無い");

                    BoxCollider model = near.GetComponent<BoxCollider>();
                    GameObject exit = new GameObject("ST_Root3");
                    exit.tag = near.tag;
                    exit.transform.position = Root2ExitAt;
                    Door(exit, model.size, model.center, ExitKind.ToNext);
                }

                Signpost(RootNamed(scene, "ST_Root3"), ExitKind.ToNext);

                Physics.SyncTransforms();
                Floor(far.position, 0f, "根源3から戻ってきたときに立つ地点");

                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
            finally
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                if (!reuse) EditorSceneManager.CloseScene(scene, true);
            }
        }

        /// <summary>Build Settings に入れる。場所は名前で読み込むので、入っていないと移ったときに例外になる</summary>
        private static void AddToBuild()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!scenes.Exists(scene => scene.path == ScenePath))
            {
                scenes.Add(new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }

            // 一覧はプロジェクトを保存するまでファイルに書かれない
            AssetDatabase.SaveAssets();
        }

        /// <summary>at の真下に、高さ floor 前後の床があるか。無ければ止める。下の階では上の階の床（高さ 4）の下から撃つ</summary>
        private static void Floor(Vector3 at, float floor, string what)
        {
            RaycastHit hit;
            bool found = Physics.Raycast(new Vector3(at.x, floor + 1.5f, at.z), Vector3.down, out hit, 4f, ~0, QueryTriggerInteraction.Ignore);
            if (!found || Mathf.Abs(hit.point.y - floor) > 0.6f)
                throw new InvalidOperationException(what + " " + at + " の下に床が無い" + (found ? "（当たった高さ " + hit.point.y + "）" : ""));
        }

        private static GameObject RootNamed(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                if (root.name == name) return root;
            }

            return null;
        }

        /// <summary>古いスクリプトの型。古いスクリプトは asmdef の無いアセンブリにあり、ここからは型を名前でしか引けない</summary>
        private static Type Legacy(string name)
        {
            Type type = Type.GetType(name + ", Assembly-CSharp");
            if (type == null) throw new InvalidOperationException(name + " が Assembly-CSharp に無い");

            return type;
        }

        private static T Load<T>(string path) where T : UnityEngine.Object
        {
            T asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null) throw new InvalidOperationException(path + " が無い");

            return asset;
        }

        private static Vector3 Inverse(Vector3 scale)
        {
            return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
        }
    }
}
