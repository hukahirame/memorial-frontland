using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>受けたクエストの、拠点に戻ったときに出す結果</summary>
    public enum QuestOutcome
    {
        None,
        /// <summary>受けた根源の中で倒れた</summary>
        Failed,
        /// <summary>達成したまま、受けた根源を生きて出た</summary>
        Succeeded
    }

    /// <summary>
    /// クエストの持ち主。一覧と受注を持ち、出す・進める・報酬を払う。常駐シーンに 1 つ置く。
    /// 受注できるのは全体で 1 本だけ。調査と決壊は受けた根源の中でだけ進み、達成したまま生きて根源を出ると成功が決まる
    /// </summary>
    public sealed class QuestManager : MonoBehaviour
    {
        /// <summary>調査の目的。始めたときの見出しと、調査の枠と、画面の左上の受注中の札に出す</summary>
        public const string MainObjective = "スポナーを探し出し、破壊する";

        /// <summary>決壊で核を壊したあとの目的</summary>
        public static readonly string DefeatObjective = "敵を" + Quest.BreachDefeats + "体討伐する";

        /// <summary>決壊の目的。核を壊しても終わらず、続けて敵を倒す。出す所は調査の目的と同じ</summary>
        public static readonly string BreachObjective = "スポナーを破壊し、" + DefeatObjective;

        /// <summary>達成したあとの目的。根源を出る前にその中で倒れると失敗になるので、達成した時点では達成と出さない</summary>
        public const string ReturnObjective = "生きて根源を出る";

        /// <summary>調査と決壊の対象。根源の核</summary>
        private const string CoreTarget = "MainSpawner";

        /// <summary>サブの対象と数と報酬</summary>
        private const string SubTarget = "Slime";
        private const int SubAmount = 3;
        private const int SubCoins = 100;

        private const string ProgressReward = "progress";
        private const string CoinReward = "coin";

        [SerializeField] private AreaTravel _travel;
        [SerializeField] private RootsManager _roots;
        [SerializeField] private Sun _sun;
        [SerializeField] private ItemBook _items;
        [SerializeField] private Purse _purse;
        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private BigText _bigText;
        [SerializeField] private MiddleText _middleText;

        /// <summary>成功が決まり、まだ受け取っていないクエスト。着いた先ですぐ受け取る</summary>
        private string _unannounced = "";

        public QuestRegistry Quests { get; } = new QuestRegistry();

        /// <summary>調査クエストの発行計画</summary>
        public DayPlan Plan { get; } = new DayPlan();

        /// <summary>受注しているクエスト。受注していなければ空</summary>
        public string OrderedId { get; private set; } = "";

        /// <summary>拠点に戻ったときに出す結果</summary>
        public QuestOutcome PendingOutcome { get; private set; }

        /// <summary>結果を出すクエスト。受けた根源で始めたものを指す</summary>
        public string ResultQuestId { get; private set; } = "";

        /// <summary>クエストを払って一覧から消した</summary>
        public event Action<string> Removed;

        private void OnEnable()
        {
            _sun.Morning += IssueAtDawn;
            _roots.Overflowed += IssueBreach;
        }

        private void OnDisable()
        {
            _sun.Morning -= IssueAtDawn;
            _roots.Overflowed -= IssueBreach;
        }

        /// <summary>
        /// 保存が無いまま始めたとき、根源ごとに調査とサブを 1 本ずつ出す。
        /// 夜明けの発行は保留を挟むので、ここで出さないと 2 回目の夜明けまで受けるものが無い
        /// </summary>
        public void IssueFirst()
        {
            foreach (Root root in _roots.Roots.All) IssueFirst(root);
        }

        /// <summary>根源 1 つに、始めの調査とサブを出す。保存のあとで足した根源にも使う</summary>
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

        /// <summary>根源にサブを 1 本足す。夜明けと始めたときとで同じものを出す。足すと 1 体倒すたびに両方が進み、2 本ぶん払う</summary>
        private void IssueSub(string rootId)
        {
            Quests.Create(QuestKind.Sub, rootId, SubTarget, SubAmount, new[] { new Reward(CoinReward, SubCoins) });
        }

        /// <summary>
        /// 夜明け。サブは無ければ足す。調査は、その根源に調査か決壊が無ければ、1 度保留してから出す。初回の夜明けでは積むだけ
        /// </summary>
        private void IssueAtDawn()
        {
            bool issued = false;

            foreach (Root root in _roots.Roots.All)
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

            if (issued) _middleText.Midtxt_Anim("新クエストが発生しました");
        }

        /// <summary>氾濫した根源に決壊を 1 本出す。既にあれば足さない</summary>
        private void IssueBreach(Root root)
        {
            if (Quests.HasBreachFor(root.Id)) return;

            IssueMain(root, QuestKind.Breach);
            _middleText.Midtxt_Anim(root.Name + "に決壊クエストが出ました");
        }

        /// <summary>受注する。受注できるかは呼ぶ側が決める</summary>
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

        /// <summary>受けたクエストの根源に入った。始まるのは受けたクエストの根源に入ったときだけで、ほかの根源は通り道</summary>
        public void StartOrdered()
        {
            Quest quest = Quests.Find(OrderedId);
            if (quest == null || quest.RootId != _travel.Where.RootId) return;

            ResultQuestId = quest.Id;
            _bigText.Bigtxt_Anim(Headline(quest, CurrentObjective(quest)), "開始");
        }

        /// <summary>受けた根源の中で倒れた。達成していても、根源を出る前に倒れたらやり直し</summary>
        public void Fail()
        {
            PendingOutcome = QuestOutcome.Failed;
        }

        /// <summary>拠点に戻って出す結果を受け取る。受け取ると結果は空く</summary>
        public QuestOutcome TakeOutcome()
        {
            QuestOutcome outcome = PendingOutcome;
            PendingOutcome = QuestOutcome.None;
            return outcome;
        }

        /// <summary>
        /// 場所を離れる。受けたクエストを達成したまま、その根源を生きて出たら成功が決まる。
        /// 受注の枠はここで空き、報酬は着いた先ですぐ受け取る
        /// </summary>
        public void Leave(string rootId)
        {
            Quest quest = Quests.Find(OrderedId);
            if (quest == null || rootId == null || quest.RootId != rootId || !quest.IsComplete) return;

            //倒れて拠点へ戻される途中。失敗のまま
            if (PendingOutcome == QuestOutcome.Failed) return;

            PendingOutcome = QuestOutcome.Succeeded;
            OrderedId = "";
            _unannounced = quest.Id;
        }

        /// <summary>
        /// 場所に着いた。成功が決まっていれば、ここで受け取る。拠点では結果の画面（RewardUI）で受け取るので何もしない。
        /// 根源では画面を出さず、払った分を大きい文字に添える。結果の画面は時間を止めず閉じるまで残るので、根源で開くと敵の前で手が止まる
        /// </summary>
        public void Arrive(AreaKind kind)
        {
            Quest quest = Quests.Find(_unannounced);
            _unannounced = "";
            if (quest == null || kind == AreaKind.Base) return;

            //受け取るとクエストは一覧から消える。見出しに使う quest は、受け取る前に引いてある
            PendingOutcome = QuestOutcome.None;
            string paid = Describe(Total(Cleared()));
            PayCleared();
            _bigText.Bigtxt_Anim(Headline(quest, paid), "成功");
        }

        /// <summary>プレイヤーが居る根源で target を倒した</summary>
        public void Defeat(string target)
        {
            DefeatIn(target, _travel.Where.RootId);
        }

        /// <summary>rootId の根源で target を倒した。根源の外なら rootId は null。プレイヤーの居場所ではなく、倒した物の居た根源で進める</summary>
        public void DefeatIn(string target, string rootId)
        {
            //サブは達成したその場で一覧から消えるので、写しを回す
            foreach (Quest quest in new List<Quest>(Quests.All))
            {
                if (!quest.AdvancesOn(target)) continue;

                if (quest.Kind == QuestKind.Sub && quest.RootId == rootId) Advance(quest);
                else if (quest.Kind == QuestKind.Common) Advance(quest);
                //受けた調査と決壊は、対象の根源の中でだけ進む。通り道の根源で核を壊しても進めない
                else if (quest.Id == OrderedId && quest.RootId == rootId) Advance(quest);
            }
        }

        private void Advance(Quest quest)
        {
            //達成したあとも、対象を倒すたびに呼ばれる。知らせるのは達成した 1 回だけ
            if (quest.IsComplete) return;

            quest.Advance();

            if (quest.Kind == QuestKind.Main || quest.Kind == QuestKind.Breach)
            {
                //大きい文字は、核を壊したときと、決壊で敵を倒しきったときだけ。倒した数は受注中の札に出る。
                //成功が決まるのは、生きてこの根源を出たとき（Leave）
                string word = quest.DefeatedAfterCore == 0 ? "破壊" : quest.IsComplete ? "討伐" : null;
                if (word != null) _bigText.Bigtxt_Anim(Headline(quest, CurrentObjective(quest)), word);
            }
            else if (!quest.IsComplete) return;
            else if (quest.Kind == QuestKind.Sub)
            {
                //サブは達成したその場で払って一覧から消す。次の夜明けにまた出る
                string paid = Pay(quest);
                _middleText.Midtxt_Anim("サブクエスト達成：" + Objective(quest) + "（" + paid + "）");
            }
            else _middleText.Midtxt_Anim("共通クエスト達成：" + Objective(quest));
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

        /// <summary>達成済みのクエストをすべて払って一覧から消す</summary>
        public void PayCleared()
        {
            //消すのは添字ではなく Id で行う。添字は1つ消すたびに後ろがずれる
            foreach (Quest quest in Cleared()) Pay(quest);
        }

        /// <summary>1 本ぶん払って一覧から消す。払ったものを 1 行で返す</summary>
        private string Pay(Quest quest)
        {
            foreach (Reward reward in quest.Rewards) Grant(quest, reward);

            //空けるのは受け取ったものの受注だけ。一緒に失敗したクエストは受けたまま残す
            if (OrderedId == quest.Id) OrderedId = "";

            Quests.Remove(quest.Id);
            Removed?.Invoke(quest.Id);
            return Describe(quest.Rewards);
        }

        private void Grant(Quest quest, Reward reward)
        {
            if (reward.Kind == ProgressReward)
            {
                Root rewarded = _roots.Roots.Find(quest.RootId);
                if (rewarded != null) rewarded.Gain(reward.Amount);
            }
            else if (reward.Kind == CoinReward)
            {
                _purse.Coins.Add(reward.Amount);
            }
            else
            {
                //coin でも progress でもないものは品の報酬。持ち物へ 1 個ずつ入れ、入りきらない分は受け取れない
                for (int i = 0; i < reward.Amount; i++)
                {
                    if (_inventory.LoadInventory(reward.Kind, 0) != 1) break;
                }
            }
        }

        /// <summary>その根源の攻略度が報酬で伸びる量</summary>
        public static int ProgressGain(List<Quest> quests, string rootId)
        {
            int gain = 0;
            foreach (Quest quest in quests)
            {
                if (quest.RootId != rootId) continue;

                foreach (Reward reward in quest.Rewards)
                {
                    if (reward.Kind == ProgressReward) gain += reward.Amount;
                }
            }

            return gain;
        }

        /// <summary>種類ごとに足し合わせる。並びは最初に出た順</summary>
        public static List<Reward> Total(List<Quest> quests)
        {
            List<Reward> total = new List<Reward>();
            foreach (Quest quest in quests)
            {
                foreach (Reward reward in quest.Rewards)
                {
                    int at = total.FindIndex(found => found.Kind == reward.Kind);
                    if (at < 0) total.Add(reward);
                    else total[at] = new Reward(reward.Kind, total[at].Amount + reward.Amount);
                }
            }

            return total;
        }

        /// <summary>払ったものを 1 行に。「コイン x 100、攻略度 x 15」</summary>
        private string Describe(IEnumerable<Reward> rewards)
        {
            List<string> paid = new List<string>();
            foreach (Reward reward in rewards) paid.Add(RewardName(reward.Kind) + " x " + reward.Amount);

            return string.Join("、", paid);
        }

        /// <summary>報酬の表示名。品の報酬は原簿の表示名で出す</summary>
        public string RewardName(string kind)
        {
            if (kind == ProgressReward) return "攻略度";
            if (kind == CoinReward) return "コイン";

            ItemDefinition item = _items.Items.Find(kind);
            return item != null ? item.Name : kind;
        }

        /// <summary>サブと共通の目的</summary>
        public static string Objective(Quest quest)
        {
            return TargetName(quest.Target) + "を" + quest.Amount + "体討伐する";
        }

        /// <summary>討伐の対象の表示名。対象は倒した敵の GameObject の名前と突き合わせるので、名前のまま持つ</summary>
        private static string TargetName(string target)
        {
            return target == SubTarget ? "スライム" : target;
        }

        /// <summary>開始時と、根源の核を壊したとき、成功が決まったときに出す見出し。決壊クエストは種別の名前を差し替える</summary>
        private string Headline(Quest quest, string objective)
        {
            return "「" + _roots.Roots.Find(quest.RootId).Name + "」" + KindName(quest) + "クエスト\n\n" + objective;
        }

        /// <summary>調査か決壊か。見出しと受注中の札に出す</summary>
        public static string KindName(Quest quest)
        {
            return quest.Kind == QuestKind.Breach ? "決壊" : "調査";
        }

        /// <summary>調査か決壊の、いまの目的。見出しと受注中の札に出す</summary>
        public static string CurrentObjective(Quest quest)
        {
            if (quest.IsComplete) return ReturnObjective;
            if (quest.Kind != QuestKind.Breach) return MainObjective;

            return quest.CoreBroken ? DefeatObjective : BreachObjective;
        }
    }
}
