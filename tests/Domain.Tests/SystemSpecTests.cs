using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 仕様書と関心事の対応を見る。関心事の単位は層の直下のフォルダで、
    /// Domain / Game / Legacy の同じ名前のフォルダが1つの関心事になる。仕様書の id はその名前。
    /// フォルダを足したのに仕様書を書き忘れると、ここで落ちる。
    /// </summary>
    public class SystemSpecTests
    {
        private const string Directory = "docs/systems";

        /// <summary>どの仕様書にも要る欄</summary>
        private static readonly string[] RequiredSections =
        {
            "体験の意図", "デフォルトと違う点", "相互作用", "やらないこと", "未決", "確認", "調整値"
        };

        /// <summary>frontmatter に要る鍵</summary>
        private static readonly string[] RequiredKeys = { "id", "name", "touches" };

        [Test]
        public void すべての型が関心事のフォルダに入っている()
        {
            SourceIndex.Index index = SourceIndex.Build();

            List<string> loose = index.Owner
                .Where(pair => !pair.Value.Contains("/"))
                .Select(pair => pair.Value + " 直下の " + pair.Key)
                .OrderBy(text => text, StringComparer.Ordinal)
                .ToList();

            Assert.IsEmpty(loose, "層の直下に型がある。関心事のフォルダに入れること。" +
                Environment.NewLine + string.Join(Environment.NewLine, loose));
        }

        [Test]
        public void 関心事のフォルダと仕様書が1対1で対応する()
        {
            HashSet<string> concerns = new HashSet<string>(
                SourceIndex.Build().Nodes
                           .Where(node => node.Contains("/"))
                           .Select(node => node.Substring(node.IndexOf('/') + 1).ToLowerInvariant()),
                StringComparer.Ordinal);

            HashSet<string> ids = new HashSet<string>(
                SpecFiles().Select(Path.GetFileNameWithoutExtension), StringComparer.Ordinal);

            List<string> missing = concerns.Where(name => !ids.Contains(name))
                                           .OrderBy(name => name, StringComparer.Ordinal).ToList();
            Assert.IsEmpty(missing,
                "仕様書の無い関心事のフォルダがある。" + Directory + " に <フォルダ名を小文字にしたもの>.md を足すこと。" +
                Environment.NewLine + string.Join(", ", missing));

            List<string> extra = ids.Where(id => !concerns.Contains(id))
                                    .OrderBy(id => id, StringComparer.Ordinal).ToList();
            Assert.IsEmpty(extra,
                "どのフォルダにも当たらない仕様書がある。id を関心事のフォルダ名に合わせること。" +
                Environment.NewLine + string.Join(", ", extra));
        }

        [Test]
        public void 仕様書の_id_がファイル名と一致する()
        {
            List<string> wrong = new List<string>();

            foreach (string path in SpecFiles())
            {
                string name = Path.GetFileNameWithoutExtension(path);
                string id = Value(File.ReadAllText(path), "id");

                if (id != name) wrong.Add(name + ".md の id は " + (id ?? "無し"));
            }

            Assert.IsEmpty(wrong, "id とファイル名が違う。" +
                Environment.NewLine + string.Join(Environment.NewLine, wrong));
        }

        [Test]
        public void 必須の欄が揃っている()
        {
            List<string> lacking = new List<string>();

            foreach (string path in SpecFiles())
            {
                string text = File.ReadAllText(path);
                string name = Path.GetFileName(path);

                foreach (string key in RequiredKeys)
                {
                    if (Value(text, key) == null) lacking.Add(name + " frontmatter の " + key);
                }

                foreach (string section in RequiredSections)
                {
                    if (!text.Contains("## " + section)) lacking.Add(name + " ## " + section);
                }
            }

            Assert.IsEmpty(lacking, "仕様書に足りない欄がある。" +
                Environment.NewLine + string.Join(Environment.NewLine, lacking));
        }

        /// <summary>INDEX.md は一覧なので仕様書に数えない</summary>
        private static IEnumerable<string> SpecFiles()
        {
            string dir = Path.Combine(SourceIndex.RepositoryRoot(),
                                      Directory.Replace('/', Path.DirectorySeparatorChar));

            if (!System.IO.Directory.Exists(dir))
                throw new DirectoryNotFoundException(Directory + " が無い。仕様書の置き場はそこ。");

            return System.IO.Directory.GetFiles(dir, "*.md")
                         .Where(p => Path.GetFileName(p) != "INDEX.md")
                         .OrderBy(p => p, StringComparer.Ordinal);
        }

        /// <summary>frontmatter の値。無ければ null。入れ子は読まない</summary>
        private static string Value(string text, string key)
        {
            bool inside = false;

            foreach (string raw in text.Replace("\r", "").Split('\n'))
            {
                string line = raw.Trim('﻿').Trim();

                if (line == "---")
                {
                    if (inside) return null;

                    inside = true;
                    continue;
                }

                if (!inside) continue;
                if (!line.StartsWith(key + ":", StringComparison.Ordinal)) continue;

                return line.Substring(key.Length + 1).Trim();
            }

            return null;
        }
    }
}
