using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// Domain の公開している面を一覧にして docs/public-api.txt を作る。
    /// 一致しなければ書き換えたうえで失敗する。git diff に面の変化が出る。
    ///
    /// 使う側の書き方が通らなくなる変化は、既存の行が消えるか書き換わる形で出るようにする。
    /// static や out や get / set まで1行に含めるのはそのため。足しただけなら行が増えるだけで済む。
    /// flow verify の API-CHANGED はこの性質に頼っている（tools/flow/flow-rules/refactor.md）。
    /// </summary>
    public class PublicApiTests
    {
        public const string OutputPath = "docs/public-api.txt";

        /// <summary>Domain だけを見る。Game と Legacy は動いている最中なので固定しない</summary>
        private const string Layer = "Domain";

        /// <summary>型の名前。名前空間は1つしかないので付けない。入れ子は外側の型から書く</summary>
        private static readonly SymbolDisplayFormat TypeName = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                           | SymbolDisplayGenericsOptions.IncludeVariance,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        /// <summary>メンバの名前と引数。型は後ろに「 : 型」で足す</summary>
        private static readonly SymbolDisplayFormat MemberName = new SymbolDisplayFormat(
            globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
            typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
            genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters
                           | SymbolDisplayGenericsOptions.IncludeTypeConstraints,
            memberOptions: SymbolDisplayMemberOptions.IncludeContainingType
                         | SymbolDisplayMemberOptions.IncludeParameters,
            parameterOptions: SymbolDisplayParameterOptions.IncludeType
                            | SymbolDisplayParameterOptions.IncludeParamsRefOut
                            | SymbolDisplayParameterOptions.IncludeDefaultValue
                            | SymbolDisplayParameterOptions.IncludeExtensionThis,
            miscellaneousOptions: SymbolDisplayMiscellaneousOptions.UseSpecialTypes);

        [Test]
        public void 公開APIの一覧がソースと一致する()
        {
            string expected = Generate();
            string full = Path.Combine(SourceIndex.RepositoryRoot(),
                                       OutputPath.Replace('/', Path.DirectorySeparatorChar));
            string actual = File.Exists(full) ? File.ReadAllText(full) : null;

            // 改行コードは比較しない。git が作業コピーを CRLF に戻すため
            if (Normalize(actual) == Normalize(expected)) return;

            File.WriteAllText(full, expected, new UTF8Encoding(true));
            Assert.Fail(OutputPath + " が古かったため書き直した。" +
                        "公開している面が変わっている。差分を見ること。");
        }

        private static string Generate()
        {
            StringBuilder text = new StringBuilder();
            text.Append("# 公開API。PublicApiTests が作る。手で編集しない。\n");
            text.Append("#\n");
            text.Append("# Domain が使う側へ見せている型とメンバ。1行が1つの面。\n");
            text.Append("# 使う側が通らなくなる変化は、既存の行が消えるか書き換わる形で出る。\n");
            text.Append("# 足しただけなら行が増えるだけ。ただし既存の型に abstract の行が増えると、実装する側が壊れる。\n");
            text.Append("#\n");
            text.Append("# 型ごとに、型の行、基底の行、メンバの行の順に並ぶ。private と internal は入らない。\n");

            IEnumerable<INamedTypeSymbol> types = Types(Compile().Assembly.GlobalNamespace)
                .OrderBy(type => type.ToDisplayString(TypeName), StringComparer.Ordinal);

            foreach (INamedTypeSymbol type in types)
            {
                text.Append('\n');
                text.Append(Header(type)).Append('\n');

                foreach (string line in Bases(type)) text.Append(line).Append('\n');
                foreach (string line in Members(type)) text.Append(line).Append('\n');
            }

            return text.ToString();
        }

        /// <summary>
        /// Domain のソースだけをコンパイルする。構文だけでは out や static や
        /// 暗黙のコンストラクタが取れないため。Domain は UnityEngine に依存しないので実行環境の参照で足りる
        /// </summary>
        private static CSharpCompilation Compile()
        {
            IEnumerable<SyntaxTree> trees = Directory
                .GetFiles(SourceIndex.LayerDirectory(Layer), "*.cs", SearchOption.AllDirectories)
                .OrderBy(file => file, StringComparer.Ordinal)
                .Select(file => CSharpSyntaxTree.ParseText(File.ReadAllText(file), path: file));

            IEnumerable<MetadataReference> references =
                ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path));

            CSharpCompilation compilation = CSharpCompilation.Create(
                "Domain", trees, references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            List<Diagnostic> errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToList();

            Assert.That(errors, Is.Empty, "Domain がコンパイルできないと面を書き出せない");
            return compilation;
        }

        private static IEnumerable<INamedTypeSymbol> Types(INamespaceSymbol ns)
        {
            foreach (INamespaceSymbol child in ns.GetNamespaceMembers())
                foreach (INamedTypeSymbol type in Types(child))
                    yield return type;

            foreach (INamedTypeSymbol type in ns.GetTypeMembers())
                foreach (INamedTypeSymbol visible in WithNested(type))
                    yield return visible;
        }

        private static IEnumerable<INamedTypeSymbol> WithNested(INamedTypeSymbol type)
        {
            if (!Visible(type)) yield break;

            yield return type;

            foreach (INamedTypeSymbol nested in type.GetTypeMembers())
                foreach (INamedTypeSymbol visible in WithNested(nested))
                    yield return visible;
        }

        /// <summary>外から触れるか。protected は継承できる型の中でだけ面になる</summary>
        private static bool Visible(ISymbol symbol)
        {
            switch (symbol.DeclaredAccessibility)
            {
                case Accessibility.Public:
                    break;
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    INamedTypeSymbol owner = symbol.ContainingType;
                    if (owner == null || owner.IsSealed || owner.IsStatic) return false;
                    break;
                default:
                    return false;
            }

            return symbol.ContainingType == null || Visible(symbol.ContainingType);
        }

        /// <summary>例: static class DayClock / readonly struct AddResult / interface IRandom</summary>
        private static string Header(INamedTypeSymbol type)
        {
            List<string> words = new List<string>();

            if (type.DeclaredAccessibility != Accessibility.Public) words.Add("protected");

            if (type.TypeKind == TypeKind.Class)
            {
                if (type.IsStatic) words.Add("static");
                else if (type.IsAbstract) words.Add("abstract");
                else if (type.IsSealed) words.Add("sealed");
            }

            if (type.TypeKind == TypeKind.Struct && type.IsReadOnly) words.Add("readonly");
            if (type.IsRefLikeType) words.Add("ref");
            if (type.IsRecord) words.Add("record");

            switch (type.TypeKind)
            {
                case TypeKind.Class: words.Add("class"); break;
                case TypeKind.Struct: words.Add("struct"); break;
                case TypeKind.Interface: words.Add("interface"); break;
                case TypeKind.Enum: words.Add("enum"); break;
                case TypeKind.Delegate: words.Add("delegate"); break;
            }

            string name = type.ToDisplayString(TypeName);

            if (type.TypeKind == TypeKind.Delegate)
                name = type.DelegateInvokeMethod.ToDisplayString(MemberName).Replace(".Invoke", "")
                     + " : " + Display(type.DelegateInvokeMethod.ReturnType);

            words.Add(name);
            return string.Join(" ", words);
        }

        /// <summary>例: Health : IDamageable。足すのは自由で、外すと使う側が壊れる</summary>
        private static IEnumerable<string> Bases(INamedTypeSymbol type)
        {
            string name = type.ToDisplayString(TypeName);

            if (type.TypeKind == TypeKind.Enum && type.EnumUnderlyingType.SpecialType != SpecialType.System_Int32)
                yield return name + " : " + Display(type.EnumUnderlyingType);

            if (type.TypeKind == TypeKind.Class && type.BaseType != null
                && type.BaseType.SpecialType != SpecialType.System_Object)
                yield return name + " : " + Display(type.BaseType);

            foreach (string face in type.Interfaces.Select(Display).OrderBy(n => n, StringComparer.Ordinal))
                yield return name + " : " + face;
        }

        private static IEnumerable<string> Members(INamedTypeSymbol type)
        {
            if (type.TypeKind == TypeKind.Delegate) yield break;

            List<(string Key, string Line)> lines = new List<(string, string)>();

            foreach (ISymbol member in type.GetMembers())
            {
                if (!Visible(member)) continue;

                string line = Line(type, member);
                if (line == null) continue;

                string key = member is IMethodSymbol accessor && accessor.AssociatedSymbol != null
                           ? accessor.AssociatedSymbol.Name
                           : member.Name;

                lines.Add((key, line));
            }

            foreach ((string _, string line) in lines.OrderBy(l => l.Key, StringComparer.Ordinal)
                                                     .ThenBy(l => l.Line, StringComparer.Ordinal))
                yield return line;
        }

        /// <summary>
        /// 1つの面を1行に。null なら書かない。
        /// プロパティは get と set を別の行にする。set を足したことが既存の行の書き換えにならないように
        /// </summary>
        private static string Line(INamedTypeSymbol type, ISymbol member)
        {
            switch (member)
            {
                case IFieldSymbol field when type.TypeKind == TypeKind.Enum:
                    // 値まで書く。途中に足すと後ろの値がずれ、保存した数が別の意味になる
                    return field.ToDisplayString(MemberName) + " = " + field.ConstantValue;

                case IFieldSymbol field:
                    if (field.IsImplicitlyDeclared) return null;
                    return Modifiers(field) + field.ToDisplayString(MemberName) + " : " + Display(field.Type);

                case IEventSymbol ev:
                    return Modifiers(ev) + "event " + ev.ToDisplayString(MemberName) + " : " + Display(ev.Type);

                case IMethodSymbol method:
                    switch (method.MethodKind)
                    {
                        case MethodKind.Constructor:
                            // 暗黙のコンストラクタも面になる。明示のものを足すと消えるため。
                            // enum の暗黙のものは誰も呼ばないので書かない
                            if (type.TypeKind == TypeKind.Enum) return null;
                            return type.ToDisplayString(TypeName) + ".ctor"
                                 + Parameters(method);

                        case MethodKind.PropertyGet:
                            return Modifiers(method) + method.ToDisplayString(MemberName)
                                 + " : " + Display(method.ReturnType);

                        case MethodKind.PropertySet:
                            return Modifiers(method) + method.ToDisplayString(MemberName)
                                 + (method.IsInitOnly ? " init" : "")
                                 + " : " + Display(method.Parameters.Last().Type);

                        case MethodKind.Ordinary:
                        case MethodKind.UserDefinedOperator:
                        case MethodKind.Conversion:
                            if (method.IsImplicitlyDeclared) return null;
                            return Modifiers(method) + method.ToDisplayString(MemberName)
                                 + " : " + (method.ReturnsByRef ? "ref " : "")
                                 + (method.ReturnsByRefReadonly ? "ref readonly " : "")
                                 + Display(method.ReturnType);

                        default:
                            return null;
                    }

                default:
                    return null;
            }
        }

        private static string Parameters(IMethodSymbol method)
        {
            string shown = method.ToDisplayString(MemberName);
            return shown.Substring(shown.IndexOf('('));
        }

        /// <summary>行の頭に付ける修飾子。付け外しは既存の行の書き換えとして差分に出る</summary>
        private static string Modifiers(ISymbol symbol)
        {
            List<string> words = new List<string>();

            if (symbol.DeclaredAccessibility != Accessibility.Public) words.Add("protected");

            if (symbol is IFieldSymbol constant && constant.IsConst) words.Add("const");
            else if (symbol.IsStatic) words.Add("static");

            if (symbol is IFieldSymbol field && field.IsReadOnly) words.Add("readonly");

            // インタフェースのメンバも abstract になる。既存の型に足すと実装する側が壊れる印
            if (symbol.IsAbstract) words.Add("abstract");
            else if (symbol.IsOverride) words.Add(symbol.IsSealed ? "sealed override" : "override");
            else if (symbol.IsVirtual) words.Add("virtual");

            return words.Count == 0 ? "" : string.Join(" ", words) + " ";
        }

        private static string Display(ITypeSymbol type)
        {
            return type.ToDisplayString(TypeName);
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace(((char)13).ToString(), "");
        }
    }
}
