using System;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class TravelRuleTests
    {
        private static Quest OrderedFor(string rootId)
        {
            return new Quest("X0", rootId, "MainSpawner", 1, null);
        }

        [Test]
        public void 拠点への出口はクエストを受けていなくても通れる()
        {
            Route route = TravelRule.Through(ExitKind.ToBase, null);

            Assert.IsFalse(route.IsRefused);
            Assert.AreEqual(TravelRule.BaseAreaId, route.AreaId);
        }

        [Test]
        public void 根源への出口は受注中のクエストの根源へ向かう()
        {
            Route route = TravelRule.Through(ExitKind.ToOrderedRoot, OrderedFor("Root2"));

            Assert.AreEqual("Root2", route.AreaId);
        }

        [Test]
        public void クエストを受けていなければ根源へは行けない()
        {
            Assert.IsTrue(TravelRule.Through(ExitKind.ToOrderedRoot, null).IsRefused);
        }

        [Test]
        public void 出口を通ってきたら入口に立つ()
        {
            Assert.AreEqual(Arrival.Entrance, TravelRule.Through(ExitKind.ToBase, null).Arrival);
            Assert.AreEqual(Arrival.Entrance, TravelRule.Through(ExitKind.ToOrderedRoot, OrderedFor("Root1")).Arrival);
        }

        [Test]
        public void 倒れたら拠点の決まった地点へ戻る()
        {
            Route route = TravelRule.Revive;

            Assert.AreEqual(TravelRule.BaseAreaId, route.AreaId);
            Assert.AreEqual(Arrival.Home, route.Arrival);
        }

        [Test]
        public void 保存した場所が無ければ拠点の決まった地点から始まる()
        {
            Assert.AreEqual(TravelRule.BaseAreaId, TravelRule.Begin(null).AreaId);
            Assert.AreEqual(TravelRule.BaseAreaId, TravelRule.Begin("").AreaId);
            Assert.AreEqual(Arrival.Home, TravelRule.Begin(null).Arrival);
        }

        [Test]
        public void 保存した場所があればそこから始まる()
        {
            Assert.AreEqual("Root1", TravelRule.Begin("Root1").AreaId);
        }

        [Test]
        public void 行き先の無い道は作れない()
        {
            Assert.Throws<ArgumentException>(() => Route.To(null, Arrival.Home));
            Assert.Throws<ArgumentException>(() => Route.To("", Arrival.Home));
        }
    }

    public class WhereaboutsTests
    {
        [Test]
        public void 着く前はどこにも居ない()
        {
            Whereabouts where = new Whereabouts();

            Assert.IsFalse(where.HasArrived);
            Assert.IsFalse(where.IsInBase);
            Assert.IsFalse(where.IsInRoot);
            Assert.IsNull(where.RootId);
        }

        [Test]
        public void 根源に居るときだけ根源のIdが分かる()
        {
            Whereabouts where = new Whereabouts();

            where.Arrive("Root1", AreaKind.Root);
            Assert.IsTrue(where.IsInRoot);
            Assert.AreEqual("Root1", where.RootId);

            where.Arrive(TravelRule.BaseAreaId, AreaKind.Base);
            Assert.IsTrue(where.IsInBase);
            Assert.IsFalse(where.IsInRoot);
            Assert.IsNull(where.RootId);
        }

        [Test]
        public void 場所の無い到着は受け付けない()
        {
            Whereabouts where = new Whereabouts();

            Assert.Throws<ArgumentException>(() => where.Arrive(null, AreaKind.Base));
            Assert.Throws<ArgumentException>(() => where.Arrive("", AreaKind.Root));
            Assert.IsFalse(where.HasArrived);
        }
    }
}
