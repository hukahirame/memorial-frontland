using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// Domain の公開している面を一覧にして docs/public-api.txt を作る。
    /// 一致しなければ書き換えたうえで失敗する。git diff に面の変化が出る。
    ///
    /// refactor で人間が承認するのはこの差分だけ（tools/flow/flow-rules/refactor.md）。
    /// 実装は見ない。
    /// </summary>
    public class PublicApiTests
    {
        public const string OutputPath = "docs/public-api.txt";

        /// <summary>Domain だけを見る。Game と Legacy は動いている最中なので固定しない</summary>
        private const string Layer = "Domain";

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
            SourceIndex.Index index = SourceIndex.Build();

            StringBuilder text = new StringBuilder();
            text.Append("# 公開API。PublicApiTests が作る。手で編集しない。\n");
            text.Append("#\n");
            text.Append("# Domain が使う側へ見せている型とメンバ。増減はここに差分として出る。\n");
            text.Append("# refactor で人間が見るのはこの差分だけ。\n");
            text.Append("#\n");
            text.Append("# 入れ子の型は外側と同じ高さに並ぶ。private と internal は入らない。\n");

            if (!index.PublicApi.TryGetValue(Layer, out var signatures)) return text.ToString();

            string owner = null;

            foreach (string signature in signatures)
            {
                string type = signature.Split('.')[0];
                if (type != owner)
                {
                    text.Append('\n');
                    owner = type;
                }

                text.Append(signature).Append('\n');
            }

            return text.ToString();
        }

        private static string Normalize(string text)
        {
            return text == null ? null : text.Replace(((char)13).ToString(), "");
        }
    }
}
