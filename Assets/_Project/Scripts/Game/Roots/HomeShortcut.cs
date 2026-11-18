using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>地図に出す、拠点へ戻る近道のボタン。根源に居るときだけ見え、受けたクエストの対象の根源では押せない</summary>
    public sealed class HomeShortcut : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField] private RootsManager _roots;
        [SerializeField] private QuestManager _quests;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField, FormerlySerializedAs("button")] private Button _button;
        [SerializeField, FormerlySerializedAs("label")] private Text _label;

        [Tooltip("押して移るときに閉じる画面。地図と根源の詳細")]
        [SerializeField, FormerlySerializedAs("closes")] private CloseManager[] _closes;

        /// <summary>地図を開いたとき</summary>
        public void Show()
        {
            bool questHere = _quests.IsUnderwayIn(_travel.Where.RootId);
            _label.text = questHere ? "クエスト中は戻れません" : "拠点へ戻る（無料）";
            _button.interactable = !questHere;
            gameObject.SetActive(!TravelRule.ShortcutHome(_travel.Where.AreaId, _roots.RootIds()).IsRefused);
        }

        /// <summary>ボタンから呼ぶ。地図を開いたあとに受けたクエストの根源へ入っていたら戻らない</summary>
        public void Go()
        {
            if (!_movement.CanAct) return;
            if (_quests.IsUnderwayIn(_travel.Where.RootId)) return;
            if (!_travel.ShortcutHome(_roots.RootIds())) return;

            foreach (CloseManager close in _closes) close.Close();
        }
    }
}
