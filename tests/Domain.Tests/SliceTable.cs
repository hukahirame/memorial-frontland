using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>
    /// 関心事スライスの表。すべての型がどれかの核に入っているかを見るためだけにある。
    /// 切り方の実体は docs/slices.txt にあり、この表はコードの索引でもある。
    /// </summary>
    public static class SliceTable
    {
        /// <summary>切り方の定義。人が触る唯一の入力</summary>
        public const string TablePath = "docs/slices.txt";

        private sealed class Slice
        {
            public List<string> Seeds;   // Domain/Root.cs か QuestManager
            public string Name;          // 根源 🌳
            public string Summary;
        }

        /// <summary>どのスライスの核にも入っていない型。放っておくと地図に穴が開く</summary>
        public static List<string> UncoveredTypes()
        {
            SourceIndex.Index index = SourceIndex.Build();
            HashSet<string> covered = new HashSet<string>(StringComparer.Ordinal);

            foreach (Slice slice in ReadSliceTable())
                foreach (string name in Resolve(slice, index))
                    covered.Add(name);

            return index.Layers.Keys
                        .Where(name => !covered.Contains(name))
                        .OrderBy(name => name, StringComparer.Ordinal)
                        .ToList();
        }

        /// <summary>核の指定を型の並びに直す。ファイルならそれが宣言する型に展開する</summary>
        private static List<string> Resolve(Slice slice, SourceIndex.Index index)
        {
            List<string> core = new List<string>();

            foreach (string seed in slice.Seeds)
            {
                if (seed.EndsWith(".cs", StringComparison.Ordinal))
                {
                    List<string> hit = index.Files.Keys
                        .Where(p => p == seed || p.EndsWith("/" + seed, StringComparison.Ordinal))
                        .ToList();

                    if (hit.Count != 1)
                        throw new InvalidOperationException(
                            slice.Name + " の核 " + seed + " が" +
                            (hit.Count == 0 ? "見つからない。" : "複数のファイルに当たる。もっと長い道で書くこと。"));

                    core.AddRange(index.Files[hit[0]]);
                }
                else if (index.Layers.ContainsKey(seed))
                {
                    core.Add(seed);
                }
                else
                {
                    throw new InvalidOperationException(
                        slice.Name + " の核 " + seed + " という型もファイルも無い。");
                }
            }

            return core;
        }

        private static List<Slice> ReadSliceTable()
        {
            string path = Path.Combine(SourceIndex.RepositoryRoot(),
                                       TablePath.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(path))
                throw new FileNotFoundException(TablePath + " が無い。スライスの切り方の実体はそこにある。");

            List<Slice> table = new List<Slice>();
            foreach (string raw in File.ReadAllText(path).Split('\n'))
            {
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith("#", StringComparison.Ordinal)) continue;

                string[] cell = line.Split('|').Select(c => c.Trim()).ToArray();
                if (cell.Length != 3)
                    throw new InvalidOperationException(
                        "スライス表の行は「核 | 名前 | 一行説明」の3欄。合わない行: " + line);

                table.Add(new Slice
                {
                    Seeds = cell[0].Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).ToList(),
                    Name = cell[1],
                    Summary = cell[2],
                });
            }

            return table;
        }
    }
}
