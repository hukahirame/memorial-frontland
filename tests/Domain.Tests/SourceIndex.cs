using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
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

            /// <summary>宣言されている「型.メンバ」。仕様書が名指しした先が在るかを見る</summary>
            public readonly HashSet<string> Members = new HashSet<string>(StringComparer.Ordinal);

            /// <summary>
            /// 「型.メンバ」-> const の値。const だけを入れる。
            /// SerializeField はシーンの値が優先されるため、ソースの初期値と一致しない
            /// </summary>
            public readonly Dictionary<string, string> Constants =
                new Dictionary<string, string>(StringComparer.Ordinal);

            /// <summary>層 -> 公開されている型とメンバの署名。増減を差分として見るため</summary>
            public readonly Dictionary<string, SortedSet<string>> PublicApi =
                new Dictionary<string, SortedSet<string>>(StringComparer.Ordinal);

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

                    foreach ((string member, string value) in DeclaredMembers(File.ReadAllText(file)))
                    {
                        index.Members.Add(member);
                        if (value != null) index.Constants[member] = value;
                    }

                    if (!index.PublicApi.ContainsKey(layer))
                        index.PublicApi[layer] = new SortedSet<string>(StringComparer.Ordinal);

                    foreach (string signature in PublicSignatures(File.ReadAllText(file)))
                        index.PublicApi[layer].Add(signature);
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

        /// <summary>
        /// そのソースが宣言する「型.メンバ」と、const ならその値。const 以外は値が null。
        /// メソッド・プロパティ・フィールド・enum の値を拾う。
        /// 公開の別は見ない。仕様書は private な定数も名指しするため
        /// </summary>
        private static IEnumerable<(string Member, string Value)> DeclaredMembers(string source)
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();

            foreach (SyntaxNode node in root.DescendantNodes())
            {
                BaseTypeDeclarationSyntax owner = node.FirstAncestorOrSelf<BaseTypeDeclarationSyntax>();
                if (owner == null) continue;

                string type = owner.Identifier.ValueText;

                if (node is MethodDeclarationSyntax method)
                {
                    yield return (type + "." + method.Identifier.ValueText, null);
                }
                else if (node is PropertyDeclarationSyntax property)
                {
                    yield return (type + "." + property.Identifier.ValueText, null);
                }
                else if (node is EnumMemberDeclarationSyntax enumMember)
                {
                    yield return (type + "." + enumMember.Identifier.ValueText, null);
                }
                else if (node is FieldDeclarationSyntax field)
                {
                    bool isConst = field.Modifiers.Any(m => m.ValueText == "const");

                    foreach (VariableDeclaratorSyntax declarator in field.Declaration.Variables)
                    {
                        string name = type + "." + declarator.Identifier.ValueText;
                        yield return (name, isConst ? Literal(declarator.Initializer) : null);
                    }
                }
            }
        }

        /// <summary>
        /// 公開されている型とメンバの署名。使う側から見える面だけを並べる。
        /// メソッドは引数の型と戻り値まで出す。引数を1つ足したことが差分に出るように
        /// </summary>
        private static IEnumerable<string> PublicSignatures(string source)
        {
            SyntaxNode root = CSharpSyntaxTree.ParseText(source).GetRoot();

            foreach (BaseTypeDeclarationSyntax declaration in
                     root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>())
            {
                if (!IsPublic(declaration.Modifiers)) continue;

                string type = declaration.Identifier.ValueText;
                yield return type;

                // インタフェースのメンバは修飾子が無くても公開されている
                bool openByDefault = declaration is InterfaceDeclarationSyntax;

                if (declaration is EnumDeclarationSyntax enumeration)
                {
                    foreach (EnumMemberDeclarationSyntax value in enumeration.Members)
                        yield return type + "." + value.Identifier.ValueText;

                    continue;
                }

                if (!(declaration is TypeDeclarationSyntax body)) continue;

                foreach (MemberDeclarationSyntax member in body.Members)
                {
                    if (member is BaseTypeDeclarationSyntax) continue; // 入れ子の型は外側で拾う

                    if (member is MethodDeclarationSyntax method)
                    {
                        if (!openByDefault && !IsPublic(method.Modifiers)) continue;

                        yield return type + "." + method.Identifier.ValueText
                                   + Parameters(method.ParameterList) + " : " + method.ReturnType;
                    }
                    else if (member is ConstructorDeclarationSyntax constructor)
                    {
                        if (!IsPublic(constructor.Modifiers)) continue;

                        yield return type + ".ctor" + Parameters(constructor.ParameterList);
                    }
                    else if (member is PropertyDeclarationSyntax property)
                    {
                        if (!openByDefault && !IsPublic(property.Modifiers)) continue;

                        yield return type + "." + property.Identifier.ValueText + " : " + property.Type;
                    }
                    else if (member is FieldDeclarationSyntax field)
                    {
                        if (!IsPublic(field.Modifiers)) continue;

                        foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                        {
                            yield return type + "." + variable.Identifier.ValueText
                                       + " : " + field.Declaration.Type;
                        }
                    }
                }
            }
        }

        private static bool IsPublic(SyntaxTokenList modifiers)
        {
            return modifiers.Any(m => m.ValueText == "public");
        }

        private static string Parameters(BaseParameterListSyntax list)
        {
            if (list == null) return "()";

            return "(" + string.Join(", ", list.Parameters.Select(p => p.Type.ToString())) + ")";
        }

        /// <summary>初期値が素の数か文字なら、その字面。式なら null</summary>
        private static string Literal(EqualsValueClauseSyntax initializer)
        {
            if (initializer == null) return null;

            if (initializer.Value is LiteralExpressionSyntax literal) return literal.Token.ValueText;

            // -10f のような符号付きは前置演算子になる
            if (initializer.Value is PrefixUnaryExpressionSyntax unary
                && unary.Operand is LiteralExpressionSyntax inner)
            {
                return unary.OperatorToken.ValueText + inner.Token.ValueText;
            }

            return null;
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
