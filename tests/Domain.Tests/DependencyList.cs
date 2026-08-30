using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// フォルダ間の依存を数えて docs/dependency-list.md を作る。
    /// 節は「層/直下のフォルダ」。Legacy/Inventory と Game/Craft は同じ高さに並ぶ。
    ///
    /// 構文木から拾うのでコメントと文字列は入らない。ただし var で受けた依存は
    /// 字面に型名が出ないため見えない。数字は目安で、大小関係を見るためのもの。
    /// </summary>
    public static class DependencyList
    {
        public const string OutputPath = "docs/dependency-list.md";

        /// <summary>markdown の強制改行。これが無いと描画時に前後の行が繋がる</summary>
        private const string HardBreak = "  ";

        /// <summary>ここから下が生成される。これより上は手書きで、作り直しても残す</summary>
        private const string TableHeading = "## fan-out 昇順テーブル";

        /// <summary>
        /// 冒頭の説明。既存のファイルから引き継ぐ。
        /// 表と詳細だけを作り直し、人が書いた説明は消さない
        /// </summary>
        private static string Preface()
        {
            string full = Path.Combine(SourceIndex.RepositoryRoot(),
                                       OutputPath.Replace('/', Path.DirectorySeparatorChar));

            if (File.Exists(full))
            {
                string text = File.ReadAllText(full);
                int at = text.IndexOf(TableHeading, StringComparison.Ordinal);

                if (at >= 0) return text.Substring(0, at);
            }

            StringBuilder head = new StringBuilder();
            head.AppendLine("# 依存関係一覧表");
            head.AppendLine();
            head.AppendLine("<!-- DependencyListTests が生成する。手で編集しない -->");
            head.AppendLine();
            head.AppendLine("- フォルダ間のクラス依存数");
            head.AppendLine("- 🟦 Domain ／ 🟩 Game ／ 🟨 Legacy");
            head.AppendLine("- ↑（fan out）：依存する");
            head.AppendLine("- ↓（fan in） ：依存される");
            head.AppendLine("- 構文木から数える。コメントと文字列は入らない。");
            head.AppendLine("  var で受けた依存は、そのフォルダ内のどこにも型名が出なければ見えない");
            head.AppendLine();

            return head.ToString();
        }

        /// <summary>節 -> 依存先の節 -> そこで名指しした型と回数</summary>
        public static SortedDictionary<string, SortedDictionary<string, Dictionary<string, int>>> Edges()
        {
            SourceIndex.Index index = SourceIndex.Build();
            var edges = new SortedDictionary<string, SortedDictionary<string, Dictionary<string, int>>>(StringComparer.Ordinal);

            foreach (string node in index.Nodes)
            {
                var to = new SortedDictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

                Dictionary<string, int> mentioned;
                if (index.Mentions.TryGetValue(node, out mentioned))
                {
                    foreach (var pair in mentioned)
                    {
                        string owner;
                        if (!index.Owner.TryGetValue(pair.Key, out owner)) continue;
                        if (owner == node) continue;

                        if (!to.ContainsKey(owner)) to[owner] = new Dictionary<string, int>(StringComparer.Ordinal);
                        to[owner][pair.Key] = pair.Value;
                    }
                }

                edges[node] = to;
            }

            return edges;
        }

        /// <summary>「Root **7**, RootRegistry **2**」の形。多い順</summary>
        private static string Detail(Dictionary<string, int> types)
        {
            return string.Join(", ", types.OrderByDescending(t => t.Value)
                                          .ThenBy(t => t.Key, StringComparer.Ordinal)
                                          .Select(t => t.Key + " **" + t.Value + "**"));
        }

        /// <summary>層は絵文字が示すので、表示は親を省いたフォルダ名だけにする</summary>
        private static string Short(string node)
        {
            int slash = node.IndexOf('/');

            return slash < 0 ? node : node.Substring(slash + 1);
        }

        /// <summary>関心事の絵文字。docs/slices.txt の名前に合わせてある</summary>
        private static string Icon(string node)
        {
            switch (Short(node))
            {
                case "Domain":    return "💠";
                case "Roots":     return "🌳";
                case "Inventory": return "🎒";
                case "Craft":     return "🔨";
                case "Quest":     return "📜";
                case "Day":       return "☀️";
                case "Save":      return "💾";
                case "Player":    return "🚶";
                case "Enemy":     return "👾";
                case "OutField":  return "🌲";
                case "Scene":     return "🚪";
                case "Staging":   return "🎥";
                default:          return "📁";
            }
        }

        /// <summary>層で色を振る。Legacy が減って Domain と Game が増えるのが見た目に出る</summary>
        private static string Mark(string node)
        {
            if (node.StartsWith("Domain", StringComparison.Ordinal)) return "🟦";
            if (node.StartsWith("Game", StringComparison.Ordinal)) return "🟩";

            return "🟨";
        }

        private static int Total(Dictionary<string, int> types)
        {
            return types.Values.Sum();
        }

        public static string Generate()
        {
            var edges = Edges();

            var fanIn = new SortedDictionary<string, SortedDictionary<string, Dictionary<string, int>>>(StringComparer.Ordinal);
            foreach (string node in edges.Keys) fanIn[node] = new SortedDictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);
            foreach (var from in edges)
                foreach (var to in from.Value)
                    fanIn[to.Key][from.Key] = to.Value;

            StringBuilder sb = new StringBuilder();
            sb.Append(Preface());
            sb.AppendLine(TableHeading);
            sb.AppendLine();
            sb.AppendLine("| 節 | ↑ | ↓ |");
            sb.AppendLine("|---|---|---|");

            foreach (string node in edges.Keys
                                        .OrderBy(n => edges[n].Count)
                                        .ThenBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine("| " + Mark(node) + " `" + Short(node) + "` | **" + edges[node].Count +
                              "** | **" + fanIn[node].Count + "** |");
            }

            sb.AppendLine();
            sb.AppendLine("## 詳細");

            foreach (string node in edges.Keys)
            {
                sb.AppendLine();
                sb.AppendLine("### " + Mark(node) + Short(node) + " " + Icon(node) + " ");
                sb.AppendLine();

                if (edges[node].Count == 0 && fanIn[node].Count == 0)
                {
                    sb.AppendLine("（依存なし）");
                    continue;
                }

                foreach (var to in edges[node])
                    sb.AppendLine("↑ " + Mark(to.Key) + " `" + Short(to.Key) + "` **" + Total(to.Value) +
                                  "** — " + Detail(to.Value) + HardBreak);

                // ↑ と ↓ の間は空ける
                if (edges[node].Count > 0 && fanIn[node].Count > 0) sb.AppendLine();

                foreach (var from in fanIn[node])
                    sb.AppendLine("↓ " + Mark(from.Key) + " `" + Short(from.Key) + "` **" + Total(from.Value) +
                                  "** — " + Detail(from.Value) + HardBreak);
            }

            return sb.ToString();
        }
    }
}
