using System.Collections.Generic;
using MemorialFloor.Domain;

namespace MemorialFloor.Game
{
    /// <summary>クエストを画面に出すときの言葉。掲示板・受注中の札・結果の画面・知らせで同じ言葉を使う</summary>
    public static class QuestText
    {
        private static readonly string DefeatAfterCore = "敵を" + Quest.BreachDefeats + "体討伐する";

        /// <summary>いまの目的</summary>
        public static string Objective(Quest quest)
        {
            return Describe(quest.Objective, quest);
        }

        /// <summary>出たばかりのときの目的。掲示板の枠に出す</summary>
        public static string FirstObjective(Quest quest)
        {
            return Describe(Quest.FirstObjective(quest.Kind), quest);
        }

        /// <summary>調査か決壊か。見出しと受注中の札に出す</summary>
        public static string KindName(Quest quest)
        {
            return quest.Kind == QuestKind.Breach ? "決壊" : "調査";
        }

        /// <summary>報酬の名前。品の報酬は原簿の表示名で出す</summary>
        public static string RewardName(string kind, ItemCatalog items)
        {
            if (kind == RewardKind.Progress) return "攻略度";
            if (kind == RewardKind.Coin) return "コイン";

            ItemDefinition item = items.Find(kind);
            return item != null ? item.Name : kind;
        }

        /// <summary>報酬を 1 行に。「コイン x 100、攻略度 x 15」</summary>
        public static string Rewards(IEnumerable<Reward> rewards, ItemCatalog items)
        {
            List<string> named = new List<string>();
            foreach (Reward reward in rewards) named.Add(RewardName(reward.Kind, items) + " x " + reward.Amount);

            return string.Join("、", named);
        }

        private static string Describe(QuestObjective objective, Quest quest)
        {
            switch (objective)
            {
                case QuestObjective.BreakCore: return "スポナーを探し出し、破壊する";
                case QuestObjective.BreakCoreThenDefeat: return "スポナーを破壊し、" + DefeatAfterCore;
                case QuestObjective.DefeatAfterCore: return DefeatAfterCore;
                case QuestObjective.LeaveRoot: return "生きて根源を出る";
                default: return TargetName(quest.Target) + "を" + quest.Amount + "体討伐する";
            }
        }

        /// <summary>討伐の対象の表示名。対象は倒した敵の GameObject の名前と突き合わせるので、クエストは名前のまま持つ</summary>
        private static string TargetName(string target)
        {
            return target == QuestBook.SubTarget ? "スライム" : target;
        }
    }
}
