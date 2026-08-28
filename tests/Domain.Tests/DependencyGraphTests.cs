using System;
using System.IO;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 生成器の出力が、コミット済みの素データと一致するか見る。
    /// 生成そのものは DependencyGraphGenerator にある。
    ///
    /// 検査をテストに載せているのは、既存の dotnet test と CI がそのまま
    /// 見張り役になるため。独立したツールにすると、実行を忘れても誰も気づかない。
    /// </summary>
    public class DependencyGraphTests
    {
        [Test]
        public void 依存の素データがソースと一致する()
        {
            string expected = DependencyGraphGenerator.Generate();
            string full = Path.Combine(DependencyGraphGenerator.RepositoryRoot(),
                                       DependencyGraphGenerator.OutputPath);
            string actual = File.Exists(full) ? File.ReadAllText(full) : null;

            // 改行コードは比較しない。git が作業コピーを CRLF に戻すため、
            // 生成側の LF とそのままでは永久に一致しない
            if (Normalize(actual) == Normalize(expected)) return;

            Directory.CreateDirectory(Path.GetDirectoryName(full));
            File.WriteAllText(full, expected.Replace("\n", Environment.NewLine));
            Assert.Fail(
                DependencyGraphGenerator.OutputPath + " がソースと一致していなかったため書き換えた。\n" +
                "tools/diagram-diff.ps1 で変化を図にし、dependencies-diagrams/ を更新すること。");
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace("\r\n", "\n");
        }
    }
}
