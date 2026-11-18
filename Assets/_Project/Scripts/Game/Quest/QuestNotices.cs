using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// クエストに起きたことを、知らせの文字で出す。出た・始まった・核を壊した・倒しきった・達成した・受け取った。
    /// 拠点で受け取る結果は結果の画面（RewardUI）が出す
    /// </summary>
    public sealed class QuestNotices : MonoBehaviour
    {
        [SerializeField] private QuestManager _quests;
        [SerializeField] private RootsManager _roots;
        [SerializeField] private ItemBook _items;
        [SerializeField] private MiddleText _middleText;
        [SerializeField] private BigText _bigText;

        private void OnEnable()
        {
            _quests.IssuedAtDawn += ShowIssued;
            _quests.BreachIssued += ShowBreachIssued;
            _quests.Started += ShowStarted;
            _quests.Advanced += ShowAdvanced;
            _quests.Claimed += ShowClaimed;
        }

        private void OnDisable()
        {
            _quests.IssuedAtDawn -= ShowIssued;
            _quests.BreachIssued -= ShowBreachIssued;
            _quests.Started -= ShowStarted;
            _quests.Advanced -= ShowAdvanced;
            _quests.Claimed -= ShowClaimed;
        }

        private void ShowIssued()
        {
            _middleText.Show("新クエストが発生しました");
        }

        private void ShowBreachIssued(Root root)
        {
            _middleText.Show(root.Name + "に決壊クエストが出ました");
        }

        private void ShowStarted(Quest quest)
        {
            _bigText.Show(Headline(quest, QuestText.Objective(quest)), "開始");
        }

        private void ShowAdvanced(Quest quest)
        {
            if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
            {
                //大きい文字は、核を壊したときと、決壊で敵を倒しきったときだけ。倒した数は受注中の札に出る
                string word = quest.DefeatedAfterCore == 0 ? "破壊" : quest.IsComplete ? "討伐" : null;
                if (word != null) _bigText.Show(Headline(quest, QuestText.Objective(quest)), word);
            }
            else if (!quest.IsComplete) return;
            else if (quest.Kind == QuestKind.Sub)
            {
                _middleText.Show("サブクエスト達成：" + QuestText.Objective(quest) + "（" + QuestText.Rewards(quest.Rewards, _items.Items) + "）");
            }
            else _middleText.Show("共通クエスト達成：" + QuestText.Objective(quest));
        }

        //根源では結果の画面を出さないので、払った分を大きい文字に添える
        private void ShowClaimed(Quest quest, IReadOnlyList<Reward> paid)
        {
            _bigText.Show(Headline(quest, QuestText.Rewards(paid, _items.Items)), "成功");
        }

        /// <summary>大きい文字の見出し。決壊クエストは種別の名前を差し替える</summary>
        private string Headline(Quest quest, string body)
        {
            return "「" + _roots.Roots.Find(quest.RootId).Name + "」" + QuestText.KindName(quest) + "クエスト\n\n" + body;
        }
    }
}
