using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>押しているあいだボタンを少し縮め、離すか指が外れたら戻す</summary>
    public sealed class PressFeedback : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        /// <summary>押しているあいだの大きさ。もとの大きさに対する比</summary>
        public const float PressedScale = 0.92f;

        private Selectable _selectable;
        private Vector3 _released;
        private bool _pressed;

        /// <summary>root より下のボタンのうち、まだ付いていないものすべてに付ける</summary>
        public static void AttachAll(Transform root)
        {
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
            {
                if (!button.TryGetComponent<PressFeedback>(out _)) button.gameObject.AddComponent<PressFeedback>();
            }
        }

        private void Awake()
        {
            _selectable = GetComponent<Selectable>();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_pressed || (_selectable != null && !_selectable.IsInteractable())) return;

            _released = transform.localScale;
            transform.localScale = _released * PressedScale;
            _pressed = true;
        }

        public void OnPointerUp(PointerEventData eventData) => Release();

        public void OnPointerExit(PointerEventData eventData) => Release();

        private void OnDisable() => Release();

        //押しているあいだに、隠すために大きさを 0 にするような書き換えが入ったら、そちらを残す
        private void Release()
        {
            if (!_pressed) return;

            _pressed = false;
            if (transform.localScale == _released * PressedScale) transform.localScale = _released;
        }
    }
}
