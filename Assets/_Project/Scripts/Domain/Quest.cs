using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// クエストの種別。ID の先頭1文字に埋め込まれている。
    ///
    /// 文字は Legacy の保存データと GameObject の名前に出るので変えられない。
    /// 値の名前だけを日本語の意味に合わせてある。
    /// </summary>
    public enum QuestKind
    {
        /// <summary>調査。根源1つにつき1本まで</summary>
        Main,

        /// <summary>決壊。氾濫した根源に出る</summary>
        Breach,

        /// <summary>サブ。同じシーンにいる間だけ進む</summary>
        Sub,

        /// <summary>共通。どのシーンでも進む</summary>
        Common,
    }

    /// <summary>
    /// クエストの ID。「種別1文字 + 連番」という文字列である。
    /// 読み方はここに集める。ID を直に切り出して種別を判定しない。
    /// </summary>
    public static class QuestId
    {
        /// <summary>種別を表す文字。enum の並びと同じ順</summary>
        private const string Letters = "XYSC";

        /// <summary>種別に対応する文字。ID を組み立てるときに使う</summary>
        public static char LetterOf(QuestKind kind)
        {
            return Letters[(int)kind];
        }

        /// <summary>ID から種別を読む。読めない ID は false</summary>
        public static bool TryReadKind(string id, out QuestKind kind)
        {
            kind = default;
            if (string.IsNullOrEmpty(id)) return false;

            int at = Letters.IndexOf(id[0]);
            if (at < 0) return false;

            kind = (QuestKind)at;
            return true;
        }

        /// <summary>その ID が指定の種別か</summary>
        public static bool Is(string id, QuestKind kind)
        {
            return TryReadKind(id, out QuestKind actual) && actual == kind;
        }
    }

    /// <summary>
    /// クエストの進捗。目標量と現在量の関係だけを持つ。
    /// 達成の判定はここだけで行う。表示側で別に判定しない。
    /// </summary>
    public static class QuestProgress
    {
        /// <summary>1つ進めた現在量。目標量を超えない</summary>
        public static int Advance(int current, int target)
        {
            return Clamp(current + 1, target);
        }

        /// <summary>目標量で頭打ちにした現在量。負の値は0にする</summary>
        public static int Clamp(int current, int target)
        {
            if (current < 0) return 0;

            return current > target ? target : current;
        }

        /// <summary>達成しているか</summary>
        public static bool IsComplete(int current, int target)
        {
            return current >= target;
        }
    }

    /// <summary>
    /// クエストの報酬1種。種類と量を対で持つ。
    /// </summary>
    public readonly struct Reward
    {
        /// <summary>coin、progress、またはアイテムID</summary>
        public string Kind { get; }
        public int Amount { get; }

        public Reward(string kind, int amount)
        {
            Kind = kind;
            Amount = amount;
        }
    }

    /// <summary>
    /// クエスト1つ。
    /// </summary>
    public sealed class Quest
    {
        public string Id { get; }
        public QuestKind Kind { get; }

        /// <summary>対象の根源 Id。共通クエストでは Common が入る</summary>
        public string RootId { get; }

        /// <summary>討伐や収集の対象。Slime、MainSpawner など</summary>
        public string Target { get; }

        /// <summary>目標量</summary>
        public int Amount { get; }

        /// <summary>現在量。Amount を超えない</summary>
        public int Progress { get; private set; }

        public IReadOnlyList<Reward> Rewards { get; }

        public Quest(string id, string rootId, string target, int amount, IReadOnlyList<Reward> rewards)
        {
            Id = id;
            Kind = QuestId.TryReadKind(id, out QuestKind kind) ? kind : QuestKind.Main;
            RootId = rootId;
            Target = target;
            Amount = amount;
            Rewards = rewards ?? new List<Reward>();
        }

        public bool IsComplete
        {
            get { return QuestProgress.IsComplete(Progress, Amount); }
        }

        /// <summary>1つ進める。目標を超えない</summary>
        public void Advance()
        {
            Progress = QuestProgress.Advance(Progress, Amount);
        }

        /// <summary>読み込みなど、途中の値をそのまま入れるとき。目標で頭打ちにする</summary>
        public void SetProgress(int value)
        {
            Progress = QuestProgress.Clamp(value, Amount);
        }
    }

    /// <summary>
    /// クエストの唯一の窓口。報酬はクエストと対にして持つ。
    /// </summary>
    public sealed class QuestRegistry
    {
        private readonly List<Quest> _quests = new List<Quest>();

        public IReadOnlyList<Quest> All
        {
            get { return _quests; }
        }

        public int Count
        {
            get { return _quests.Count; }
        }

        /// <summary>
        /// 採番して追加する。番号は空きを前から探す。
        /// 「その種別の現在数」を番号にすると、消したあとに Id が衝突する。
        /// </summary>
        public Quest Create(QuestKind kind, string rootId, string target, int amount, IReadOnlyList<Reward> rewards)
        {
            char letter = QuestId.LetterOf(kind);

            int number = 0;
            while (Find(letter + number.ToString()) != null) number++;

            Quest quest = new Quest(letter + number.ToString(), rootId, target, amount, rewards);
            _quests.Add(quest);

            return quest;
        }

        /// <summary>見つからなければ null</summary>
        public Quest Find(string id)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].Id == id) return _quests[i];
            }

            return null;
        }

        /// <summary>達成して片付けるとき。報酬も一緒に消える</summary>
        public bool Remove(string id)
        {
            Quest quest = Find(id);
            if (quest == null) return false;

            return _quests.Remove(quest);
        }

        /// <summary>その根源に、調査か決壊のクエストが既にあるか</summary>
        public bool HasMainFor(string rootId)
        {
            for (int i = 0; i < _quests.Count; i++)
            {
                if (_quests[i].RootId != rootId) continue;
                if (_quests[i].Kind == QuestKind.Main || _quests[i].Kind == QuestKind.Breach) return true;
            }

            return false;
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _quests.Clear();
        }
    }
}
