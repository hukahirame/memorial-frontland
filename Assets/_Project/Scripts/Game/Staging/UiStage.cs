using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// Canvas に置き、画面の出し入れとボタンの手触りを受け持つ。
    /// 持ち物や地図などの画面が開いているあいだは後ろを暗くし、開いた瞬間は短く浮かび上がらせる。
    /// 画面は開くと真ん中へ、閉じると画面の外（y 5000）へ移されるだけなので、開いているかは位置で見る。
    /// 暗くしても後ろの操作は止めない
    /// </summary>
    public sealed class UiStage : MonoBehaviour
    {
        /// <summary>後ろを暗くする濃さ</summary>
        public const float DimAlpha = 0.45f;

        /// <summary>暗くなりきるまで、明るく戻りきるまでの秒数</summary>
        public const float DimSeconds = 0.12f;

        /// <summary>開いた画面が浮かび上がりきるまでの秒数</summary>
        public const float AppearSeconds = 0.12f;

        /// <summary>開いた画面が浮かび上がり始める大きさ。もとの大きさに対する比</summary>
        public const float AppearScale = 0.94f;

        /// <summary>
        /// 1 フレームで画面の幅のこの割合より大きく動いて画面に入ったら、開いたとみなす。
        /// 持ち物と地図の入れ替えのように滑って入ってくるものは、それ自体が動きなので浮かび上がらせない
        /// </summary>
        private const float JumpRatio = 0.5f;

        /// <summary>開いている画面に増えたボタンへ、押したときの手触りを付け直す間隔（実時間の秒）</summary>
        private const float RescanSeconds = 0.5f;

        [Tooltip("開いているあいだ後ろを暗くする画面。どれか 1 つでも開いていれば暗くする")]
        [SerializeField] private RectTransform[] panels = new RectTransform[0];

        private Image _dimmer;
        private float _dim;
        private float _rescanAt;

        private Vector3[] _lastPositions;
        private bool[] _shown;
        private bool[] _appearing;
        private float[] _appearedAt;
        private Vector3[] _baseScales;
        private CanvasGroup[] _groups;

        private void Awake()
        {
            _dimmer = CreateDimmer();

            int count = panels.Length;
            _lastPositions = new Vector3[count];
            _shown = new bool[count];
            _appearing = new bool[count];
            _appearedAt = new float[count];
            _baseScales = new Vector3[count];
            _groups = new CanvasGroup[count];

            for (int i = 0; i < count; i++)
            {
                _lastPositions[i] = panels[i].position;
                _shown[i] = IsShown(panels[i]);
                _groups[i] = panels[i].TryGetComponent(out CanvasGroup group) ? group : panels[i].gameObject.AddComponent<CanvasGroup>();
            }

            PressFeedback.AttachAll(transform);
        }

        private void LateUpdate()
        {
            float now = Time.unscaledTime;
            bool anyShown = false;

            for (int i = 0; i < panels.Length; i++)
            {
                RectTransform panel = panels[i];
                bool shown = IsShown(panel);

                if (shown && !_shown[i] && (panel.position - _lastPositions[i]).magnitude > Screen.width * JumpRatio)
                {
                    _appearing[i] = true;
                    _appearedAt[i] = now;
                    _baseScales[i] = panel.localScale;
                    PressFeedback.AttachAll(panel);
                }

                if (_appearing[i]) Appear(i, shown ? (now - _appearedAt[i]) / AppearSeconds : 1f);

                _shown[i] = shown;
                _lastPositions[i] = panel.position;
                anyShown |= shown;
            }

            //地図の根源のボタンのように、開くたびに作り直されるものにも付ける
            if (anyShown && now >= _rescanAt)
            {
                for (int i = 0; i < panels.Length; i++)
                {
                    if (_shown[i]) PressFeedback.AttachAll(panels[i]);
                }
                _rescanAt = now + RescanSeconds;
            }

            _dim = Mathf.MoveTowards(_dim, anyShown ? DimAlpha : 0f, DimAlpha * Time.unscaledDeltaTime / DimSeconds);
            _dimmer.color = new Color(0f, 0f, 0f, _dim);
            _dimmer.enabled = _dim > 0f;
        }

        //終わり際をゆっくりにして、透明から濃く、少し小さい所から元の大きさへ
        private void Appear(int i, float progress)
        {
            float eased = 1f - Mathf.Pow(1f - Mathf.Clamp01(progress), 3f);
            _groups[i].alpha = eased;
            panels[i].localScale = _baseScales[i] * Mathf.Lerp(AppearScale, 1f, eased);

            if (progress >= 1f) _appearing[i] = false;
        }

        //真ん中が画面の中にあれば開いている。重ねて表示する Canvas なので、位置は画面の画素で測る
        private static bool IsShown(RectTransform panel)
        {
            if (!panel.gameObject.activeInHierarchy) return false;

            Vector3 center = panel.position;
            return center.x >= 0f && center.x <= Screen.width && center.y >= 0f && center.y <= Screen.height;
        }

        //画面のうち最も奥に描かれるもののすぐ後ろに差し込む。それより前に並ぶ操作のボタンと文字が暗くなる
        private Image CreateDimmer()
        {
            var dimmer = new GameObject("Dimmer", typeof(RectTransform), typeof(Image));
            dimmer.layer = gameObject.layer;

            var rect = (RectTransform)dimmer.transform;
            rect.SetParent(transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetSiblingIndex(BackmostPanelIndex());

            var image = dimmer.GetComponent<Image>();
            image.color = Color.clear;
            image.raycastTarget = false;
            image.enabled = false;
            return image;
        }

        private int BackmostPanelIndex()
        {
            int index = transform.childCount - 1;
            foreach (RectTransform panel in panels)
            {
                if (panel.parent == transform) index = Mathf.Min(index, panel.GetSiblingIndex());
            }

            return index;
        }
    }
}
