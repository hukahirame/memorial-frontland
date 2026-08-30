using System;
using System.Collections.Generic;
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

        /// <summary>節 -> 依存先の節と、その本数</summary>
        public static SortedDictionary<string, SortedDictionary<string, int>> Edges()
        {
            SourceIndex.Index index = SourceIndex.Build();
            var edges = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);

            foreach (string node in index.Nodes)
            {
                var to = new SortedDictionary<string, int>(StringComparer.Ordinal);

                HashSet<string> mentioned;
                if (index.Mentions.TryGetValue(node, out mentioned))
                {
                    foreach (string name in mentioned)
                    {
                        string owner;
                        if (!index.Owner.TryGetValue(name, out owner)) continue;
                        if (owner == node) continue;

                        to[owner] = to.ContainsKey(owner) ? to[owner] + 1 : 1;
                    }
                }

                edges[node] = to;
            }

            return edges;
        }

        public static string Generate()
        {
            var edges = Edges();

            var fanIn = new SortedDictionary<string, SortedDictionary<string, int>>(StringComparer.Ordinal);
            foreach (string node in edges.Keys) fanIn[node] = new SortedDictionary<string, int>(StringComparer.Ordinal);
            foreach (var from in edges)
                foreach (var to in from.Value)
                    fanIn[to.Key][from.Key] = to.Value;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("# 依存関係一覧表");
            sb.AppendLine();
            sb.AppendLine("<!-- DependencyListTests が生成する。手で編集しない -->");
            sb.AppendLine();
            sb.AppendLine("- フォルダ（Craft, Inventory）単位。節は「層/直下のフォルダ」");
            sb.AppendLine("- →（fan out）：依存\"先\"の数");
            sb.AppendLine("- ←（fan in） ：依存\"元\"の数");
            sb.AppendLine("- 構文木から数える。コメントと文字列は入らない。");
            sb.AppendLine("  var で受けた依存は型名が字面に出ないため見えない");
            sb.AppendLine();
            sb.AppendLine("## fan-out 昇順テーブル");
            sb.AppendLine();
            sb.AppendLine("| 節 | → | ← |");
            sb.AppendLine("|---|---|---|");

            foreach (string node in edges.Keys
                                        .OrderBy(n => edges[n].Count)
                                        .ThenBy(n => n, StringComparer.Ordinal))
            {
                sb.AppendLine("| `" + node + "` | " + edges[node].Count + " | " + fanIn[node].Count + " |");
            }

            sb.AppendLine();
            sb.AppendLine("## 詳細");

            foreach (string node in edges.Keys)
            {
                sb.AppendLine();
                sb.AppendLine("### " + node);
                sb.AppendLine();

                if (edges[node].Count == 0 && fanIn[node].Count == 0)
                {
                    sb.AppendLine("    （依存なし）");
                    continue;
                }

                foreach (var to in edges[node]) sb.AppendLine("    → " + to.Key + " " + to.Value);
                foreach (var from in fanIn[node]) sb.AppendLine("    ← " + from.Key + " " + from.Value);
            }

            return sb.ToString();
        }
    }
}
