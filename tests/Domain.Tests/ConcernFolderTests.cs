using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 関心事の単位は層の直下のフォルダで、Domain / Game / Legacy の同じ名前のフォルダが1つの関心事になる（[D-20]）。
    /// 仕様書との対応は tools/spec/build.py が見る。
    /// </summary>
    public class ConcernFolderTests
    {
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
    }
}
