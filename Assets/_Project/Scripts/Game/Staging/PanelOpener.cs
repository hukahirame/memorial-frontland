using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 画面を開け閉めするボタンの呼び先。閉じた画面は見えない所へ退け、開くと決めた所に置く
    /// </summary>
    public sealed class PanelOpener : MonoBehaviour
    {
        /// <summary>閉じた画面を置く所。どの画面からも外れる</summary>
        private static readonly Vector3 Away = new Vector3(0f, 5000f, 0f);

        [Tooltip("開け閉めする画面")]
        [SerializeField, FormerlySerializedAs("rt")] private RectTransform _panel;

        [Tooltip("開いた画面を置く所")]
        [SerializeField, FormerlySerializedAs("center")] private RectTransform _center;

        [SerializeField] private SoundEffects _sounds;

        public void Open()
        {
            _panel.position = _center.position;
            _sounds.Play("Fantasy_Game_Action_Backpack_Open");
        }

        public void Close()
        {
            _panel.position = Away;
        }
    }
}
