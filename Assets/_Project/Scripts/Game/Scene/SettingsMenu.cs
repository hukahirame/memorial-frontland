using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Events;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 遊ぶ画面の設定。持ち物のボタンの真下に歯車のボタンを置き、押すか Esc で設定の窓を開く。
    /// 窓を開いているあいだはゲームを止め（時間・敵・一日の巡り）、後ろのボタンやスティックも押させない。
    /// 窓には「タイトルに戻る」と閉じるボタンを置く。タイトルへ戻す処理（保存して読み替える）は TitleRequested を受けた側が行う。
    /// 窓とボタンは、根源の詳細の窓の地と閉じるボタン、近道のボタンを写して作り、ほかの画面と見た目をそろえる
    /// </summary>
    public sealed class SettingsMenu : MonoBehaviour
    {
        /// <summary>歯車のボタンの一辺。参照解像度の画素。絵のまわりに余白があるので、見える歯車が持ち物の絵と同じくらいになる大きさ</summary>
        private const float GearSize = 140f;

        /// <summary>窓の大きさ</summary>
        private static readonly Vector2 WindowSize = new Vector2(760f, 420f);

        /// <summary>窓の題の字の大きさ</summary>
        private const int HeadingFontSize = 48;

        /// <summary>窓の題の、窓の上端からの深さ</summary>
        private const float HeadingDepth = 80f;

        /// <summary>閉じるボタンの、窓の右上の角からの離れ</summary>
        private const float CloseInset = 24f;

        /// <summary>閉じるボタンの縮め。写す元（根源の詳細の ×）は大きな窓に合わせた 160 角</summary>
        private const float CloseScale = 0.7f;

        /// <summary>ほかの画面（地図・持ち物）より手前、移るときの暗転（100）より奥に描く</summary>
        private const int SortingOrder = 50;

        [SerializeField] private AreaTravel travel;

        [Tooltip("歯車の絵")]
        [SerializeField] private Sprite gear;

        [Tooltip("この真下に歯車のボタンを置く。持ち物のボタン")]
        [SerializeField] private RectTransform below;

        [Tooltip("窓の地を写す元。根源の詳細の窓")]
        [SerializeField] private Image windowModel;

        [Tooltip("閉じるボタンを写す元。根源の詳細の ×")]
        [SerializeField] private Button closeModel;

        [Tooltip("字のボタンを写す元。近道のボタン")]
        [SerializeField] private Button buttonModel;

        private GameObject _panel;

        /// <summary>窓を開いているか。場面を離れるときは窓が先に壊れることがあるので、窓とは別に持つ</summary>
        private bool _open;

        /// <summary>「タイトルに戻る」が押された。窓は開いたまま、止めたまま渡す</summary>
        public event Action TitleRequested;

        public bool IsOpen
        {
            get { return _open; }
        }

        private void Start()
        {
            RectTransform gearButton = BuildGear();
            _panel = BuildPanel();

            KeyHint.Put(gearButton, "Esc");
            PressFeedback.AttachAll(gearButton);
            PressFeedback.AttachAll(_panel.transform);
        }

        private void Update()
        {
            if (!Input.GetKeyDown(KeyCode.Escape)) return;

            if (IsOpen) Close();
            else Open();
        }

        //当てたときの止め（HitStop）は、止め終わると時間を戻す。窓を開いているあいだは、どの Update のあとでも止め直す
        private void LateUpdate()
        {
            if (_open) Time.timeScale = 0f;
        }

        //開いたまま場面を離れても、止めたまま残さない
        private void OnDestroy()
        {
            if (_open) Time.timeScale = 1f;
        }

        /// <summary>歯車のボタンと Esc から。移っている途中は開かない</summary>
        public void Open()
        {
            if (_open || _panel == null || travel.IsTraveling) return;

            _open = true;
            _panel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Close()
        {
            if (!_open) return;

            _open = false;
            _panel.SetActive(false);
            Time.timeScale = 1f;
        }

        /// <summary>「タイトルに戻る」から。テストからも呼ぶ</summary>
        public void RequestTitle()
        {
            TitleRequested?.Invoke();
        }

        /// <summary>持ち物のボタンの真下。持ち物のボタンから測るので、持ち物のボタンを動かしてもついていく</summary>
        private RectTransform BuildGear()
        {
            RectTransform rect = NewRect("SettingsButton", below.parent);
            rect.anchorMin = below.anchorMin;
            rect.anchorMax = below.anchorMax;
            rect.sizeDelta = new Vector2(GearSize, GearSize);

            float centerX = below.anchoredPosition.x + below.sizeDelta.x * (0.5f - below.pivot.x);
            float bottom = below.anchoredPosition.y - below.sizeDelta.y * below.pivot.y;
            rect.anchoredPosition = new Vector2(centerX, bottom - GearSize / 2f);

            //持ち物のボタンのすぐ後ろに並べ、ほかの画面を開いたときに同じように暗くなる
            rect.SetSiblingIndex(below.GetSiblingIndex() + 1);

            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = gear;
            image.preserveAspect = true;

            Button button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(Open);
            return rect;
        }

        /// <summary>後ろを暗くして覆い、真ん中に窓を出す。覆いは、後ろのボタンやスティックへの指も受け止める</summary>
        private GameObject BuildPanel()
        {
            RectTransform root = NewRect("SettingsPanel", below.parent);

            //隠したまま組み立てる。写した部品に元から付いていた振る舞いが、外す前に動き出さないように
            root.gameObject.SetActive(false);
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            //地図や持ち物を開いたまま開いても、その手前に出す
            Canvas canvas = root.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = SortingOrder;
            root.gameObject.AddComponent<GraphicRaycaster>();

            Image shade = root.gameObject.AddComponent<Image>();
            shade.color = new Color(0f, 0f, 0f, UiStage.DimAlpha);

            RectTransform window = NewRect("Window", root);
            window.sizeDelta = WindowSize;
            Image ground = window.gameObject.AddComponent<Image>();
            ground.sprite = windowModel.sprite;
            ground.type = windowModel.type;
            ground.pixelsPerUnitMultiplier = windowModel.pixelsPerUnitMultiplier;
            ground.color = windowModel.color;

            Text sample = buttonModel.GetComponentInChildren<Text>(true);
            Text heading = NewRect("Heading", window).gameObject.AddComponent<Text>();
            heading.text = "設定";
            heading.font = sample.font;
            heading.fontSize = HeadingFontSize;
            heading.color = Color.white;
            heading.alignment = TextAnchor.MiddleCenter;
            heading.horizontalOverflow = HorizontalWrapMode.Overflow;
            heading.verticalOverflow = VerticalWrapMode.Overflow;
            heading.raycastTarget = false;
            ((RectTransform)heading.transform).anchoredPosition = new Vector2(0f, WindowSize.y / 2f - HeadingDepth);

            Button toTitle = Copy(buttonModel, window, RequestTitle);
            toTitle.GetComponentInChildren<Text>(true).text = "タイトルに戻る";
            ((RectTransform)toTitle.transform).anchoredPosition = new Vector2(0f, -WindowSize.y / 8f);

            Button close = Copy(closeModel, window, Close);
            var closeRect = (RectTransform)close.transform;
            closeRect.anchorMin = Vector2.one;
            closeRect.anchorMax = Vector2.one;
            closeRect.pivot = Vector2.one;
            closeRect.anchoredPosition = new Vector2(-CloseInset, -CloseInset);
            closeRect.localScale = Vector3.one * CloseScale;

            return root.gameObject;
        }

        /// <summary>見た目だけを写す。元の呼び先と、元に付いていた振る舞い（近道や閉じる処理）、キーの札は持ち込まない</summary>
        private Button Copy(Button model, Transform parent, UnityAction onClick)
        {
            Button copy = Instantiate(model, parent);
            copy.name = model.name;
            copy.gameObject.SetActive(true);
            copy.interactable = true;

            var rect = (RectTransform)copy.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.localScale = Vector3.one;

            copy.onClick = new Button.ButtonClickedEvent();
            copy.onClick.AddListener(onClick);

            foreach (MonoBehaviour behaviour in copy.GetComponentsInChildren<MonoBehaviour>(true))
            {
                if (behaviour is UIBehaviour || behaviour is PressFeedback) continue;

                Destroy(behaviour);
            }

            foreach (RectTransform child in copy.GetComponentsInChildren<RectTransform>(true))
            {
                if (child.name == "KeyHint") Destroy(child.gameObject);
            }

            return copy;
        }

        /// <summary>UI の層に置いた、真ん中を基準にした枠</summary>
        private RectTransform NewRect(string name, Transform parent)
        {
            var made = new GameObject(name, typeof(RectTransform));
            made.layer = below.gameObject.layer;

            var rect = (RectTransform)made.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            return rect;
        }
    }
}
