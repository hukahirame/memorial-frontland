using System;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class TravelRuleTests
    {
        private static readonly string[] Roots = { "Root1", "Root2" };

        [Test]
        public void 拠点の奥の出口は最初の根源へ通じる()
        {
            Route route = TravelRule.Through(ExitKind.ToNext, TravelRule.BaseAreaId, Roots);

            Assert.IsFalse(route.IsRefused);
            Assert.AreEqual("Root1", route.AreaId);
        }

        [Test]
        public void 根源の出口は並びの隣へ通じる()
        {
            Assert.AreEqual("Root2", TravelRule.Through(ExitKind.ToNext, "Root1", Roots).AreaId);
            Assert.AreEqual(TravelRule.BaseAreaId, TravelRule.Through(ExitKind.ToPrevious, "Root1", Roots).AreaId);
            Assert.AreEqual("Root1", TravelRule.Through(ExitKind.ToPrevious, "Root2", Roots).AreaId);
        }

        [Test]
        public void 来た側の出口の前に立つ()
        {
            Assert.AreEqual(Arrival.FromPrevious, TravelRule.Through(ExitKind.ToNext, TravelRule.BaseAreaId, Roots).Arrival);
            Assert.AreEqual(Arrival.FromPrevious, TravelRule.Through(ExitKind.ToNext, "Root1", Roots).Arrival);
            Assert.AreEqual(Arrival.FromNext, TravelRule.Through(ExitKind.ToPrevious, "Root2", Roots).Arrival);
            Assert.AreEqual(Arrival.FromNext, TravelRule.Through(ExitKind.ToPrevious, "Root1", Roots).Arrival);
        }

        [Test]
        public void 並びの端より先へは行けない()
        {
            Assert.IsTrue(TravelRule.Through(ExitKind.ToPrevious, TravelRule.BaseAreaId, Roots).IsRefused);
            Assert.IsTrue(TravelRule.Through(ExitKind.ToNext, "Root2", Roots).IsRefused);
        }

        [Test]
        public void 並びに無い場所からは行けない()
        {
            Assert.IsTrue(TravelRule.Through(ExitKind.ToNext, "Root9", Roots).IsRefused);
            Assert.IsTrue(TravelRule.Through(ExitKind.ToNext, null, Roots).IsRefused);
        }

        [Test]
        public void 根源が無ければ拠点の奥へは行けない()
        {
            Assert.IsTrue(TravelRule.Through(ExitKind.ToNext, TravelRule.BaseAreaId, null).IsRefused);
            Assert.IsTrue(TravelRule.Through(ExitKind.ToNext, TravelRule.BaseAreaId, new string[0]).IsRefused);
        }

        [Test]
        public void 出口の種類の値はシーンに保存した値のまま()
        {
            Assert.AreEqual(0, (int)ExitKind.ToPrevious);
            Assert.AreEqual(1, (int)ExitKind.ToNext);
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
