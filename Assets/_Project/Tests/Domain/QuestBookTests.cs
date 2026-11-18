using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class QuestBookTests
    {
        private static readonly Root Root1 = new Root("Root1", "はじまりの根源", "0101", 1000, 0f, 0f);
        private static readonly Root Root2 = new Root("Root2", "水の根源", "0202", 2000, 0f, 0f);

        private static QuestBook Started()
        {
            QuestBook book = new QuestBook();
            book.IssueFirst(Root1);
            book.IssueFirst(Root2);
            return book;
        }

        private static Quest MainOf(QuestBook book, string rootId)
        {
            return book.Quests.All.Single(quest => quest.Kind == QuestKind.Main && quest.RootId == rootId);
        }

        private static Quest SubOf(QuestBook book, string rootId)
        {
            return book.Quests.All.Single(quest => quest.Kind == QuestKind.Sub && quest.RootId == rootId);
        }

        private static IReadOnlyList<Quest> Defeat(QuestBook book, string target, string rootId)
        {
            return book.DefeatIn(target, rootId, out _);
        }

        //受けて、根源に入り、核を壊して、生きて出る
        private static Quest Clear(QuestBook book, string rootId)
        {
            Quest main = MainOf(book, rootId);
            Assert.AreEqual(OrderResult.Ordered, book.ToggleOrder(main.Id, null));
            book.Start(rootId);
            Defeat(book, QuestBook.CoreTarget, rootId);
            book.Leave(rootId);
            return main;
        }

        [Test]
        public void 始めは根源ごとに調査とサブが1本ずつ出て二度は出ない()
        {
            QuestBook book = Started();
            book.IssueFirst(Root1);

            Assert.AreEqual(4, book.Quests.Count);
            Assert.AreEqual(QuestRewards.For(QuestKind.Main, 2000)[0].Amount, MainOf(book, "Root2").Rewards[0].Amount);
        }

        [Test]
        public void 夜明けの調査は根源ごとに1度保留してから出る()
        {
            QuestBook book = new QuestBook();

            Assert.IsTrue(book.IssueAtDawn(new[] { Root1 }), "サブが出ない");
            Assert.AreEqual(0, book.Quests.All.Count(quest => quest.Kind == QuestKind.Main), "保留せずに調査が出る");

            Assert.IsTrue(book.IssueAtDawn(new[] { Root1 }));
            Assert.AreEqual(1, book.Quests.All.Count(quest => quest.Kind == QuestKind.Main));

            Assert.IsFalse(book.IssueAtDawn(new[] { Root1 }), "調査とサブがあるのにまた出る");
        }

        [Test]
        public void 決壊は氾濫した根源に1本だけ出る()
        {
            QuestBook book = Started();

            Assert.IsTrue(book.IssueBreach(Root2));
            Assert.IsFalse(book.IssueBreach(Root2));
            Assert.AreEqual(1, book.Quests.All.Count(quest => quest.Kind == QuestKind.Breach));
        }

        [Test]
        public void 受注は全体で1本だけで受けたものを押すと解除になる()
        {
            QuestBook book = Started();
            string root1 = MainOf(book, "Root1").Id;
            string root2 = MainOf(book, "Root2").Id;

            Assert.AreEqual(OrderResult.Ordered, book.ToggleOrder(root1, null));
            Assert.AreEqual(OrderResult.OtherOrdered, book.ToggleOrder(root2, null));
            Assert.AreEqual(root1, book.OrderedId);

            Assert.AreEqual(OrderResult.Cancelled, book.ToggleOrder(root1, null));
            Assert.AreEqual("", book.OrderedId);
            Assert.AreEqual(OrderResult.Ordered, book.ToggleOrder(root2, null));
        }

        [Test]
        public void 対象の根源の中では受けも解除もできない()
        {
            QuestBook book = Started();
            string root1 = MainOf(book, "Root1").Id;

            Assert.AreEqual(OrderResult.CannotOrderInside, book.ToggleOrder(root1, "Root1"));
            Assert.AreEqual(OrderResult.Ordered, book.ToggleOrder(root1, "Root2"), "ほかの根源からは受けられる");
            Assert.AreEqual(OrderResult.CannotCancelInside, book.ToggleOrder(root1, "Root1"));
            Assert.AreEqual(root1, book.OrderedId);
        }

        [Test]
        public void 達成したクエストは受け取るまで受け直しも解除もできない()
        {
            QuestBook book = Started();
            Quest main = MainOf(book, "Root1");
            book.ToggleOrder(main.Id, null);
            Defeat(book, QuestBook.CoreTarget, "Root1");

            Assert.AreEqual(OrderResult.Settled, book.ToggleOrder(main.Id, null));
            Assert.AreEqual(main.Id, book.OrderedId);
        }

        [Test]
        public void 受けた調査は対象の根源の中でだけ進み通り道の根源では進まない()
        {
            QuestBook book = Started();
            Quest root2 = MainOf(book, "Root2");
            book.ToggleOrder(root2.Id, null);

            Assert.IsEmpty(Defeat(book, QuestBook.CoreTarget, "Root1"), "通り道の根源の核で進む");
            Assert.AreEqual(0, root2.Progress);

            CollectionAssert.AreEqual(new[] { root2 }, Defeat(book, QuestBook.CoreTarget, "Root2"));
            Assert.IsTrue(root2.IsComplete);
        }

        [Test]
        public void 受けていない調査は対象の根源で核を壊しても進まない()
        {
            QuestBook book = Started();

            Assert.IsEmpty(Defeat(book, QuestBook.CoreTarget, "Root1"));
            Assert.AreEqual(0, MainOf(book, "Root1").Progress);
        }

        [Test]
        public void サブはその根源で倒すと進み達成したその場で受け取って一覧から消える()
        {
            QuestBook book = Started();
            Quest sub = SubOf(book, "Root1");

            Assert.IsEmpty(Defeat(book, QuestBook.SubTarget, "Root2").Where(quest => quest == sub), "ほかの根源で進む");

            for (int i = 1; i < QuestBook.SubAmount; i++) Defeat(book, QuestBook.SubTarget, "Root1");
            IReadOnlyList<Quest> advanced = book.DefeatIn(QuestBook.SubTarget, "Root1", out IReadOnlyList<Quest> paid);

            CollectionAssert.Contains(advanced, sub);
            CollectionAssert.AreEqual(new[] { sub }, paid);
            Assert.IsNull(book.Quests.Find(sub.Id));
        }

        [Test]
        public void 達成したあとに対象を倒しても進めない()
        {
            QuestBook book = Started();
            Quest main = MainOf(book, "Root1");
            book.ToggleOrder(main.Id, null);
            Defeat(book, QuestBook.CoreTarget, "Root1");

            Assert.IsEmpty(Defeat(book, QuestBook.CoreTarget, "Root1"));
        }

        [Test]
        public void 受けた根源に入ると始まりほかの根源は通り道()
        {
            QuestBook book = Started();
            Quest root2 = MainOf(book, "Root2");
            book.ToggleOrder(root2.Id, null);

            Assert.IsNull(book.Start("Root1"));
            Assert.AreEqual("", book.ResultQuestId);

            Assert.AreSame(root2, book.Start("Root2"));
            Assert.AreEqual(root2.Id, book.ResultQuestId);
        }

        [Test]
        public void 達成したまま根源を出て根源に着くと達成済みをすべて受け取る()
        {
            QuestBook book = Started();
            Quest main = Clear(book, "Root1");

            Assert.AreEqual(QuestOutcome.Succeeded, book.PendingOutcome);
            Assert.AreEqual("", book.OrderedId, "出た時点で受注が空かない");

            Quest claimed = book.Arrive(false, out IReadOnlyList<Quest> paid);

            Assert.AreSame(main, claimed);
            CollectionAssert.AreEqual(new[] { main }, paid);
            Assert.IsNull(book.Quests.Find(main.Id));
            Assert.AreEqual(QuestOutcome.None, book.PendingOutcome);
        }

        [Test]
        public void 拠点に着いたときは受け取らず結果の画面に回す()
        {
            QuestBook book = Started();
            Quest main = Clear(book, "Root1");

            Assert.IsNull(book.Arrive(true, out IReadOnlyList<Quest> paid));
            Assert.IsEmpty(paid);
            Assert.AreEqual(QuestOutcome.Succeeded, book.TakeOutcome());

            CollectionAssert.AreEqual(new[] { main }, book.PayCleared());
            Assert.IsNull(book.Quests.Find(main.Id));
        }

        [Test]
        public void 達成していても根源を出る前に倒れると失敗になり進みが戻って受注は残る()
        {
            QuestBook book = Started();
            Quest main = MainOf(book, "Root1");
            book.ToggleOrder(main.Id, null);
            book.Start("Root1");
            Defeat(book, QuestBook.CoreTarget, "Root1");

            Assert.IsTrue(book.IsUnderwayIn("Root1"));
            book.Fail();
            book.Leave("Root1");

            Assert.AreEqual(QuestOutcome.Failed, book.TakeOutcome());
            Assert.AreEqual(0, main.Progress);
            Assert.AreEqual(main.Id, book.OrderedId);
            Assert.AreEqual(QuestOutcome.None, book.TakeOutcome(), "結果が空かない");
        }

        [Test]
        public void 受けた根源の外では倒れても失敗にならない()
        {
            QuestBook book = Started();
            book.ToggleOrder(MainOf(book, "Root2").Id, null);

            Assert.IsFalse(book.IsUnderwayIn("Root1"));
            Assert.IsFalse(book.IsUnderwayIn(null));
        }

        [Test]
        public void 受け取ると受け取ったものの受注だけが空く()
        {
            QuestBook book = Started();
            Quest root2 = MainOf(book, "Root2");
            book.ToggleOrder(root2.Id, null);

            //受注していない共通クエストを達成して受け取っても、受注は残る
            book.Quests.Create(QuestKind.Common, "Common", QuestBook.SubTarget, 1, new Reward[0]);
            Defeat(book, QuestBook.SubTarget, null);
            book.PayCleared();

            Assert.AreEqual(root2.Id, book.OrderedId);
        }
    }
}
