using System;
using System.Collections.Generic;
using System.Linq;
using MemorialFloor.Domain;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace MemorialFloor.Tests.Integrity
{
    /// <summary>
    /// 敵が落とす品が、拾える品になっているかを見る。落とし物の絵は品の ID で Resources から引き（Slime）、
    /// 拾うときは絵の名前を品の ID として読む（Dropitem）。綴りが違うと絵が出ず、原簿に無いと積めない。
    ///
    /// 敵は Resources の prefab だけを見る。湧かせる側が引くのはそこにある物だけ
    /// </summary>
    public class EnemyDropTests
    {
        private const string Enemies = "Assets/_Project/Resources";

        private const string CatalogPath = "Assets/_Project/Resources/ItemData.csv";

        [Test]
        public void 敵が落とす品はすべて原簿にあり品と同じ名前の絵がある()
        {
            ItemCatalog catalog = ItemCatalog.Parse(AssetDatabase.LoadAssetAtPath<TextAsset>(CatalogPath).text);
            List<GameObject> enemies = EnemyPrefabs().ToList();
            List<string> broken = new List<string>();

            foreach (GameObject enemy in enemies)
            {
                foreach (string itemId in DropsOf(enemy))
                {
                    if (catalog.Find(itemId) == null) broken.Add(enemy.name + " の " + itemId + " が原簿に無い");

                    Sprite icon = Resources.Load<Sprite>(itemId);
                    if (icon == null) broken.Add(enemy.name + " の " + itemId + " の絵が Resources に無い");
                    else if (icon.name != itemId) broken.Add(enemy.name + " の " + itemId + " の絵の名前が " + icon.name);
                }
            }

            Assert.IsNotEmpty(enemies, Enemies + " に敵が無い。突き合わせが空回りする");
            Assert.IsEmpty(broken, "拾えない落とし物がある\n" + string.Join("\n", broken));
        }

        [Test]
        public void ソーダスライムはスライムゼリーの代わりに専用のゼリーを落としスライムコアはスライムより出やすい()
        {
            List<string> always = Strings(Fields(Enemy("SodaSlime")).FindProperty("drops"));
            CollectionAssert.Contains(always, "SodaSlimejelly");
            CollectionAssert.DoesNotContain(always, "Slimejelly");

            Assert.Greater(CorePercent(Enemy("SodaSlime")), CorePercent(Enemy("Slime")), "スライムコアがスライムより出やすくない");
        }

        /// <summary>必ず落ちる品と、割合で落ちる品。空の ID は落ちない（DropRule）</summary>
        private static IEnumerable<string> DropsOf(GameObject enemy)
        {
            SerializedObject fields = Fields(enemy);
            IEnumerable<string> chances = Elements(fields.FindProperty("chanceDrops"))
                .Select(chance => chance.FindPropertyRelative("ItemId").stringValue);

            return Strings(fields.FindProperty("drops")).Concat(chances).Where(itemId => itemId.Length > 0);
        }

        private static int CorePercent(GameObject enemy)
        {
            return Elements(Fields(enemy).FindProperty("chanceDrops"))
                .Where(chance => chance.FindPropertyRelative("ItemId").stringValue == "Slimecore")
                .Sum(chance => chance.FindPropertyRelative("Percent").intValue);
        }

        private static IEnumerable<GameObject> EnemyPrefabs()
        {
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { Enemies }))
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid));
                if (prefab != null && prefab.GetComponent(SlimeType()) != null) yield return prefab;
            }
        }

        private static GameObject Enemy(string name)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + "/" + name + ".prefab");
            Assert.IsNotNull(prefab, name + " の prefab が無い");

            return prefab;
        }

        private static SerializedObject Fields(GameObject enemy)
        {
            Component slime = enemy.GetComponent(SlimeType());
            Assert.IsNotNull(slime, enemy.name + " に Slime が無い");

            return new SerializedObject(slime);
        }

        private static List<string> Strings(SerializedProperty list)
        {
            return Elements(list).Select(item => item.stringValue).ToList();
        }

        private static IEnumerable<SerializedProperty> Elements(SerializedProperty list)
        {
            Assert.IsNotNull(list, "Slime の落とす品の欄が無い");

            for (int i = 0; i < list.arraySize; i++) yield return list.GetArrayElementAtIndex(i);
        }

        /// <summary>古いスクリプトの型。asmdef の無いアセンブリにあり、ここからは名前でしか引けない</summary>
        private static Type SlimeType()
        {
            Type type = Type.GetType("Slime, Assembly-CSharp");
            Assert.IsNotNull(type, "Slime が Assembly-CSharp に無い");

            return type;
        }
    }
}
