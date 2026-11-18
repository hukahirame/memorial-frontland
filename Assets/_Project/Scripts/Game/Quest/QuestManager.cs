using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// クエストの持ち主。常駐シーンに 1 つ置く。決まりは帳簿（QuestBook）が持ち、ここは夜明け・氾濫・移動を帳簿へ渡し、
    /// 受け取った報酬を所持金・攻略度・持ち物へ入れる。知らせの文字は出さない。起きたことをイベントで伝え、画面の側（QuestNotices）が出す
    /// </summary>
    public sealed class QuestManager : MonoBehaviour
    {
        [SerializeField] private AreaTravel _travel;
        [SerializeField] private RootsManager _roots;
        [SerializeField] private Sun _sun;
        [SerializeField] private Purse _purse;
        [SerializeField] private PlayerInventory _inventory;

        private readonly QuestBook _book = new QuestBook();

        public QuestRegistry Quests => _book.Quests;

        /// <summary>調査クエストの発行計画</summary>
        public DayPlan Plan => _book.Plan;

        /// <summary>受注しているクエスト。受注していなければ空</summary>
        public string OrderedId => _book.OrderedId;

        /// <summary>拠点に戻ったときに出す結果</summary>
        public QuestOutcome PendingOutcome => _book.PendingOutcome;

        /// <summary>結果を出すクエスト。受けた根源で始めたものを指す</summary>
        public string ResultQuestId => _book.ResultQuestId;

        /// <summary>夜明けに、クエストを 1 本以上出した</summary>
        public event Action IssuedAtDawn;

        /// <summary>氾濫した根源に決壊を出した</summary>
        public event Action<Root> BreachIssued;

        /// <summary>受けたクエストの根源に入り、クエストが始まった</summary>
        public event Action<Quest> Started;

        /// <summary>クエストが 1 つ進んだ。達成したあとに対象を倒しても知らせない。サブは達成したら払ってから知らせる</summary>
        public event Action<Quest> Advanced;

        /// <summary>成功が決まったクエストを、根源に着いて受け取った。払ったものは達成済みすべての合計。拠点では結果の画面が受け取るので知らせない</summary>
        public event Action<Quest, IReadOnlyList<Reward>> Claimed;

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
            foreach (Root root in _roots.Roots.All) _book.IssueFirst(root);
        }

        /// <summary>根源 1 つに、始めの調査とサブを出す。保存のあとで足した根源にも使う</summary>
        public void IssueFirst(Root root)
        {
            _book.IssueFirst(root);
        }

        private void IssueAtDawn()
        {
            if (_book.IssueAtDawn(_roots.Roots.All)) IssuedAtDawn?.Invoke();
        }

        private void IssueBreach(Root root)
        {
            if (_book.IssueBreach(root)) BreachIssued?.Invoke(root);
        }

        /// <summary>枠を押した。受けていなければ受け、受けていれば解除する。できるかは帳簿の決まりで決まる</summary>
        public OrderResult ToggleOrder(string questId)
        {
            return _book.ToggleOrder(questId, _travel.Where.RootId);
        }

        /// <summary>そのまま受注にする。保存から戻すときに使う</summary>
        public void Order(string questId)
        {
            _book.Order(questId);
        }

        public void CancelOrder()
        {
            _book.CancelOrder();
        }

        /// <summary>受けたクエストの根源に居るか。ここで倒れると、そのクエストは失敗になる</summary>
        public bool IsUnderwayIn(string rootId)
        {
            return _book.IsUnderwayIn(rootId);
        }

        /// <summary>根源に入った。始まるのは受けたクエストの根源に入ったときだけで、ほかの根源は通り道</summary>
        public void StartOrdered()
        {
            Quest quest = _book.Start(_travel.Where.RootId);
            if (quest != null) Started?.Invoke(quest);
        }

        /// <summary>受けた根源の中で倒れた</summary>
        public void Fail()
        {
            _book.Fail();
        }

        /// <summary>拠点に戻って出す結果を受け取る。受け取ると結果は空く</summary>
        public QuestOutcome TakeOutcome()
        {
            return _book.TakeOutcome();
        }

        /// <summary>場所を離れる</summary>
        public void Leave(string rootId)
        {
            _book.Leave(rootId);
        }

        /// <summary>
        /// 場所に着いた。根源に着いたときは、成功が決まったクエストをここで受け取る。拠点では結果の画面（RewardUI）で受け取る。
        /// 根源で結果の画面を出さないのは、結果の画面は時間を止めず閉じるまで残るので、敵の前で手が止まるため
        /// </summary>
        public void Arrive(AreaKind kind)
        {
            Quest claimed = _book.Arrive(kind == AreaKind.Base, out IReadOnlyList<Quest> paid);
            if (claimed == null) return;

            Receive(paid);
            Claimed?.Invoke(claimed, QuestRewards.Total(paid));
        }

        /// <summary>プレイヤーが居る根源で target を倒した</summary>
        public void Defeat(string target)
        {
            DefeatIn(target, _travel.Where.RootId);
        }

        /// <summary>rootId の根源で target を倒した。根源の外なら rootId は null</summary>
        public void DefeatIn(string target, string rootId)
        {
            IReadOnlyList<Quest> advanced = _book.DefeatIn(target, rootId, out IReadOnlyList<Quest> paid);

            Receive(paid);
            foreach (Quest quest in advanced) Advanced?.Invoke(quest);
        }

        /// <summary>達成済みのクエスト</summary>
        public List<Quest> Cleared()
        {
            return _book.Cleared();
        }

        /// <summary>達成済みのクエストをすべて受け取る</summary>
        public void PayCleared()
        {
            Receive(_book.PayCleared());
        }

        /// <summary>受け取ったクエストの報酬を入れる。クエストは帳簿の一覧からもう消えている</summary>
        private void Receive(IReadOnlyList<Quest> paid)
        {
            foreach (Quest quest in paid)
            {
                foreach (Reward reward in quest.Rewards) Grant(quest, reward);
                Removed?.Invoke(quest.Id);
            }
        }

        private void Grant(Quest quest, Reward reward)
        {
            if (reward.Kind == RewardKind.Progress)
            {
                Root rewarded = _roots.Roots.Find(quest.RootId);
                if (rewarded != null) rewarded.Gain(reward.Amount);
            }
            else if (reward.Kind == RewardKind.Coin)
            {
                _purse.Coins.Add(reward.Amount);
            }
            else
            {
                //coin でも progress でもないものは品の報酬。持ち物へ 1 個ずつ入れ、入りきらない分は受け取れない。
                //持ち物の画面へ知らせるので、帳簿ではなくここで PlayerInventory を通して入れる
                for (int i = 0; i < reward.Amount; i++)
                {
                    if (_inventory.LoadInventory(reward.Kind, 0) != 1) break;
                }
            }
        }
    }
}
