using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 仕様書とコードを突き合わせる。
    ///
    /// 仕様の文と振る舞いが合っているかは決定的には見られない。ここで見るのは2つだけ。
    /// 名指しした先が実在すること。書いた定数の値がコードと一致すること。
    /// どちらも、コードを変えたときに仕様書が黙って嘘になるのを止める。
    /// </summary>
    public class SpecConformanceTests
    {
        private const string Directory = "docs/systems";

        /// <summary>調整値の表。`項目 | 値 | 居場所` の3欄</summary>
        private const string TuningHeading = "## 調整値";

        /// <summary>バッククォートで囲った「型.メンバ」だけを拾う。道や式には当たらない</summary>
        private static readonly Regex Symbol = new Regex("`([A-Z][A-Za-z0-9_]*)[.]([A-Za-z0-9_]+)`");

        [Test]
        public void 仕様書が名指しした型とメンバが実在する()
        {
            SourceIndex.Index index = SourceIndex.Build();
            List<string> missing = new List<string>();

            foreach (string path in SpecFiles())
            {
                string name = Path.GetFileName(path);
                string[] lines = File.ReadAllLines(path);

                for (int i = 0; i < lines.Length; i++)
                {
                    foreach (Match match in Symbol.Matches(lines[i]))
                    {
                        string type = match.Groups[1].Value;

                        //このリポジトリの型でなければ見ない。UnityEngine の API など
                        if (!index.Layers.ContainsKey(type)) continue;

                        string member = type + "." + match.Groups[2].Value;
                        if (index.Members.Contains(member)) continue;

                        missing.Add(name + ":" + (i + 1) + " " + member);
                    }
                }
            }

            Assert.IsEmpty(missing,
                "仕様書が名指ししている先がコードに無い。改名か削除で仕様書が置き去りになっている。" +
                Environment.NewLine + string.Join(Environment.NewLine, missing));
        }

        [Test]
        public void 調整値に書いた定数がコードと一致する()
        {
            SourceIndex.Index index = SourceIndex.Build();
            List<string> wrong = new List<string>();
            int compared = 0;

            foreach (string path in SpecFiles())
            {
                string name = Path.GetFileName(path);

                foreach ((string value, string member, int line) in TuningRows(path))
                {
                    if (!index.Constants.TryGetValue(member, out string actual)) continue;
                    if (!Number(value, out decimal stated)) continue;
                    if (!Number(actual, out decimal real)) continue;

                    compared++;
                    if (stated == real) continue;

                    wrong.Add(name + ":" + line + " " + member +
                              " は仕様書が " + value + "、コードは " + actual);
                }
            }

            Assert.IsEmpty(wrong,
                "仕様書に書いた値がコードと違う。どちらかが古い。" +
                Environment.NewLine + string.Join(Environment.NewLine, wrong));

            Assert.Greater(compared, 0,
                "突き合わせた定数が1つも無い。調整値の表が「項目 | 値 | 居場所」の形か、" +
                "居場所がバッククォートで囲った「型.メンバ」になっているかを見ること");
        }

        /// <summary>調整値の節の表の行。値と、居場所が指す「型.メンバ」と、行番号</summary>
        private static IEnumerable<(string Value, string Member, int Line)> TuningRows(string path)
        {
            string[] lines = File.ReadAllLines(path);
            bool inside = false;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (line.StartsWith("## ", StringComparison.Ordinal))
                {
                    inside = line == TuningHeading;
                    continue;
                }

                if (!inside || !line.StartsWith("|", StringComparison.Ordinal)) continue;

                string[] cell = line.Trim('|').Split('|').Select(c => c.Trim()).ToArray();
                if (cell.Length != 3) continue;

                Match where = Symbol.Match(cell[2]);
                if (!where.Success) continue;

                yield return (cell[1], where.Groups[1].Value + "." + where.Groups[2].Value, i + 1);
            }
        }

        private static bool Number(string text, out decimal value)
        {
            return decimal.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out value);
        }

        /// <summary>INDEX.md は一覧なので仕様書に数えない</summary>
        private static IEnumerable<string> SpecFiles()
        {
            string dir = Path.Combine(SourceIndex.RepositoryRoot(),
                                      Directory.Replace('/', Path.DirectorySeparatorChar));

            return System.IO.Directory.GetFiles(dir, "*.md")
                         .Where(p => Path.GetFileName(p) != "INDEX.md")
                         .OrderBy(p => p, StringComparer.Ordinal);
        }
    }
}
