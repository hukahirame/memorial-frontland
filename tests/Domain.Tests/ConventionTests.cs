using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// docs/conventions.md の取り決めのうち、ソースを読めば判定できるもの。
    /// Unity のコンパイルは .editorconfig の命名も解析器も見ないので、Game も含めてここで落とす。構文木だけを読み、Unity は要らない
    /// </summary>
    public class ConventionTests
    {
        /// <summary>取り決めが効く範囲。.editorconfig と同じ</summary>
        private const string Project = "Assets/_Project";

        /// <summary>遊んでいる最中に動くコード。エディタの道具とテストは含めない</summary>
        private const string Runtime = "Assets/_Project/Scripts";

        private static readonly string[] NotRuntime = { "Assets/_Project/Scripts/Editor" };

        /// <summary>場面全体から物を探す呼び出し。Transform.Find（子を名前で引く）は含めない</summary>
        private static readonly HashSet<string> GlobalSearches = new HashSet<string>(StringComparer.Ordinal)
        {
            "FindWithTag", "FindGameObjectWithTag", "FindGameObjectsWithTag",
            "FindObjectOfType", "FindObjectsOfType", "FindAnyObjectByType", "FindFirstObjectByType", "FindObjectsByType",
        };

        private static readonly Regex Pascal = new Regex("^[A-Z][A-Za-z0-9]*$");
        private static readonly Regex UnderscoreCamel = new Regex("^_[a-z][A-Za-z0-9]*$");

        [Test]
        public void 定数はPascalCaseで公開しないフィールドは下線で始まるcamelCase()
        {
            List<string> found = new List<string>();

            foreach (Source source in Sources(Project))
            {
                foreach (FieldDeclarationSyntax field in source.Root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    bool isConstant = Has(field, SyntaxKind.ConstKeyword) || (Has(field, SyntaxKind.StaticKeyword) && Has(field, SyntaxKind.ReadOnlyKeyword));

                    //公開フィールドは保存や素材の鍵になる入れ物だけにあり（下のテスト）、名前を変えると前のデータが読めない
                    if (!isConstant && Has(field, SyntaxKind.PublicKeyword)) continue;

                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables)
                    {
                        string name = variable.Identifier.Text;
                        if (isConstant ? Pascal.IsMatch(name) : UnderscoreCamel.IsMatch(name)) continue;

                        found.Add(source.At(variable) + " " + name + (isConstant ? "（定数は PascalCase）" : "（_camelCase）"));
                    }
                }
            }

            Assert.IsEmpty(found, "フィールドの名前が取り決めと違う。シーンや prefab に保存される欄は FormerlySerializedAs を付けて変える" + Lines(found));
        }

        //外から見えない入れ子の型（計測の 1 行など）は見ない。.NET の CA1051 と同じ範囲
        [Test]
        public void 外から見える型の書き換えられる公開フィールドはSerializableの入れ物とScriptableObjectにだけある()
        {
            List<string> found = new List<string>();

            foreach (Source source in Sources(Project))
            {
                foreach (FieldDeclarationSyntax field in source.Root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    if (!Has(field, SyntaxKind.PublicKeyword) || Has(field, SyntaxKind.ConstKeyword)
                        || Has(field, SyntaxKind.StaticKeyword) || Has(field, SyntaxKind.ReadOnlyKeyword)) continue;

                    if (field.Parent is not TypeDeclarationSyntax owner || !IsVisible(owner) || IsDataHolder(owner)) continue;

                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables) found.Add(source.At(variable) + " " + variable.Identifier.Text);
                }
            }

            Assert.IsEmpty(found, "書き換えられる public フィールドがある。[SerializeField] private にして、読むだけなら get だけのプロパティで出す（[D-7]）" + Lines(found));
        }

        //付随する enum や結果の型は同じファイルに置いてよいが、主な型の名前がファイル名になる。テストは対象外（1 ファイルに場面ごとの組を並べる）
        [Test]
        public void スクリプトのファイルにはファイル名と同じ名前の型がある()
        {
            List<string> found = new List<string>();

            foreach (Source source in Sources(Runtime))
            {
                string name = System.IO.Path.GetFileNameWithoutExtension(source.Path);
                bool hasType = source.Root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().Any(type => type.Identifier.Text == name)
                               || source.Root.DescendantNodes().OfType<DelegateDeclarationSyntax>().Any(handler => handler.Identifier.Text == name);

                if (!hasType) found.Add(source.Path);
            }

            Assert.IsEmpty(found, "ファイル名と同じ名前の型が無い。主な型ごとにファイルを分け、その型の名前をファイル名にする" + Lines(found));
        }

        [Test]
        public void 遊んでいる最中のコードはstaticに状態を置かない()
        {
            List<string> found = new List<string>();

            foreach (Source source in Sources(Runtime, NotRuntime))
            {
                foreach (FieldDeclarationSyntax field in source.Root.DescendantNodes().OfType<FieldDeclarationSyntax>())
                {
                    if (!Has(field, SyntaxKind.StaticKeyword) || Has(field, SyntaxKind.ReadOnlyKeyword) || Has(field, SyntaxKind.ConstKeyword)) continue;

                    foreach (VariableDeclaratorSyntax variable in field.Declaration.Variables) found.Add(source.At(variable) + " " + variable.Identifier.Text);
                }

                foreach (PropertyDeclarationSyntax property in source.Root.DescendantNodes().OfType<PropertyDeclarationSyntax>())
                {
                    bool isWritable = property.AccessorList != null && property.AccessorList.Accessors.Any(accessor =>
                        accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration));
                    if (property.Modifiers.Any(SyntaxKind.StaticKeyword) && isWritable) found.Add(source.At(property) + " " + property.Identifier.Text);
                }

                foreach (EventFieldDeclarationSyntax handler in source.Root.DescendantNodes().OfType<EventFieldDeclarationSyntax>())
                {
                    if (!handler.Modifiers.Any(SyntaxKind.StaticKeyword)) continue;

                    foreach (VariableDeclaratorSyntax variable in handler.Declaration.Variables) found.Add(source.At(variable) + " " + variable.Identifier.Text);
                }
            }

            Assert.IsEmpty(found, "static に状態を置いている。常駐シーンの部品の値にする（[D-6]）" + Lines(found));
        }

        [Test]
        public void 遊んでいる最中のコードは場面全体から名前やタグや型で物を探さない()
        {
            List<string> found = new List<string>();

            foreach (Source source in Sources(Runtime, NotRuntime))
            {
                foreach (InvocationExpressionSyntax call in source.Root.DescendantNodes().OfType<InvocationExpressionSyntax>())
                {
                    string receiver = call.Expression is MemberAccessExpressionSyntax access ? access.Expression.ToString() : "";
                    string name = MethodName(call.Expression);
                    bool isGlobal = GlobalSearches.Contains(name) || (name == "Find" && (receiver == "GameObject" || receiver == "UnityEngine.GameObject"));

                    if (isGlobal) found.Add(source.At(call) + " " + call.Expression);
                }
            }

            Assert.IsEmpty(found, "場面全体から物を探している。Inspector で参照を張るか、作るときに渡すか、場所（Area）から引く" + Lines(found));
        }

        private static string MethodName(ExpressionSyntax expression)
        {
            switch (expression)
            {
                case MemberAccessExpressionSyntax access: return access.Name.Identifier.Text;
                case SimpleNameSyntax simple: return simple.Identifier.Text;
                default: return "";
            }
        }

        private static bool Has(FieldDeclarationSyntax field, SyntaxKind modifier)
        {
            return field.Modifiers.Any(modifier);
        }

        /// <summary>型とそれを囲む型がすべて public か protected で、束の外から見える</summary>
        private static bool IsVisible(TypeDeclarationSyntax type)
        {
            return type.AncestorsAndSelf().OfType<BaseTypeDeclarationSyntax>().All(declared =>
                declared.Modifiers.Any(SyntaxKind.PublicKeyword) || declared.Modifiers.Any(SyntaxKind.ProtectedKeyword));
        }

        /// <summary>[Serializable] の入れ物か ScriptableObject。中身を Unity や保存が書くので、公開フィールドでよい</summary>
        private static bool IsDataHolder(TypeDeclarationSyntax type)
        {
            bool isSerializable = type.AttributeLists.SelectMany(list => list.Attributes)
                .Any(attribute => attribute.Name.ToString() is "Serializable" or "System.Serializable");
            bool isAsset = type.BaseList != null && type.BaseList.Types.Any(baseType => baseType.Type.ToString() == "ScriptableObject");

            return isSerializable || isAsset;
        }

        private static string Lines(List<string> found)
        {
            return Environment.NewLine + string.Join(Environment.NewLine, found);
        }

        private sealed class Source
        {
            public string Path { get; }
            public SyntaxNode Root { get; }

            public Source(string path, SyntaxNode root)
            {
                Path = path;
                Root = root;
            }

            /// <summary>道と行。「Assets/_Project/Scripts/Game/X.cs:12」</summary>
            public string At(SyntaxNode node)
            {
                return Path + ":" + (node.GetLocation().GetLineSpan().StartLinePosition.Line + 1);
            }
        }

        private static IEnumerable<Source> Sources(string relative, params string[] skipped)
        {
            string repository = SourceIndex.RepositoryRoot();
            string dir = System.IO.Path.Combine(repository, relative.Replace('/', System.IO.Path.DirectorySeparatorChar));

            //エディタでだけ動く行も読む
            CSharpParseOptions options = CSharpParseOptions.Default.WithPreprocessorSymbols("UNITY_EDITOR");

            foreach (string file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories).OrderBy(f => f, StringComparer.Ordinal))
            {
                string shown = System.IO.Path.GetRelativePath(repository, file).Replace('\\', '/');
                if (skipped.Any(skip => shown.StartsWith(skip + "/", StringComparison.Ordinal))) continue;

                yield return new Source(shown, CSharpSyntaxTree.ParseText(File.ReadAllText(file), options).GetRoot());
            }
        }
    }
}
