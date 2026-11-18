using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>根源の詳細に出す近道のボタン。拠点に居るときだけ見える。払う額は攻略度で決まる</summary>
    public sealed class RootShortcut : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField] private RootsManager _roots;
        [SerializeField, FormerlySerializedAs("purse")] private Purse _purse;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private MiddleText _middleText;
        [SerializeField, FormerlySerializedAs("label")] private Text _label;

        [Tooltip("押して移るときに閉じる画面。根源の詳細と地図")]
        [SerializeField, FormerlySerializedAs("closes")] private CloseManager[] _closes;

        /// <summary>詳細の枠は根源どうしで使い回すので、開くたびに差し替える</summary>
        private string _rootId;

        /// <summary>出している額。押したときの額と違えば、払わせずに出し直す</summary>
        private int _shownFee;

        /// <summary>根源の詳細を開いたとき。払う額は開いたときの攻略度で出す</summary>
        public void Show(string rootId)
        {
            _rootId = rootId;

            _shownFee = TravelRule.ShortcutFeeTo(_roots.Roots.Find(rootId));
            _label.text = "近道で向かう（" + (_shownFee == 0 ? "無料" : _shownFee + " コイン") + "）";
            gameObject.SetActive(!TravelRule.Shortcut(_travel.Where.AreaId, rootId, _roots.RootIds()).IsRefused);
        }

        /// <summary>ボタンから呼ぶ。出している額だけを払う。開いたあとに拠点を出ていたら移らない</summary>
        public void Go()
        {
            if (!_movement.CanAct) return;

            //開いたまま零時を越えると、攻略度が段を割って額が変わる。見せていない額は取らない
            int fee = TravelRule.ShortcutFeeTo(_roots.Roots.Find(_rootId));
            if (fee != _shownFee)
            {
                Show(_rootId);
                _middleText.Midtxt_Anim("近道の料金が変わりました");
                return;
            }

            if (!_purse.Coins.CanAfford(fee))
            {
                _middleText.Midtxt_Anim("所持金が足りません");
                return;
            }

            if (!_travel.Shortcut(_rootId, _roots.RootIds())) return;

            _purse.Coins.Spend(fee);
            foreach (CloseManager close in _closes) close.CloseClick();
        }
    }
}
