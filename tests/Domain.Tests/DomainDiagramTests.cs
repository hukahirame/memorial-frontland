using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// Domain 層のクラス図を型から生成し、docs/domain-class-diagram.md と一致するか見る。
    /// 一致しなければファイルを書き換えたうえで失敗する。git diff に構造の変化が出る。
    /// dotnet 専用。Unity 側からは見えない（csproj で明示的に足しているだけ）。
    /// </summary>
    public class DomainDiagramTests
    {
        private const string DiagramPath = "docs/domain-class-diagram.md";

        [Test]
        public void クラス図がDomainの型と一致する()
        {
            string expected = Generate();
            string full = Path.Combine(RepositoryRoot(), DiagramPath);
            string actual = File.Exists(full) ? File.ReadAllText(full) : null;

            //改行コードは比較しない。git が作業コピーを CRLF に戻すため、
            //生成側の LF とそのままでは永久に一致しない
            if (Normalize(actual) == Normalize(expected)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, expected.Replace("\n", Environment.NewLine));
            Assert.Fail(
                DiagramPath + " が Domain の型と一致していなかったため書き換えた。\n" +
                "git diff で構造の変化を確認し、意図どおりならコミットすること。");
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace("\r\n", "\n");
        }

        private static string Generate()
        {
            var types = typeof(Root).Assembly.GetTypes()
                .Where(t => t.IsPublic && t.Namespace == "MemorialFloor.Domain")
                .OrderBy(t => t.Name, StringComparer.Ordinal)
                .ToList();

            var names = new HashSet<string>(types.Select(t => t.Name));
            var sb = new StringBuilder();

            sb.Append("<!-- このファイルは DomainDiagramTests が生成する。手で編集しない -->\n\n");
            sb.Append("# Domain 層のクラス図\n\n");
            sb.Append("```mermaid\nclassDiagram\n");

            foreach (var t in types) AppendType(sb, t);

            foreach (var line in Relations(types, names)) sb.Append("    " + line + "\n");

            sb.Append("```\n");
            return sb.ToString();
        }

        private static void AppendType(StringBuilder sb, Type t)
        {
            sb.Append("    class " + t.Name + " {\n");

            if (t.IsEnum)
            {
                sb.Append("        <<enumeration>>\n");
                foreach (var name in Enum.GetNames(t)) sb.Append("        " + name + "\n");
                sb.Append("    }\n");
                return;
            }

            if (t.IsValueType) sb.Append("        <<struct>>\n");

            foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
                              .OrderBy(f => f.Name, StringComparer.Ordinal))
            {
                sb.Append("        +" + Nice(f.FieldType) + " " + f.Name + "$\n");
            }

            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                              .OrderBy(p => p.Name, StringComparer.Ordinal))
            {
                //読み取り専用が既定。外から書けるものだけ印を付ける
                string setter = p.SetMethod != null && p.SetMethod.IsPublic ? " [set]" : "";
                sb.Append("        +" + Nice(p.PropertyType) + " " + p.Name + setter + "\n");
            }

            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                              .Where(m => !m.IsSpecialName)
                              .OrderBy(m => m.Name, StringComparer.Ordinal)
                              .ThenBy(m => m.GetParameters().Length))
            {
                string args = string.Join(", ", m.GetParameters().Select(x => Nice(x.ParameterType)));
                sb.Append("        +" + m.Name + "(" + args + ") " + Nice(m.ReturnType) + "\n");
            }

            sb.Append("    }\n");
        }

        /// <summary>保持しているもの（フィールド・プロパティ）は実線、
        /// signature に出るだけのものは破線。何を持っているかを見分けるため</summary>
        private static IEnumerable<string> Relations(List<Type> types, HashSet<string> names)
        {
            var lines = new SortedSet<string>(StringComparer.Ordinal);

            foreach (var t in types)
            {
                if (t.IsEnum) continue;

                var held = new HashSet<string>(Held(t));
                foreach (var other in held.Concat(Used(t)).Distinct().OrderBy(x => x, StringComparer.Ordinal))
                {
                    if (other == t.Name || !names.Contains(other)) continue;
                    lines.Add(t.Name + (held.Contains(other) ? " --> " : " ..> ") + other);
                }
            }

            return lines;
        }

        private static IEnumerable<string> Held(Type t)
        {
            foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var n in Unwrap(p.PropertyType)) yield return n;

            foreach (var f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly))
                foreach (var n in Unwrap(f.FieldType)) yield return n;
        }

        private static IEnumerable<string> Used(Type t)
        {
            foreach (var m in t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly).Where(m => !m.IsSpecialName))
            {
                foreach (var n in Unwrap(m.ReturnType)) yield return n;
                foreach (var p in m.GetParameters())
                    foreach (var n in Unwrap(p.ParameterType)) yield return n;
            }
        }

        private static IEnumerable<string> Unwrap(Type t)
        {
            if (t.IsGenericType)
            {
                foreach (var arg in t.GetGenericArguments())
                    foreach (var n in Unwrap(arg)) yield return n;
                yield break;
            }

            yield return t.Name;
        }

        private static string Nice(Type t)
        {
            if (t == typeof(void)) return "void";
            if (t == typeof(string)) return "string";
            if (t == typeof(int)) return "int";
            if (t == typeof(bool)) return "bool";
            if (t == typeof(float)) return "float";

            if (!t.IsGenericType) return t.Name;

            string bare = t.Name.Substring(0, t.Name.IndexOf('`'));
            return bare + "~" + string.Join(", ", t.GetGenericArguments().Select(Nice)) + "~";
        }

        /// <summary>docs/ を持つ階層まで遡る。dotnet と CI で作業ディレクトリが違うため</summary>
        private static string RepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;

            Assert.IsNotNull(dir, "docs/ を持つ階層が見つからない");
            return dir.FullName;
        }
    }
}
