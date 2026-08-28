using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// _Project と LegacyScripts のソースから、型・メンバ・依存を書き出す生成器。
    /// 手順の解説は .claude/skills/class-diff-diagram/broken.md（停止中の skill）にある。
    /// 食い違ったらこちらを直す。
    ///
    /// Legacy は UnityEngine 依存でリフレクションが使えないため、ソースを字面で解析する。
    /// コメントと文字列リテラルは文字単位の走査で除き、波括弧の深さでフィールドと
    /// 局所変数を分ける。厳密ではない。限界は生成物の冒頭に書いてある。
    ///
    /// テスト枠組みには依存しない。呼び出しと検査は DependencyGraphTests が行う。
    /// </summary>
    public static class DependencyGraphGenerator
    {
        public const string OutputPath = "docs/dependencies-diagrams/graph.txt";

        private static readonly (string Layer, string Path)[] Roots =
        {
            ("Domain", "Assets/_Project/Scripts/Domain"),
            ("Game",   "Assets/_Project/Scripts/Game"),
            ("Legacy", "Assets/LegacyScripts"),
        };

        private const string Modifiers =
            "public|private|protected|internal|static|readonly|const|override|virtual|" +
            "abstract|sealed|async|new|partial|extern|unsafe|volatile|event|required";

        private sealed class TypeInfo
        {
            public string Name;
            public string Layer;
            public string Kind;
            public readonly List<string> Members = new List<string>();   // "名前|表示"
            public readonly HashSet<string> Held = new HashSet<string>(StringComparer.Ordinal);
            public readonly HashSet<string> Used = new HashSet<string>(StringComparer.Ordinal);
        }

        /// <summary>素データの本文を作る。ファイルには書かない</summary>
        public static string Generate()
        {
            var types = new Dictionary<string, TypeInfo>(StringComparer.Ordinal);
            var files = new SortedSet<string>(StringComparer.Ordinal);
            var bodies = new List<string>();

            foreach (var (layer, rel) in Roots)
            {
                string dir = Path.Combine(RepositoryRoot(), rel.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(dir)) continue;

                foreach (var file in Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                                              .OrderBy(f => f, StringComparer.Ordinal))
                {
                    string body = Strip(File.ReadAllText(file));
                    bodies.Add(body);

                    var declared = ParseFile(body, layer, types);
                    if (declared.Count == 0) continue;

                    // ファイル名も残す。関心事スライスは「1ファイル = 1枚」で切るため、
                    // 型がどのファイルに宣言されたかが分からないと図を作れない
                    string path = rel + "/" + file.Substring(dir.Length + 1)
                                                  .Replace(Path.DirectorySeparatorChar, '/');
                    files.Add(path + " " + string.Join(" ", declared.OrderBy(t => t, StringComparer.Ordinal)));
                }
            }

            // メソッド本体の中の参照はファイル単位でしか取れない。弱い依存として足す
            foreach (var body in bodies)
            {
                var declared = DeclaredIn(body);

                foreach (var target in types.Keys)
                {
                    if (declared.Contains(target)) continue;
                    if (!Regex.IsMatch(body, @"\b" + Regex.Escape(target) + @"\b")) continue;

                    foreach (var owner in declared)
                        if (types.ContainsKey(owner) && !types[owner].Held.Contains(target))
                            types[owner].Used.Add(target);
                }
            }

            return Render(types, files);
        }

        /// <summary>docs/ を持つ階層まで遡る。dotnet と CI で作業ディレクトリが違うため</summary>
        public static string RepositoryRoot()
        {
            var dir = new DirectoryInfo(AppContext.BaseDirectory);
            while (dir != null && !Directory.Exists(Path.Combine(dir.FullName, "docs"))) dir = dir.Parent;

            if (dir == null) throw new DirectoryNotFoundException("docs/ を持つ階層が見つからない");

            return dir.FullName;
        }

        private static string Render(Dictionary<string, TypeInfo> types, SortedSet<string> files)
        {
            var sb = new StringBuilder();
            sb.Append("# 機械用の素データ。tools/diagram-diff.ps1 が読む。\n");
            sb.Append("# 人が読む図は docs/dependencies-diagrams/ と docs/dependencies-diff-diagrams/。\n");
            sb.Append("# DependencyGraphGenerator が生成する。手で編集しない。\n");
            sb.Append("#\n");
            sb.Append("# ソースの字面から拾っているので厳密ではない。\n");
            sb.Append("# - var で受けている依存は型名が現れないので出ない\n");
            sb.Append("# - 同一ファイルに宣言された型どうしには辺が出ない。[files] が補う\n");
            sb.Append("# - メソッド本体の中の参照はファイル単位。弱い依存 dep として記録する\n");
            sb.Append("# - 複数行にまたがる宣言は拾えない\n");

            sb.Append("\n[types]\n");
            foreach (var t in types.Values.OrderBy(t => t.Name, StringComparer.Ordinal))
                sb.Append(t.Name + " " + t.Layer + " " + t.Kind + "\n");

            sb.Append("\n[files]\n");
            foreach (var line in files) sb.Append(line + "\n");

            var edges = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var t in types.Values)
            {
                foreach (var to in t.Held)
                    if (types.ContainsKey(to) && to != t.Name) edges.Add(t.Name + " -> " + to + " assoc");

                foreach (var to in t.Used)
                    if (types.ContainsKey(to) && to != t.Name && !t.Held.Contains(to))
                        edges.Add(t.Name + " -> " + to + " dep");
            }

            sb.Append("\n[edges]\n");
            foreach (var e in edges) sb.Append(e + "\n");

            sb.Append("\n[members]\n");
            foreach (var t in types.Values.OrderBy(t => t.Name, StringComparer.Ordinal))
                foreach (var m in t.Members.Distinct().OrderBy(m => m, StringComparer.Ordinal))
                    sb.Append(t.Name + "|" + m + "\n");

            return sb.ToString();
        }

        private static HashSet<string> DeclaredIn(string body)
        {
            return new HashSet<string>(
                Regex.Matches(body, @"\b(?:class|struct|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)")
                     .Select(m => m.Groups[1].Value),
                StringComparer.Ordinal);
        }

        /// <summary>1ファイルを行単位で読み、波括弧の深さで型の中かどうかを判断する</summary>
        private static List<string> ParseFile(string body, string layer, Dictionary<string, TypeInfo> types)
        {
            var declared = new List<string>();
            var openTypes = new Dictionary<int, TypeInfo>();   // 深さ -> その深さで開いている型
            string pendingName = null, pendingKind = null;
            int depth = 0;

            foreach (var raw in body.Split('\n'))
            {
                string line = raw.Trim();

                var decl = Regex.Match(line, @"\b(class|struct|enum|interface)\s+([A-Za-z_][A-Za-z0-9_]*)");
                if (decl.Success)
                {
                    pendingKind = decl.Groups[1].Value;
                    pendingName = decl.Groups[2].Value;
                }
                else if (openTypes.ContainsKey(depth))
                {
                    var owner = openTypes[depth];
                    if (owner.Kind == "enum") AddEnumMember(owner, line);
                    else AddMember(owner, line);
                }

                foreach (char c in raw)
                {
                    if (c == '{')
                    {
                        depth++;
                        if (pendingName != null)
                        {
                            if (!types.ContainsKey(pendingName))
                                types[pendingName] = new TypeInfo { Name = pendingName, Layer = layer, Kind = pendingKind };

                            openTypes[depth] = types[pendingName];
                            declared.Add(pendingName);
                            pendingName = null;
                        }
                    }
                    else if (c == '}')
                    {
                        openTypes.Remove(depth);
                        depth--;
                    }
                }
            }

            return declared;
        }

        private static void AddEnumMember(TypeInfo owner, string line)
        {
            var m = Regex.Match(line, @"^([A-Za-z_][A-Za-z0-9_]*)\s*(?:=[^,]*)?,?$");
            if (m.Success) owner.Members.Add(m.Groups[1].Value + "|" + m.Groups[1].Value);
        }

        private static void AddMember(TypeInfo owner, string line)
        {
            // [SerializeField] は「コードには private、Inspector には公開」。
            // 構造として見れば外から供給される点で public と同じなので + にする。
            // _Project では public 可変フィールドを禁じている（D-007）ので、
            // 移行が進めば + のフィールドは SerializeField か SO だけになる
            bool serialized = Regex.IsMatch(line, @"^\s*\[[^\]]*SerializeField");

            line = Regex.Replace(line, @"^(?:\s*\[[^\]]*\]\s*)+", "");   // 属性を落とす
            if (line.Length == 0) return;

            // コンストラクタは戻り値が無いので先に分ける。一般形だと修飾子を型と読み違える
            var ctor = Regex.Match(line,
                @"^(?<mods>(?:(?:" + Modifiers + @")\s+)*)" +
                @"(?<name>" + Regex.Escape(owner.Name) + @")\s*\(");
            if (ctor.Success)
            {
                int cOpen = line.IndexOf('(');
                int cClose = line.IndexOf(')', cOpen);
                if (cClose < 0) return;

                string cArgs = line.Substring(cOpen + 1, cClose - cOpen - 1);
                owner.Members.Add(owner.Name + "|" + Visibility(ctor.Groups["mods"].Value) + " " +
                                  owner.Name + "(" + ParamTypes(cArgs, owner) + ")");
                return;
            }

            var m = Regex.Match(line,
                @"^(?<mods>(?:(?:" + Modifiers + @")\s+)*)" +
                @"(?<type>[A-Za-z_][A-Za-z0-9_.]*(?:<[^>]*>)?(?:\[\])*\??)\s+" +
                @"(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*(?<tail>\(|=>|=|;|\{)");

            // 括弧を次の行に置く書き方のプロパティ。宣言行に判定用の記号が無い。
            //   public IReadOnlyList<Root> All
            //   {
            // 型の直下の深さでしか見ていないので、文（return x など）とは衝突しない
            if (!m.Success)
                m = Regex.Match(line,
                    @"^(?<mods>(?:(?:" + Modifiers + @")\s+)*)" +
                    @"(?<type>[A-Za-z_][A-Za-z0-9_.]*(?:<[^>]*>)?(?:\[\])*\??)\s+" +
                    @"(?<name>[A-Za-z_][A-Za-z0-9_]*)(?<tail>)$");

            if (!m.Success) return;

            string vis = serialized ? "+" : Visibility(m.Groups["mods"].Value);
            string type = m.Groups["type"].Value;
            string name = m.Groups["name"].Value;

            if (m.Groups["tail"].Value == "(")
            {
                int open = line.IndexOf('(');
                int close = line.IndexOf(')', open);
                if (close < 0) return;

                string args = line.Substring(open + 1, close - open - 1);
                string shown = vis + " " + name + "(" + ParamTypes(args, owner) + ")";

                // void は書いても情報が無い。過半数のメソッドが void なので省く
                if (type != "void") shown += " " + type;

                owner.Members.Add(name + "|" + shown);

                owner.Used.Add(BareType(type));
                foreach (var g in GenericArgs(type)) owner.Used.Add(g);
            }
            else
            {
                owner.Members.Add(name + "|" + vis + " " + type + " " + name);

                owner.Held.Add(BareType(type));
                foreach (var g in GenericArgs(type)) owner.Held.Add(g);
            }
        }

        /// <summary>引数の型だけを並べる。ついでに弱い依存として記録する</summary>
        private static string ParamTypes(string args, TypeInfo owner)
        {
            var list = new List<string>();

            foreach (var a in SplitTop(args))
            {
                var t = Regex.Match(a.Trim(), @"^(?:(?:ref|out|in|params|this)\s+)*(?<type>\S+)");
                if (!t.Success) continue;

                string type = t.Groups["type"].Value;
                list.Add(type);

                owner.Used.Add(BareType(type));
                foreach (var g in GenericArgs(type)) owner.Used.Add(g);
            }

            return string.Join(", ", list);
        }

        private static IEnumerable<string> SplitTop(string args)
        {
            int depth = 0, start = 0;

            for (int i = 0; i < args.Length; i++)
            {
                char c = args[i];
                if (c == '<' || c == '(' || c == '[') depth++;
                else if (c == '>' || c == ')' || c == ']') depth--;
                else if (c == ',' && depth == 0) { yield return args.Substring(start, i - start); start = i + 1; }
            }

            if (start < args.Length) yield return args.Substring(start);
        }

        private static string BareType(string type)
        {
            return Regex.Replace(type, @"[<\[].*$", "").TrimEnd('?');
        }

        private static IEnumerable<string> GenericArgs(string type)
        {
            var m = Regex.Match(type, @"<(.+)>");
            if (!m.Success) yield break;

            foreach (var a in SplitTop(m.Groups[1].Value)) yield return BareType(a.Trim());
        }

        private static string Visibility(string mods)
        {
            if (mods.Contains("public")) return "+";
            if (mods.Contains("protected")) return "#";
            if (mods.Contains("internal")) return "~";

            return "-";
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
    }
}
