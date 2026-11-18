using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>クエストの枠を押して、受注と解除をする。調査と決壊だけ</summary>
    public sealed class QuestButton : MonoBehaviour
    {
        private string _questId;
        private QuestManager _quests;
        private QuestBoard _board;
        private MiddleText _middleText;

        /// <summary>枠を並べる側が渡す</summary>
        public void Bind(string questId, QuestManager quests, QuestBoard board, MiddleText middleText)
        {
            _questId = questId;
            _quests = quests;
            _board = board;
            _middleText = middleText;
        }

        /// <summary>
        /// ボタンから呼ぶ。受注と解除は、どちらも枠の表示ではなく受注の状態で決める（QuestBook.ToggleOrder）。表示は掲示板を開いたまま移ると古いまま残る
        /// </summary>
        public void Order()
        {
            Quest quest = _quests.Quests.Find(_questId);
            if (quest == null) return;

            switch (_quests.ToggleOrder(_questId))
            {
                case OrderResult.Ordered:
                    _board.Show(quest.RootId);
                    _middleText.Show("クエストを受注しました");
                    break;
                case OrderResult.Cancelled:
                    _board.Show(quest.RootId);
                    _middleText.Show("クエストを解除しました");
                    break;
                case OrderResult.CannotOrderInside:
                    _middleText.Show("この根源の中では受注できません");
                    break;
                case OrderResult.CannotCancelInside:
                    _middleText.Show("この根源の中では解除できません");
                    break;
                case OrderResult.OtherOrdered:
                    _middleText.Show("これ以上受注できません");
                    break;
            }
        }
    }
}
