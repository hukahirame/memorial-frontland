using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// docs/dependencies-diagrams/ は人が手で維持する現状確認用の図。
    /// 放っておくと腐るので、腐り方を2つだけ機械で見張る。
    ///
    ///   - 実在しない依存が描かれていないか
    ///   - Domain / Game の型が、どのスライスにも出ていないことはないか
    ///
    /// どう切るか（何を1枚にまとめるか）は人の判断なので、そこは見ない。
    /// </summary>
    public class SliceDiagramTests
    {
        private const string SliceDir = "docs/dependencies-diagrams";
        private const string GraphFile = "docs/dependencies-diagrams/graph.txt";

        [Test]
        public void スライス図に実在しない依存が描かれていない()
        {
            var (layers, edges, cofile) = ReadGraph();
            var wrong = new List<string>();

            foreach (var (name, from, to) in SliceEdges())
            {
                if (edges.Contains(from + " -> " + to)) continue;
                if (edges.Contains(to + " -> " + from)) continue;
                if (cofile.Contains(Pair(from, to))) continue;

                wrong.Add(name + ": " + from + " --> " + to);
            }

            Assert.IsEmpty(wrong,
                "ソースに無い依存が図に描かれている。" +
                "コードが変わったなら図を直し、図が正しいならコードを疑うこと。" +
                Environment.NewLine + string.Join(Environment.NewLine, wrong));
        }

        [Test]
        public void DomainとGameの型がどれかのスライスに出ている()
        {
            var (layers, _, _) = ReadGraph();

            var drawn = new HashSet<string>(StringComparer.Ordinal);
            foreach (var (_, from, to) in SliceEdges()) { drawn.Add(from); drawn.Add(to); }

            var missing = layers
                .Where(kv => kv.Value == "Domain" || kv.Value == "Game")
                .Select(kv => kv.Key)
                .Where(t => !drawn.Contains(t))
                .OrderBy(t => t, StringComparer.Ordinal)
                .ToList();

            Assert.IsEmpty(missing,
                "どのスライス図にも出てこない型がある。" +
                "新しく作った型は docs/dependencies-diagrams/ のどれかに足すこと。" +
                Environment.NewLine + string.Join(", ", missing));
        }

        private static string Pair(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? a + " " + b : b + " " + a;
        }

        private static (Dictionary<string, string>, HashSet<string>, HashSet<string>) ReadGraph()
        {
            string path = Path.Combine(RepositoryRoot(), GraphFile);
            Assert.IsTrue(File.Exists(path), GraphFile + " が無い。先に dotnet test を実行すること");

            var layers = new Dictionary<string, string>(StringComparer.Ordinal);
            var edges = new HashSet<string>(StringComparer.Ordinal);
            var cofile = new HashSet<string>(StringComparer.Ordinal);
            string section = "";

            foreach (var raw in File.ReadAllLines(path))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#")) continue;
                if (line.StartsWith("[")) { section = line; continue; }

                if (section == "[types]")
                {
                    var parts = line.Split(' ');
                    layers[parts[0]] = parts[1];
                }
                else if (section == "[edges]")
                {
                    // 行は "A -> B assoc" の4語。ここでは種類を見ないので落とす
                    var parts = line.Split(' ');
                    edges.Add(parts[0] + " -> " + parts[2]);
                }
                else if (section == "[cofile]")
                {
                    var types = line.Split(' ');
                    foreach (var a in types)
                        foreach (var b in types)
                            if (a != b) cofile.Add(Pair(a, b));
                }
            }

            return (layers, edges, cofile);
        }

        private static IEnumerable<(string File, string From, string To)> SliceEdges()
        {
            string dir = Path.Combine(RepositoryRoot(), SliceDir);

            foreach (var file in Directory.GetFiles(dir, "*.md").OrderBy(f => f, StringComparer.Ordinal))
            {
                foreach (var raw in File.ReadAllLines(file))
                {
                    var m = Regex.Match(raw, @"^\s+([A-Za-z_][A-Za-z0-9_]*)\s+-->\s+([A-Za-z_][A-Za-z0-9_]*)\s*$");
                    if (m.Success) yield return (Path.GetFileName(file), m.Groups[1].Value, m.Groups[2].Value);
                }
            }
        }

        private static string RepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;

            Assert.IsNotNull(dir, "docs/ を持つ階層が見つからない");
            return dir.FullName;
        }
    }
}
