using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>受けたクエストの、拠点に戻ったときに出す結果</summary>
    public enum QuestOutcome
    {
        None,

        /// <summary>受けた根源の中で倒れた</summary>
        Failed,

        /// <summary>達成したまま、受けた根源を生きて出た</summary>
        Succeeded,
    }

    /// <summary>クエストの枠を押したときに起きたこと</summary>
    public enum OrderResult
    {
        Ordered,
        Cancelled,

        /// <summary>そのクエストがもう無い</summary>
        Missing,

        /// <summary>成功が決まったもの。受け取るまで受け直しも解除もしない</summary>
        Settled,

        /// <summary>対象の根源の中では受けられない。中ではもう始まっている</summary>
        CannotOrderInside,

        /// <summary>対象の根源の中では解除できない</summary>
        CannotCancelInside,

        /// <summary>ほかのクエストを受けている。受けられるのは全体で 1 本だけ</summary>
        OtherOrdered,
    }

    /// <summary>
    /// クエストの帳簿。一覧・受注・結果を持ち、出す・受ける・進める・受け取るの決まりを持つ。
    /// 受注できるのは全体で 1 本だけ。調査と決壊は受けた根源の中でだけ進み、達成したまま生きて根源を出ると成功が決まる。
    /// 受け取ったクエストは一覧から消して返す。報酬を持ち物や所持金へ入れるのは使う側
    /// </summary>
    public sealed class QuestBook
    {
        /// <summary>サブの対象。倒した敵の名前と突き合わせる</summary>
        public const string SubTarget = "Slime";

        /// <summary>調査と決壊の対象。根源の核の名前</summary>
        public const string CoreTarget = "MainSpawner";

        /// <summary>サブの数と報酬</summary>
        public const int SubAmount = 3;
        public const int SubCoins = 100;

        /// <summary>成功が決まり、まだ受け取っていないクエスト。着いた先ですぐ受け取る</summary>
        private string _unclaimed = "";

        public QuestRegistry Quests { get; } = new QuestRegistry();

        /// <summary>調査クエストの発行計画</summary>
        public DayPlan Plan { get; } = new DayPlan();

        /// <summary>受注しているクエスト。受注していなければ空</summary>
        public string OrderedId { get; private set; } = "";

        /// <summary>拠点に戻ったときに出す結果</summary>
        public QuestOutcome PendingOutcome { get; private set; }

        /// <summary>結果を出すクエスト。受けた根源で始めたものを指す</summary>
        public string ResultQuestId { get; private set; } = "";

        /// <summary>根源 1 つに、始めの調査とサブを出す。既にあれば足さない</summary>
        public void IssueFirst(Root root)
        {
            if (!Quests.HasMainFor(root.Id)) IssueMain(root, QuestKind.Main);
            if (!Quests.HasSubFor(root.Id)) IssueSub(root.Id);
        }

        /// <summary>調査か決壊を 1 本出す。報酬は根源の危険度で決まる</summary>
        public Quest IssueMain(Root root, QuestKind kind)
        {
            return Quests.Create(kind, root.Id, CoreTarget, Quest.CoreAmount(kind), QuestRewards.For(kind, root.Danger));
        }

        /// <summary>根源にサブを 1 本足す。足すと 1 体倒すたびに両方が進み、2 本ぶん払う</summary>
        private void IssueSub(string rootId)
        {
            Quests.Create(QuestKind.Sub, rootId, SubTarget, SubAmount, new[] { new Reward(RewardKind.Coin, SubCoins) });
        }

        /// <summary>
        /// 夜明け。サブは無ければ足す。調査は、その根源に調査か決壊が無ければ、1 度保留してから出す。初回の夜明けでは積むだけ。
        /// 1 本でも出したら true
        /// </summary>
        public bool IssueAtDawn(IEnumerable<Root> roots)
        {
            bool issued = false;

            foreach (Root root in roots)
            {
                if (!Quests.HasSubFor(root.Id))
                {
                    IssueSub(root.Id);
                    issued = true;
                }

                if (Quests.HasMainFor(root.Id) || !Plan.ShouldIssue(root.Id)) continue;

                IssueMain(root, QuestKind.Main);
                issued = true;
            }

            return issued;
        }

        /// <summary>氾濫した根源に決壊を 1 本出す。既にあれば足さずに false</summary>
        public bool IssueBreach(Root root)
        {
            if (Quests.HasBreachFor(root.Id)) return false;

            IssueMain(root, QuestKind.Breach);
            return true;
        }

        /// <summary>
        /// 枠を押した。受けていなければ受け、受けていれば解除する。hereRootId はプレイヤーの居る根源（根源の外なら null）。
        /// 受けるのも解除するのも、対象の根源の外でだけ
        /// </summary>
        public OrderResult ToggleOrder(string questId, string hereRootId)
        {
            Quest quest = Quests.Find(questId);
            if (quest == null) return OrderResult.Missing;
            if (quest.IsComplete) return OrderResult.Settled;

            bool ordered = OrderedId == quest.Id;
            if (hereRootId == quest.RootId) return ordered ? OrderResult.CannotCancelInside : OrderResult.CannotOrderInside;

            if (ordered)
            {
                OrderedId = "";
                return OrderResult.Cancelled;
            }

            //受注の Id が消えたクエストを指していれば、空いているとみなす
            if (Quests.Find(OrderedId) != null) return OrderResult.OtherOrdered;

            OrderedId = quest.Id;
            return OrderResult.Ordered;
        }

        /// <summary>そのまま受注にする。保存から戻すときに使う。決まりは見ない</summary>
        public void Order(string questId)
        {
            OrderedId = questId;
        }

        public void CancelOrder()
        {
            OrderedId = "";
        }

        /// <summary>受けたクエストの根源に居るか。ここで倒れると、そのクエストは失敗になる</summary>
        public bool IsUnderwayIn(string rootId)
        {
            Quest quest = Quests.Find(OrderedId);
            return quest != null && rootId != null && quest.RootId == rootId;
        }

        /// <summary>根源に入った。受けたクエストの根源なら始めて、そのクエストを返す。ほかの根源は通り道で、null を返す</summary>
        public Quest Start(string rootId)
        {
            Quest quest = Quests.Find(OrderedId);
            if (quest == null || rootId == null || quest.RootId != rootId) return null;

            ResultQuestId = quest.Id;
            return quest;
        }

        /// <summary>受けた根源の中で倒れた。達成していても、根源を出る前に倒れたらやり直し</summary>
        public void Fail()
        {
            PendingOutcome = QuestOutcome.Failed;
        }

        /// <summary>拠点に戻って出す結果を受け取る。受け取ると結果は空く。失敗なら、結果を出すクエストの進みを始めに戻す（受注は残す）</summary>
        public QuestOutcome TakeOutcome()
        {
            QuestOutcome outcome = PendingOutcome;
            PendingOutcome = QuestOutcome.None;

            if (outcome == QuestOutcome.Failed)
            {
                Quest failed = Quests.Find(ResultQuestId);
                if (failed != null) failed.SetProgress(0);
            }

            return outcome;
        }

        /// <summary>
        /// 場所を離れる。rootId は離れる場所の根源（根源でなければ null）。受けたクエストを達成したまま、その根源を生きて出たら成功が決まる。
        /// 受注の枠はここで空き、報酬は着いた先で受け取る
        /// </summary>
        public void Leave(string rootId)
        {
            Quest quest = Quests.Find(OrderedId);
            if (quest == null || rootId == null || quest.RootId != rootId || !quest.IsComplete) return;

            //倒れて拠点へ戻される途中。失敗のまま
            if (PendingOutcome == QuestOutcome.Failed) return;

            PendingOutcome = QuestOutcome.Succeeded;
            OrderedId = "";
            _unclaimed = quest.Id;
        }

        /// <summary>
        /// 場所に着いた。成功が決まっていて、着いたのが根源なら、ここで受け取る。拠点では結果の画面で受け取る（PayCleared）。
        /// 受け取ると達成済みのクエストをすべて一覧から消し、paid に入れる。成功が決まったクエストを返し、受け取らなければ null
        /// </summary>
        public Quest Arrive(bool atBase, out IReadOnlyList<Quest> paid)
        {
            Quest quest = Quests.Find(_unclaimed);
            _unclaimed = "";
            paid = new List<Quest>();
            if (quest == null || atBase) return null;

            PendingOutcome = QuestOutcome.None;
            paid = PayCleared();
            return quest;
        }

        /// <summary>
        /// rootId の根源で target を倒した。根源の外なら rootId は null。プレイヤーの居場所ではなく、倒した物の居た根源で進める。
        /// 進めたクエストを返す。達成したあとに倒しても進めない。サブは達成したその場で受け取り、一覧から消して paid に入れる
        /// </summary>
        public IReadOnlyList<Quest> DefeatIn(string target, string rootId, out IReadOnlyList<Quest> paid)
        {
            List<Quest> advanced = new List<Quest>();
            List<Quest> settled = new List<Quest>();

            //サブは達成したその場で一覧から消えるので、写しを回す
            foreach (Quest quest in new List<Quest>(Quests.All))
            {
                if (!quest.AdvancesOn(target) || !AdvancesIn(quest, rootId) || quest.IsComplete) continue;

                quest.Advance();
                advanced.Add(quest);

                //次の夜明けにまた出る。調査と決壊の成功が決まるのは、生きてこの根源を出たとき（Leave）
                if (quest.Kind == QuestKind.Sub && quest.IsComplete)
                {
                    Settle(quest);
                    settled.Add(quest);
                }
            }

            paid = settled;
            return advanced;
        }

        /// <summary>サブはその根源の中で、共通はどこでも進む。受けた調査と決壊は、対象の根源の中でだけ進む。通り道の根源で核を壊しても進めない</summary>
        private bool AdvancesIn(Quest quest, string rootId)
        {
            switch (quest.Kind)
            {
                case QuestKind.Common: return true;
                case QuestKind.Sub: return quest.RootId == rootId;
                default: return quest.Id == OrderedId && quest.RootId == rootId;
            }
        }

        /// <summary>達成済みのクエスト</summary>
        public List<Quest> Cleared()
        {
            List<Quest> cleared = new List<Quest>();
            foreach (Quest quest in Quests.All)
            {
                if (quest.IsComplete) cleared.Add(quest);
            }

            return cleared;
        }

        /// <summary>達成済みのクエストをすべて受け取り、一覧から消して返す</summary>
        public IReadOnlyList<Quest> PayCleared()
        {
            List<Quest> cleared = Cleared();
            foreach (Quest quest in cleared) Settle(quest);

            return cleared;
        }

        /// <summary>受け取ったクエストを一覧から消す。空けるのは受け取ったものの受注だけで、一緒に失敗したクエストは受けたまま残す</summary>
        private void Settle(Quest quest)
        {
            if (OrderedId == quest.Id) OrderedId = "";
            Quests.Remove(quest.Id);
        }
    }
}
