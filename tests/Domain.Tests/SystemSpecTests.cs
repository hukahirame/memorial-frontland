using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 仕様書とスライスの対応を見る。単位を2つ持つとズレるので、1:1に固定する。
    /// スライスを足したのに仕様書を書き忘れると、ここで落ちる。
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
        private static readonly string[] RequiredKeys = { "id", "slice", "touches" };

        [Test]
        public void スライスと仕様書が1対1で対応する()
        {
            List<string> slices = SliceTable.SliceNames();
            Dictionary<string, string> specs = Specs();

            List<string> missing = slices.Where(name => !specs.ContainsKey(name)).ToList();
            Assert.IsEmpty(missing,
                "仕様書の無いスライスがある。" + Directory + " に足すこと。" +
                Environment.NewLine + string.Join(", ", missing));

            List<string> extra = specs.Keys.Where(name => !slices.Contains(name)).ToList();
            Assert.IsEmpty(extra,
                "どのスライスにも当たらない仕様書がある。frontmatter の slice を " +
                SliceTable.TablePath + " の名前に合わせること。" +
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

        /// <summary>slice の名前 -> 道</summary>
        private static Dictionary<string, string> Specs()
        {
            Dictionary<string, string> found = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (string path in SpecFiles())
            {
                string slice = Value(File.ReadAllText(path), "slice");
                if (slice == null) continue;

                if (found.ContainsKey(slice))
                    throw new InvalidOperationException(
                        "同じスライスを指す仕様書が2つある: " + slice);

                found[slice] = path;
            }

            return found;
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
