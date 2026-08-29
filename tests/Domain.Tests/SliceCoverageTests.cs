using System;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 型が関心事のどれかに属しているかを見る。図はやめたが、この検査だけ残した。
    /// Domain に型を足して割り当てを忘れると、ここで落ちる。
    /// </summary>
    public class SliceCoverageTests
    {
        [Test]
        public void すべての型がどれかのスライスの核に入っている()
        {
            var uncovered = SliceTable.UncoveredTypes();

            Assert.IsEmpty(uncovered,
                "どのスライスの核にも入っていない型がある。" +
                SliceTable.TablePath + " のどれかの行に足すこと。" +
                Environment.NewLine + string.Join(", ", uncovered));
        }
    }
}
