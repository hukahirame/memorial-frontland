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
        private Whereabouts _whereabouts;
        private MiddleText _middleText;

        /// <summary>枠を並べる側が渡す</summary>
        public void Bind(string questId, QuestManager quests, QuestBoard board, Whereabouts whereabouts, MiddleText middleText)
        {
            _questId = questId;
            _quests = quests;
            _board = board;
            _whereabouts = whereabouts;
            _middleText = middleText;
        }

        /// <summary>
        /// ボタンから呼ぶ。受注と解除は、どちらも枠の表示ではなく受注の状態で決める。表示は掲示板を開いたまま移ると古いまま残る
        /// </summary>
        public void Order()
        {
            Quest quest = _quests.Quests.Find(_questId);
            if (quest == null) return;

            //成功が決まったものは、拠点で受け取るまで受け直しも解除もしない
            if (quest.IsComplete) return;

            bool ordered = _quests.OrderedId == quest.Id;

            //受けるのも解除するのも、対象の根源の外でだけ。中ではもう始まっている
            if (_whereabouts.RootId == quest.RootId)
            {
                _middleText.Midtxt_Anim(ordered ? "この根源の中では解除できません" : "この根源の中では受注できません");
                return;
            }

            if (ordered)
            {
                _quests.CancelOrder();
                _board.Show(quest.RootId);
                _middleText.Midtxt_Anim("クエストを解除しました");
                return;
            }

            if (_quests.Quests.Find(_quests.OrderedId) != null)
            {
                _middleText.Midtxt_Anim("これ以上受注できません");
                return;
            }

            _quests.Order(quest.Id);
            _board.Show(quest.RootId);
            _middleText.Midtxt_Anim("クエストを受注しました");
        }
    }
}
