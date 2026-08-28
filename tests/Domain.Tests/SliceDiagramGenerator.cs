using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// docs/dependencies-diagrams/ の現状図（関心事スライス）を作る。
    ///
    /// 手で維持する図は必ず腐るので、素データから作り直す。
    /// 切り方は「Domain のソースファイル1つ = 図1枚」。ファイル分割そのものが
    /// 関心事の区切りなので、どこで切るかに人の判断を要らなくできる。
    ///
    /// 人が決めるのは見出しと一行説明だけで、それは SKILL.md のスライス表にある。
    /// </summary>
    public static class SliceDiagramGenerator
    {
        public const string OutputDir = "docs/dependencies-diagrams";
        public const string SkillPath = ".claude/skills/class-diff-diagram/SKILL.md";

        /// <summary>ここから下は手書き。作り直しても残す</summary>
        public const string NoteHeading = "## 覚え書き";

        private sealed class Slice
        {
            public List<string> Seeds;   // Domain/Root.cs か QuestManager
            public string File;          // roots.md
            public string Title;         // 根源 🌳
            public string Summary;
        }

        /// <summary>出力ファイル名 -> 本文。ファイルには書かない</summary>
        public static Dictionary<string, string> GenerateAll()
        {
            var graph = new Graph(DependencyGraphGenerator.Generate());
            var result = new Dictionary<string, string>(StringComparer.Ordinal);
            var owner = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var slice in ReadSliceTable())
            {
                var core = Resolve(slice, graph);

                // 同じ型が2枚の核に入ると、どちらを見ればよいか決まらなくなる
                foreach (var t in core)
                {
                    if (owner.TryGetValue(t, out var other))
                        throw new InvalidOperationException(
                            t + " が " + other + " と " + slice.File + " の両方の核に入っている。" +
                            "核はどちらか片方にすること。");

                    owner[t] = slice.File;
                }

                result[slice.File] = Render(slice, core, graph);
            }

            return result;
        }

        /// <summary>どのスライスの核にも入っていない型。放っておくと地図に穴が開く</summary>
        public static List<string> UncoveredTypes()
        {
            var graph = new Graph(DependencyGraphGenerator.Generate());
            var covered = new HashSet<string>(StringComparer.Ordinal);

            foreach (var slice in ReadSliceTable())
                foreach (var t in Resolve(slice, graph))
                    covered.Add(t);

            return Sorted(graph.Layers.Keys.Where(t => !covered.Contains(t)));
        }

        /// <summary>核の指定を型の並びに直す。ファイルならそれが宣言する型に展開する</summary>
        private static List<string> Resolve(Slice slice, Graph graph)
        {
            var core = new List<string>();

            foreach (var seed in slice.Seeds)
            {
                if (seed.EndsWith(".cs", StringComparison.Ordinal))
                {
                    var hit = graph.Files.Keys.Where(p => p == seed || p.EndsWith("/" + seed, StringComparison.Ordinal)).ToList();
                    if (hit.Count != 1)
                        throw new InvalidOperationException(
                            slice.File + " の核 " + seed + " が" +
                            (hit.Count == 0 ? "見つからない。" : "複数のファイルに当たる。もっと長い道で書くこと。"));

                    core.AddRange(graph.Files[hit[0]]);
                }
                else if (graph.Layers.ContainsKey(seed))
                {
                    core.Add(seed);
                }
                else
                {
                    throw new InvalidOperationException(
                        slice.File + " の核 " + seed + " という型は無い。改名か削除をしたなら表も直すこと。");
                }
            }

            return Sorted(core);
        }

        // ------------------------------------------------------------------
        // 図
        // ------------------------------------------------------------------

        private static string Render(Slice slice, List<string> core, Graph graph)
        {
            var coreSet = new HashSet<string>(core, StringComparer.Ordinal);

            // 辺は少なくとも片端が核のものだけ。核どうしを繋がない Legacy 間の関係まで
            // 描くと、Domain を中心に見るという目的から外れて読めなくなる
            var edges = graph.Edges
                             .Where(e => coreSet.Contains(e.From) || coreSet.Contains(e.To))
                             .OrderBy(e => e.From + " " + e.To, StringComparer.Ordinal)
                             .ToList();

            var nodes = new HashSet<string>(coreSet, StringComparer.Ordinal);
            foreach (var e in edges) { nodes.Add(e.From); nodes.Add(e.To); }

            var sb = new StringBuilder();
            sb.Append("<!-- 自動生成。図を手で直さない。dotnet test が作り直す。\n");
            sb.Append("     見出しと一行説明は " + SkillPath + " のスライス表にある。\n");
            sb.Append("     末尾の覚え書きの節だけが手書きで、作り直しても消えない。\n");
            sb.Append("     枠の色: 青 = Domain / 橙 = Game / 灰 = Legacy（境界として置いているだけ）\n");
            sb.Append("     メンバは Domain / Game の核の公開分だけ。Legacy の中身は載せない。\n");
            sb.Append("     線: 太線 = 属性として保持する関係 / 点線 = 本体の中で使うだけの関係 -->\n\n");

            sb.Append("# " + slice.Title + "\n\n");
            sb.Append(slice.Summary + "\n\n");

            sb.Append("```mermaid\n");
            sb.Append("graph LR\n");

            // Domain と Game は層として囲む。Legacy は境界を示すだけなので裸で置く
            foreach (var layer in new[] { "Domain", "Game" })
            {
                var inLayer = Sorted(nodes.Where(n => graph.Layer(n) == layer));
                if (inLayer.Count == 0) continue;

                sb.Append("  subgraph " + layer + "\n");
                foreach (var n in inLayer) sb.Append("    " + Node(n, coreSet, graph) + "\n");
                sb.Append("  end\n");
            }

            foreach (var n in Sorted(nodes.Where(n => graph.Layer(n) == "Legacy")))
                sb.Append("  " + Node(n, coreSet, graph) + "\n");

            foreach (var e in edges)
                sb.Append("  " + e.From + (e.Kind == "assoc" ? " ==> " : " -.-> ") + e.To + "\n");

            sb.Append("  classDef domain fill:#e8f0fe,stroke:#1967d2,color:#174ea6;\n");
            sb.Append("  classDef game   fill:#fef7e0,stroke:#b06000,color:#8a5300;\n");
            sb.Append("  classDef legacy fill:#f1f3f4,stroke:#5f6368,color:#202124;\n");

            foreach (var (layer, style) in new[] { ("Domain", "domain"), ("Game", "game"), ("Legacy", "legacy") })
            {
                var inLayer = Sorted(nodes.Where(n => graph.Layer(n) == layer));
                if (inLayer.Count == 0) continue;

                sb.Append("  class " + string.Join(",", inLayer) + " " + style + ";\n");
            }

            sb.Append("```\n\n");
            sb.Append(ReadNote(slice.File));

            return sb.ToString();
        }

        private static List<string> Sorted(IEnumerable<string> names)
        {
            return names.OrderBy(n => n, StringComparer.Ordinal).ToList();
        }

        /// <summary>
        /// 節点1つ。Domain / Game の核だけ公開メンバを載せる。
        ///
        /// 非公開を載せないのは、この図が「この関心事が外に何を差し出しているか」を
        /// 見るものだから。中の作りは差分図とテストで見る。
        /// Legacy を載せないのは、公開フィールドの多くが Inspector への口であって
        /// 設計ではないため。並べても関心事の輪郭が見えない。
        /// </summary>
        private static string Node(string type, HashSet<string> core, Graph graph)
        {
            if (!core.Contains(type) || graph.Layer(type) == "Legacy") return type;
            if (!graph.Members.TryGetValue(type, out var all)) return type;

            // 全行が公開なので + は情報を持たない。行頭から落として横幅を稼ぐ
            var rows = all.Where(IsPublic)
                          .Select(d => Regex.Replace(d, @"^\+ ", ""))
                          .ToList();
            if (rows.Count == 0) return type;

            var attrs = rows.Where(r => !r.Contains("(")).ToList();   // 括弧があれば操作
            var ops = rows.Where(r => r.Contains("(")).ToList();

            // 区切り線の長さを一番長い行に合わせる。固定長だと短くなりがちで区切りに見えない。
            // アンダースコアは他のラテン文字と幅が近いので、文字数を合わせれば幅もおおよそ合う
            string rule = new string('_', rows.Concat(new[] { type }).Max(r => r.Length));

            var parts = new List<string> { type };
            if (attrs.Count > 0) { parts.Add(rule); parts.AddRange(attrs); }
            if (ops.Count > 0) { parts.Add(rule); parts.AddRange(ops); }

            // 色は付けない。層ごとの classDef が color: を持ち、mermaid がそれに
            // !important を付けるので、ここで span を巻いても上書きされる
            return type + "[\"" + string.Join("<br/>", parts.Select(Escape)) + "\"]";
        }

        /// <summary>enum の値には可視性の記号が付かない。記号が無ければ公開扱い</summary>
        private static bool IsPublic(string display)
        {
            return !Regex.IsMatch(display, @"^[-#~] ");
        }

        /// <summary>素データは C# のまま &lt;&gt; を持つ。HTML のタグと解釈されるので逃がす</summary>
        private static string Escape(string text)
        {
            return text.Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");
        }

        /// <summary>既にある図から手書き部分を拾う。無ければ空の節を置く</summary>
        private static string ReadNote(string file)
        {
            string path = Path.Combine(DependencyGraphGenerator.RepositoryRoot(),
                                       OutputDir.Replace('/', Path.DirectorySeparatorChar), file);
            if (File.Exists(path))
            {
                string text = File.ReadAllText(path).Replace("\r\n", "\n");
                // 行頭で探す。同じ字面が図の説明の中にあっても拾わないため
                int at = text.StartsWith(NoteHeading, StringComparison.Ordinal) ? 0 : -1;
                if (at < 0)
                {
                    int found = text.IndexOf("\n" + NoteHeading, StringComparison.Ordinal);
                    if (found >= 0) at = found + 1;
                }

                if (at >= 0) return text.Substring(at).TrimEnd('\n') + "\n";
            }

            return NoteHeading + "\n\n（まだ無い）\n";
        }

        // ------------------------------------------------------------------
        // SKILL.md のスライス表
        // ------------------------------------------------------------------

        private static List<Slice> ReadSliceTable()
        {
            string path = Path.Combine(DependencyGraphGenerator.RepositoryRoot(),
                                       SkillPath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                throw new FileNotFoundException(SkillPath + " が無い。スライス表の実体はそこにある。");

            string text = File.ReadAllText(path).Replace("\r\n", "\n");
            var block = Regex.Match(text, @"<!-- SLICES:BEGIN -->\s*```text\n(.*?)\n```\s*<!-- SLICES:END -->",
                                    RegexOptions.Singleline);
            if (!block.Success)
                throw new InvalidOperationException(SkillPath + " に SLICES:BEGIN / END のブロックが無い。");

            var table = new List<Slice>();
            foreach (var raw in block.Groups[1].Value.Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                var cell = line.Split('|').Select(c => c.Trim()).ToArray();
                if (cell.Length != 4)
                    throw new InvalidOperationException(
                        "スライス表の行は「核 | 出力名 | 見出し | 一行説明」の4欄。合わない行: " + line);

                table.Add(new Slice
                {
                    Seeds = cell[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                    File = cell[1],
                    Title = cell[2],
                    Summary = cell[3],
                });
            }

            return table;
        }

        // ------------------------------------------------------------------
        // 素データ
        // ------------------------------------------------------------------

        private sealed class Graph
        {
            public readonly Dictionary<string, string> Layers = new Dictionary<string, string>(StringComparer.Ordinal);
            public readonly Dictionary<string, List<string>> Files = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            public readonly Dictionary<string, List<string>> Members = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            public readonly List<(string From, string To, string Kind)> Edges = new List<(string, string, string)>();

            public Graph(string text)
            {
                string section = "";
                foreach (var raw in text.Replace("\r\n", "\n").Split('\n'))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("[", StringComparison.Ordinal)) { section = line; continue; }

                    if (section == "[members]")
                    {
                        // <型>|<メンバ名>|<表示>。名前は並べ替えに使い終わっているので落とす
                        var cell = line.Split(new[] { '|' }, 3);
                        if (!Members.TryGetValue(cell[0], out var list))
                            Members[cell[0]] = list = new List<string>();

                        list.Add(cell[2]);
                        continue;
                    }

                    var w = line.Split(' ');
                    if (section == "[types]") Layers[w[0]] = w[1];
                    else if (section == "[files]") Files[w[0]] = w.Skip(1).ToList();
                    else if (section == "[edges]") Edges.Add((w[0], w[2], w[3]));
                }
            }

            public string Layer(string type)
            {
                return Layers.TryGetValue(type, out var layer) ? layer : "Legacy";
            }
        }
    }
}
