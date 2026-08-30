using System;
using System.IO;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// docs/dependency-list.md が今のソースと一致するか見る。
    /// 一致しなければ書き換えたうえで失敗する。git diff に依存の変化が出る。
    /// </summary>
    public class DependencyListTests
    {
        [Test]
        public void 依存関係一覧がソースと一致する()
        {
            string expected = DependencyList.Generate();
            string full = Path.Combine(SourceIndex.RepositoryRoot(),
                                       DependencyList.OutputPath.Replace('/', Path.DirectorySeparatorChar));
            string actual = File.Exists(full) ? File.ReadAllText(full) : null;

            // 改行コードは比較しない。git が作業コピーを CRLF に戻すため
            if (Normalize(actual) == Normalize(expected)) return;

            File.WriteAllText(full, expected);
            Assert.Fail(DependencyList.OutputPath + " が古かったため書き直した。差分を見ること。");
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace(((char)13).ToString(), "");
        }
    }
}
