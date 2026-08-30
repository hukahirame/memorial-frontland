using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// ソースに宣言されている型の索引。構文木から取るので、書き方の揺れに影響されない。
    /// 複数行にまたがる宣言、record、入れ子の型も同じように拾える。
    ///
    /// Legacy は UnityEngine 依存でリフレクションが使えないため、ソースから読む。
    /// </summary>
    public static class SourceIndex
    {
        private static readonly (string Layer, string Path)[] Roots =
        {
            ("Domain", "Assets/_Project/Scripts/Domain"),
            ("Game",   "Assets/_Project/Scripts/Game"),
            ("Legacy", "Assets/LegacyScripts"),
        };

        public sealed class Index
        {
            /// <summary>型名 -> 層</summary>
            public readonly Dictionary<string, string> Layers =
                new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>リポジトリからの道 -> そのファイルが宣言する型名</summary>
            public readonly Dictionary<string, List<string>> Files =
                new Dictionary<string, List<string>>(StringComparer.Ordinal);

            /// <summary>型名 -> 節。節は「層/直下のフォルダ」。直下のファイルは層そのもの</summary>
            public readonly Dictionary<string, string> Owner =
                new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>節 -> そこで名前が出た識別子と、その回数</summary>
            public readonly Dictionary<string, Dictionary<string, int>> Mentions =
                new Dictionary<string, Dictionary<string, int>>(StringComparer.Ordinal);

            /// <summary>節の一覧</summary>
            public IEnumerable<string> Nodes
            {
                get { return Owner.Values.Distinct().OrderBy(n => n, StringComparer.Ordinal); }
            }
        }

        public static Index Build()
        {
            Index index = new Index();

            foreach ((string layer, string rel) in Roots)
            {
                string dir = Path.Combine(RepositoryRoot(), rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(dir)) continue;

                foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                                                 .OrderBy(f => f, StringComparer.Ordinal))
                {
                    List<string> declared = DeclaredTypes(File.ReadAllText(file));
                    if (declared.Count == 0) continue;

                    // 型がどのファイルに宣言されたかを残す。
                    // スライスの核はファイルでも書けるため（docs/slices.txt）
                    string path = rel + "/" + file.Substring(dir.Length + 1)
                                                  .Replace(Path.DirectorySeparatorChar, '/');

                    index.Files[path] = declared;

                    string rest = path.Substring(rel.Length + 1);
                    string node = rest.Contains("/") ? layer + "/" + rest.Split('/')[0] : layer;

                    foreach (string name in declared)
                    {
                        index.Layers[name] = layer;
                        index.Owner[name] = node;
                    }

                    if (!index.Mentions.ContainsKey(node))
                        index.Mentions[node] = new Dictionary<string, int>(StringComparer.Ordinal);

                    Dictionary<string, int> seen = index.Mentions[node];
                    foreach (string name in Mentioned(File.ReadAllText(file)))
                        seen[name] = seen.ContainsKey(name) ? seen[name] + 1 : 1;
                }
            }

            return index;
        }

        /// <summary>
        /// そのソースが名前を出している識別子。出るたびに1つ返す。
        /// コメントと文字列は構文木の外なので入らない
        /// </summary>
        private static IEnumerable<string> Mentioned(string source)
        {
            return CSharpSyntaxTree.ParseText(source)
                                   .GetRoot()
                                   .DescendantNodes()
                                   .OfType<SimpleNameSyntax>()
                                   .Select(node => node.Identifier.ValueText);
        }

        /// <summary>そのソースが宣言する型の名前。class / struct / interface / enum / record</summary>
        private static List<string> DeclaredTypes(string source)
        {
            return CSharpSyntaxTree.ParseText(source)
                                   .GetRoot()
                                   .DescendantNodes()
                                   .OfType<BaseTypeDeclarationSyntax>()
                                   .Select(node => node.Identifier.ValueText)
                                   .Distinct(StringComparer.Ordinal)
                                   .OrderBy(name => name, StringComparer.Ordinal)
                                   .ToList();
        }

        /// <summary>docs/ を持つ階層まで遡る。dotnet と CI で作業ディレクトリが違うため</summary>
        public static string RepositoryRoot()
        {
            DirectoryInfo dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;

            if (dir == null) throw new DirectoryNotFoundException("docs/ を持つ階層が見つからない");

            return dir.FullName;
        }
    }
}
