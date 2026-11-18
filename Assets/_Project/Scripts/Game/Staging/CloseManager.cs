using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 地図と根源の詳細の、開け閉めの呼び先。閉じた画面は見えない所へ退け、開くと決めた所に置く。
    /// 持ち物や作業台と違い、開くときに音を鳴らさない（鳴らすのは PanelOpener）
    /// </summary>
    public sealed class CloseManager : MonoBehaviour
    {
        /// <summary>閉じた画面を置く所。どの画面からも外れる</summary>
        private static readonly Vector3 Away = new Vector3(0f, 5000f, 0f);

        [Tooltip("開け閉めする画面")]
        [SerializeField, FormerlySerializedAs("rt")] private RectTransform _panel;

        [Tooltip("開いた画面を置く所")]
        [SerializeField, FormerlySerializedAs("center")] private RectTransform _center;

        public void Open()
        {
            _panel.position = _center.position;
        }

        public void Close()
        {
            _panel.position = Away;
        }
    }
}
