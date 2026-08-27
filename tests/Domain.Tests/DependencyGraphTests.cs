using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// _Project と LegacyScripts の型どうしの依存を1行1辺で書き出し、
    /// docs/dependencies.md と一致するか見る。食い違えば書き換えて失敗する。
    ///
    /// Legacy は UnityEngine に依存していてリフレクションで読めないため、
    /// ソースを文字列として解析する。コメントと文字列リテラルは除いてから
    /// 名前を照合するので誤検出は少ないが、厳密ではない。
    /// 図ではなく一覧にしているのは、60型の図が読めないため。
    /// 近傍の図が要るときはこの一覧から起こす。
    /// </summary>
    public class DependencyGraphTests
    {
        private const string GraphPath = "docs/dependencies-diagrams/graph.txt";

        private static readonly (string Layer, string Path)[] Roots =
        {
            ("Domain", "Assets/_Project/Scripts/Domain"),
            ("Game",   "Assets/_Project/Scripts/Game"),
            ("Legacy", "Assets/LegacyScripts"),
        };

        [Test]
        public void 依存の素データがソースと一致する()
        {
            string expected = Generate();
            string full = Path.Combine(RepositoryRoot(), GraphPath);
            string actual = File.Exists(full) ? File.ReadAllText(full) : null;

            if (Normalize(actual) == Normalize(expected)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, expected.Replace("\n", Environment.NewLine));
            Assert.Fail(
                GraphPath + " がソースと一致していなかったため書き換えた。\n" +
                "tools/diagram-diff.ps1 で変化を図にし、dependencies-diagrams/ を更新すること。");
        }

        private static string Generate()
        {
            var (declaredIn, files) = Scan();

            var edges = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var (types, body) in files)
            {
                var own = new HashSet<string>(types, StringComparer.Ordinal);

                foreach (var target in declaredIn.Keys)
                {
                    if (own.Contains(target)) continue;
                    if (!Regex.IsMatch(body, @"\b" + Regex.Escape(target) + @"\b")) continue;

                    foreach (var source in types) edges.Add(source + " -> " + target);
                }
            }

            var sb = new StringBuilder();
            sb.Append("# 機械用の素データ。tools/diagram-diff.ps1 が読む。\n");
            sb.Append("# 人が読む図は docs/dependencies-diagrams/ と docs/dependencies-diff-diagrams/。\n");
            sb.Append("# DependencyGraphTests が生成する。手で編集しない。\n");
            sb.Append("#\n");
            sb.Append("# ソースの字面から拾っているので厳密ではない。\n");
            sb.Append("# - ファイル単位で参照を拾い、宣言する型すべてに割り当てる\n");
            sb.Append("# - var で受けている依存は型名が現れないので出ない\n");
            sb.Append("# - コメントと文字列リテラルは除いてから照合している\n");

            sb.Append("\n[types]\n");
            foreach (var kv in declaredIn.OrderBy(k => k.Key, StringComparer.Ordinal))
                sb.Append(kv.Key + " " + kv.Value + "\n");

            //同一ファイルに宣言された型どうしは辺が出ないため、同居を別に記録する。
            //スライス図の検査で「実在しない依存」と誤判定しないため
            sb.Append("\n[cofile]\n");
            foreach (var line in files.Where(f => f.Types.Length > 1)
                                      .Select(f => string.Join(" ", f.Types.OrderBy(t => t, StringComparer.Ordinal)))
                                      .Distinct().OrderBy(x => x, StringComparer.Ordinal))
                sb.Append(line + "\n");

            sb.Append("\n[edges]\n");
            foreach (var e in edges) sb.Append(e + "\n");

            return sb.ToString();
        }

        private static (Dictionary<string, string>, List<(string[] Types, string Body)>) Scan()
        {
            string root = RepositoryRoot();
            var declaredIn = new Dictionary<string, string>(StringComparer.Ordinal);
            var files = new List<(string[] Types, string Body)>();

            foreach (var (layer, rel) in Roots)
            {
                string dir = Path.Combine(root, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                                              .OrderBy(f => f, StringComparer.Ordinal))
                {
                    string body = Strip(File.ReadAllText(file));
                    var types = Regex.Matches(body, @"\b(?:class|struct|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)")
                                     .Select(m => m.Groups[1].Value).Distinct().ToArray();
                    foreach (var t in types) declaredIn[t] = layer;
                    files.Add((types, body));
                }
            }

            return (declaredIn, files);
        }

        /// <summary>コメントと文字列リテラルを落とす。中の単語を依存と誤認しないため</summary>
        private static string Strip(string source)
        {
            var sb = new StringBuilder(source.Length);
            bool inLine = false, inBlock = false, inText = false, inChar = false, verbatim = false;

            for (int i = 0; i < source.Length; i++)
            {
                char c = source[i];
                char next = i + 1 < source.Length ? source[i + 1] : '\0';

                if (inLine) { if (c == '\n') { inLine = false; sb.Append(c); } continue; }
                if (inBlock) { if (c == '*' && next == '/') { inBlock = false; i++; } continue; }

                if (inText)
                {
                    if (!verbatim && c == '\\') { i++; continue; }
                    if (verbatim && c == '"' && next == '"') { i++; continue; }
                    if (c == '"') inText = false;
                    continue;
                }
                if (inChar)
                {
                    if (c == '\\') { i++; continue; }
                    if (c == '\'') inChar = false;
                    continue;
                }

                if (c == '/' && next == '/') { inLine = true; i++; continue; }
                if (c == '/' && next == '*') { inBlock = true; i++; continue; }
                if (c == '@' && next == '"') { inText = true; verbatim = true; i++; continue; }
                if (c == '"') { inText = true; verbatim = false; continue; }
                if (c == '\'') { inChar = true; continue; }

                sb.Append(c);
            }

            return sb.ToString();
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace("\r\n", "\n");
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
