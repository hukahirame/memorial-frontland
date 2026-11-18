using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 根源の詳細。地図の札を押すと真ん中に開き、名前・危険度・攻略度・蓄積の段を出し、近道のボタンとクエストの枠を添える。
    /// 枠は根源どうしで使い回す。値は開いたときのもので、開いているあいだは書き直さない
    /// </summary>
    public sealed class RootDetail : MonoBehaviour
    {
        /// <summary>達していない段の色。シーンに置いたときの色</summary>
        private static readonly Color Unreached = new Color(0.47058824f, 0.47058824f, 0.47058824f, 1f);

        /// <summary>湧きを抑える段と、核の在りかが分かる段に達したときの色</summary>
        private static readonly Color SuppressReached = new Color(0, 50, 0, 255);
        private static readonly Color RevealReached = new Color(50, 0, 50, 255);

        [SerializeField] private RootsManager _roots;
        [SerializeField] private QuestBoard _board;
        [SerializeField] private RootShortcut _shortcut;

        [Tooltip("開いたときに置く所")]
        [SerializeField] private Transform _center;

        [SerializeField] private TextMeshProUGUI _name;
        [SerializeField] private Text _danger;
        [SerializeField] private Text _progress;

        [Tooltip("湧きを抑える段の字")]
        [SerializeField] private Text _suppressStage;

        [Tooltip("核の在りかが分かる段の字")]
        [SerializeField] private Text _revealStage;

        [SerializeField] private Text _accumulation;

        public void Show(string rootId)
        {
            Root root = _roots.Roots.Find(rootId);
            if (root == null) return;

            transform.position = _center.position;

            _name.text = "「" + root.Name + "」";
            _danger.text = "危険度 " + root.Danger;
            _progress.text = "　攻略度 " + root.Progress + "％";
            //達していない段も塗り直さないと、前に開いた根源の色が残る
            _suppressStage.color = root.Progress >= Root.SuppressProgress ? SuppressReached : Unreached;
            _revealStage.color = root.Progress >= Root.RevealProgress ? RevealReached : Unreached;

            ShowAccumulation(root.Level);

            _shortcut.Show(root.Id);
            _board.Show(root.Id);
        }

        //閾値は Domain の AccumulationLevel が持つ
        private void ShowAccumulation(AccumulationLevel level)
        {
            switch (level)
            {
                case AccumulationLevel.Stampede: Paint("-", Color.black); break;
                case AccumulationLevel.High: Paint("高", Color.red); break;
                case AccumulationLevel.Medium: Paint("中", Color.yellow); break;
                case AccumulationLevel.Small: Paint("小", Color.green); break;
                default: Paint("微", Color.blue); break;
            }
        }

        private void Paint(string text, Color color)
        {
            _accumulation.text = text;
            _accumulation.color = color;
        }
    }
}
