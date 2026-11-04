using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// ボタンをキーでも押せるようにし、ボタンの右下にそのキーを書く。
    /// キーで押すのは指でも押せるボタンだけで、隠れている、ほかの画面の下にある、押せない状態のものは押さない。
    /// 閉じるボタンを渡すと、その画面が開いているあいだは同じキーで閉じる
    /// </summary>
    public sealed class KeyShortcut : MonoBehaviour
    {
        [Tooltip("押すキー")]
        [SerializeField] private KeyCode key = KeyCode.None;

        [Tooltip("キーで押すボタン。指で押せる最初の 1 つを押す。どれにも右下にキーを書く")]
        [SerializeField] private Button[] buttons = new Button[0];

        [Tooltip("このボタンで開く画面の閉じるボタン。画面が開いているあいだは、キーでこちらを押す")]
        [SerializeField] private Button close;

        private readonly List<RaycastResult> _hits = new List<RaycastResult>();

        private void Start()
        {
            foreach (Button button in buttons) KeyHint.Put((RectTransform)button.transform, key.ToString());
        }

        private void Update()
        {
            if (!Input.GetKeyDown(key)) return;

            Button target = Pick();
            if (target != null) target.onClick.Invoke();
        }

        private Button Pick()
        {
            if (close != null && IsReachable(close)) return close;

            foreach (Button button in buttons)
            {
                if (IsReachable(button)) return button;
            }

            return null;
        }

        //真ん中を指したとき、いちばん手前に当たるのがそのボタンかその子なら、指で押せる。
        //大きさ 0 で隠したボタンは、潰れた矩形でも当たりが出ることがあるので先に除く
        private bool IsReachable(Button button)
        {
            if (!button.IsActive() || !button.IsInteractable()) return false;

            var rect = (RectTransform)button.transform;
            Vector3 scale = rect.lossyScale;
            if (scale.x == 0f || scale.y == 0f) return false;

            EventSystem events = EventSystem.current;
            if (events == null) return false;

            Canvas canvas = button.GetComponentInParent<Canvas>().rootCanvas;
            Camera eye = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
            Vector2 point = RectTransformUtility.WorldToScreenPoint(eye, rect.TransformPoint(rect.rect.center));

            _hits.Clear();
            events.RaycastAll(new PointerEventData(events) { position = point }, _hits);
            return _hits.Count > 0 && _hits[0].gameObject.transform.IsChildOf(rect);
        }
    }
}
