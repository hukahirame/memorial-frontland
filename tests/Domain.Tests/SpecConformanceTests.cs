using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 仕様書とコードを突き合わせる。
    ///
    /// 仕様の文と振る舞いが合っているかは決定的には見られない。ここで見るのは、名指しした先が実在することだけ。
    /// コードを変えたときに仕様書が黙って嘘になるのを止める。数は仕様書に書かないので、値は見ない。
    /// </summary>
    public class SpecConformanceTests
    {
        private const string Directory = "docs/systems";

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
