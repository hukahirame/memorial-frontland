using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class FenceDurabilityTests
    {
        [Test]
        public void 削られてからしばらくは戻らずそのあと時間で戻る()
        {
            FenceDurability fence = new FenceDurability(200);
            fence.Strike(100, 0f);

            fence.Repair(FenceDurability.RepairDelay - 0.1f, 1f);
            Assert.AreEqual(100, fence.Current, "削られた直後に戻る");

            fence.Repair(FenceDurability.RepairDelay, 1f);
            Assert.AreEqual(100 + (int)FenceDurability.RepairPerSecond, fence.Current);
        }

        [Test]
        public void 尽きると倒れて削られず四分の一を超えるまで戻ると起き上がる()
        {
            FenceDurability fence = new FenceDurability(200);
            int line = (int)(200 * FenceDurability.RiseRatio);

            Assert.IsFalse(fence.Strike(199, 0f), "尽きる前に倒れる");
            Assert.IsTrue(fence.Strike(1, 0f), "尽きても倒れない");
            Assert.IsFalse(fence.Strike(10, 0f), "倒れているのに削られる");

            float now = FenceDurability.RepairDelay;
            float under = (line - 0.5f) / FenceDurability.RepairPerSecond;
            Assert.IsFalse(fence.Repair(now, under), "4 分の 1 を超える前に起き上がる");
            Assert.IsTrue(fence.IsFallen);
            Assert.IsTrue(fence.Repair(now + under, 1f / FenceDurability.RepairPerSecond), "4 分の 1 を超えても起き上がらない");
            Assert.IsFalse(fence.IsFallen);
        }

        [Test]
        public void 離れていたあいだに戻るぶんを足して置き直し倒れていたかも戻す()
        {
            FenceDurability fence = new FenceDurability(200);

            fence.Restore(10, true, FenceDurability.RepairDelay + 20f / FenceDurability.RepairPerSecond);
            Assert.AreEqual(30, fence.Current);
            Assert.IsTrue(fence.IsFallen, "起き上がる耐久まで戻っていないのに立っている");

            fence.Restore(10, true, FenceDurability.RepairDelay + 100f / FenceDurability.RepairPerSecond);
            Assert.AreEqual(110, fence.Current);
            Assert.IsFalse(fence.IsFallen, "起き上がる耐久を超えて戻っているのに倒れている");

            fence.Restore(30, false, 0f);
            Assert.IsFalse(fence.IsFallen, "削られて立っていた柵が倒れて戻る");
        }
    }

    //位置はどれも格子の 1 マスを単位にして書く。マスの大きさを変えても同じ形で見られる
    public class FenceRuleTests
    {
        private const float C = FenceRule.Cell;

        private const float Clearance = 0.6f * C;

        private const float Tolerance = 1e-5f;

        private const float Right = 1f;

        private const float Left = -1f;

        [Test]
        public void 右を向いていれば右の最初の線に奥行きの向きで立てる()
        {
            FenceSpot spot = FenceRule.Spot(3.2f * C, -2.3f * C, Right, Clearance);

            Assert.AreEqual(4f * C, spot.X, Tolerance);
            Assert.AreEqual(-2f * C, spot.Z, Tolerance);
            Assert.AreEqual(90f, spot.Yaw);
        }

        [Test]
        public void 左を向いていれば左の最初の線に立てる()
        {
            FenceSpot spot = FenceRule.Spot(3.2f * C, -2.3f * C, Left, Clearance);

            Assert.AreEqual(2f * C, spot.X, Tolerance);
            Assert.AreEqual(-2f * C, spot.Z, Tolerance);
            Assert.AreEqual(90f, spot.Yaw);
        }

        [Test]
        public void 線が近すぎるときはその先の線に立てる()
        {
            FenceSpot spot = FenceRule.Spot(3.6f * C, 0f, Right, Clearance);

            Assert.AreEqual(5f * C, spot.X, Tolerance, "4 マス目の線は 0.4 マスしか離れておらず、柵が体に重なる");
        }

        [Test]
        public void 向きの側が決まっていなければ右に立てる()
        {
            FenceSpot spot = FenceRule.Spot(0.3f * C, 0.3f * C, 0f, Clearance);

            Assert.AreEqual(90f, spot.Yaw);
            Assert.AreEqual(1f * C, spot.X, Tolerance);
        }

        [Test]
        public void 同じ向きのまま格子を2つ進んで立てると端がつながる()
        {
            FenceSpot first = FenceRule.Spot(0f, 0.2f * C, Right, Clearance);
            FenceSpot next = FenceRule.Spot(0.1f * C, 2.1f * C, Right, Clearance);

            Assert.AreEqual(first.X, next.X, Tolerance, "並べた柵が一直線にならない");
            Assert.AreEqual(FenceRule.Length, next.Z - first.Z, Tolerance, "並べた柵の端が離れるか重なる");
        }
    }
}
