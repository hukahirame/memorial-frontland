using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 関心事スライスの表。すべての型がどれかの核に入っているかを見るためだけにある。
    ///
    /// かつてはここから docs/dependencies-diagrams/ の図を描いていたが、図はやめた。
    /// 残したのは被覆の検査で、Domain に型を足したとき地図に穴が開くのを止める。
    /// 切り方の実体は docs/slices.txt にあり、この表はコードの索引でもある。
    /// </summary>
    public static class SliceTable
    {
        /// <summary>切り方の定義。人が触る唯一の入力</summary>
        public const string TablePath = "docs/slices.txt";

        private sealed class Slice
        {
            public List<string> Seeds;   // Domain/Root.cs か QuestManager
            public string Name;          // 根源 🌳
            public string Summary;
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
                            slice.Name + " の核 " + seed + " が" +
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
                        slice.Name + " の核 " + seed + " という型は無い。改名か削除をしたなら表も直すこと。");
                }
            }

            return Sorted(core);
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

        private static List<Slice> ReadSliceTable()
        {
            string path = Path.Combine(DependencyGraphGenerator.RepositoryRoot(),
                                       TablePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                throw new FileNotFoundException(TablePath + " が無い。スライスの切り方の実体はそこにある。");

            var table = new List<Slice>();
            foreach (var raw in File.ReadAllText(path).Replace("\r\n", "\n").Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                var cell = line.Split('|').Select(c => c.Trim()).ToArray();
                if (cell.Length != 3)
                    throw new InvalidOperationException(
                        "スライス表の行は「核 | 名前 | 一行説明」の3欄。合わない行: " + line);

                table.Add(new Slice
                {
                    Seeds = cell[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Name = cell[1],
                    Summary = cell[2],
                });
            }

            return table;
        }

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
