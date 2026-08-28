using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// docs/dependencies-diagrams/ の現状図が、今のソースと一致するか見る。
    ///
    /// この図は SliceDiagramGenerator が作る。人が手で直すと次の実行で消えるので、
    /// 直したいことがあるならソースか SKILL.md のスライス表を変えること。
    /// 例外は「覚え書き」の節で、そこだけは作り直しても引き継ぐ。
    /// </summary>
    public class SliceDiagramTests
    {
        [Test]
        public void 現状図がソースと一致する()
        {
            var expected = SliceDiagramGenerator.GenerateAll();
            var stale = new List<string>();

            foreach (var pair in expected.OrderBy(p => p.Key, StringComparer.Ordinal))
            {
                string path = Path.Combine(Dir(), pair.Key);
                string actual = File.Exists(path) ? File.ReadAllText(path) : null;

                // 改行コードは比較しない。git が作業コピーを CRLF に戻すため、
                // 生成側の LF とそのままでは永久に一致しない
                if (Normalize(actual) == Normalize(pair.Value)) continue;

                File.WriteAllText(path, pair.Value.Replace("\n", Environment.NewLine));
                stale.Add(pair.Key);
            }

            Assert.IsEmpty(stale,
                "現状図が古かったため書き直した。差分を見て、意図した構造変化か確かめること。" +
                Environment.NewLine + string.Join(Environment.NewLine, stale));
        }

        [Test]
        public void 現状図に元を失ったファイルが残っていない()
        {
            var expected = SliceDiagramGenerator.GenerateAll();

            var orphans = Directory.GetFiles(Dir(), "*.md")
                                   .Select(Path.GetFileName)
                                   .Where(f => !expected.ContainsKey(f))
                                   .OrderBy(f => f, StringComparer.Ordinal)
                                   .ToList();

            Assert.IsEmpty(orphans,
                "対応する Domain のソースが無い図が残っている。" +
                "ファイルを消したか改名したなら、この図も消すこと。" +
                Environment.NewLine + string.Join(", ", orphans));
        }

        [Test]
        public void すべての型がどれかのスライスの核に入っている()
        {
            var uncovered = SliceDiagramGenerator.UncoveredTypes();

            Assert.IsEmpty(uncovered,
                "どのスライスの核にも入っていない型がある。地図に穴が開くので、" +
                "SKILL.md のスライス表のどれかに足すこと。" +
                Environment.NewLine + string.Join(", ", uncovered));
        }

        private static string Dir()
        {
            string dir = Path.Combine(DependencyGraphGenerator.RepositoryRoot(),
                                      SliceDiagramGenerator.OutputDir.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(dir);
            return dir;
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace("\r\n", "\n");
        }
    }
}
