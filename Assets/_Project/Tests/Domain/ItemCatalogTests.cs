using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class ItemCatalogTests
    {
        private const string Csv =
            "システム内名称,名称,最大ストック,最大耐久値,設置可,,,,,,紹介文\n" +
            "Slimejelly,スライムゼリー,100,0,1,,,,,,噛み応えバツグン。\n" +
            "Branch,木の枝,100,0,0,,,,,,色々なことに使える。\n";

        [Test]
        public void 見出し行は品目に数えない()
        {
            ItemCatalog catalog = ItemCatalog.Parse(Csv);

            Assert.AreEqual(2, catalog.Count);
            Assert.IsNull(catalog.Find("システム内名称"), "見出しが品目として残っている");
        }

        [Test]
        public void 列を素性に割り当てる()
        {
            ItemDefinition item = ItemCatalog.Parse(Csv).Find("Slimejelly");

            Assert.IsNotNull(item);
            Assert.AreEqual("スライムゼリー", item.Name);
            Assert.AreEqual(100, item.MaxStock);
            Assert.AreEqual(0, item.MaxDurability);
            Assert.IsTrue(item.Installable);
            Assert.AreEqual("噛み応えバツグン。", item.Description);
        }

        [Test]
        public void 設置可は1のときだけ真()
        {
            Assert.IsFalse(ItemCatalog.Parse(Csv).Find("Branch").Installable);
        }

        [Test]
        public void 無い_Id_は_null_を返す()
        {
            // 旧実装は for (int i = 0; index == -1; i++) で探しており、
            // 見つからないと配列の末尾を越えて例外になっていた
            Assert.IsNull(ItemCatalog.Parse(Csv).Find("NotExist"));
            Assert.IsNull(ItemCatalog.Parse(Csv).Find(""));
            Assert.IsNull(ItemCatalog.Parse(Csv).Find(null));
        }

        [Test]
        public void 空行と列の足りない行は捨てる()
        {
            string csv = "見出し\n\nBroken,名前,1\nGood,良品,5,0,0,,,,,,説明\n";
            ItemCatalog catalog = ItemCatalog.Parse(csv);

            Assert.AreEqual(1, catalog.Count);
            Assert.IsNull(catalog.Find("Broken"));
            Assert.IsNotNull(catalog.Find("Good"));
        }

        [Test]
        public void 数として読めない値は0にする()
        {
            string csv = "見出し\nX,名前,あ,い,う,,,,,,説明\n";
            ItemDefinition item = ItemCatalog.Parse(csv).Find("X");

            Assert.AreEqual(0, item.MaxStock);
            Assert.AreEqual(0, item.MaxDurability);
            Assert.IsFalse(item.Installable);
        }

        [Test]
        public void 空の原簿でも落ちない()
        {
            Assert.AreEqual(0, ItemCatalog.Parse("").Count);
            Assert.AreEqual(0, ItemCatalog.Parse(null).Count);
        }
    }
}
