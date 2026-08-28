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
    ///
    /// Legacy はこの文字列を4通りの書き方で読み直していた。
    /// 先頭一致、部分一致、2文字のどちらか、先頭1文字の切り出し。
    /// 連番が数字なので今はどれも同じ答えを出すが、規則が4つあること自体が危うい。
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
    ///
    /// Legacy では達成を2箇所で判定していた。進捗を進める側は int に直して
    /// 「以上」で見て、UI 側は文字列の等値で見ていた。後者が成り立つのは
    /// 前者が達成時に現在量を目標量へ切り詰めているからで、離れた2箇所の
    /// 暗黙の約束に支えられていた。ここに集めて約束を1つにする。
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
}
