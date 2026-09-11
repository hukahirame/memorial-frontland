using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class AttackRuleTests
    {
        private const float Right = 1f;
        private const float Left = -1f;

        [Test]
        public void 向いている側へ強く倒したら突き()
        {
            Assert.AreEqual(AttackKind.Thrust, AttackRule.Choose(0.9f, 0f, Right));
            Assert.AreEqual(AttackKind.Thrust, AttackRule.Choose(-0.9f, 0f, Left));
        }

        [Test]
        public void 向いていない側へ倒したらステップ()
        {
            Assert.AreEqual(AttackKind.Step, AttackRule.Choose(-0.9f, 0f, Right));
            Assert.AreEqual(AttackKind.Step, AttackRule.Choose(0.9f, 0f, Left));
        }

        [Test]
        public void どちらへも倒れていなければケサ()
        {
            Assert.AreEqual(AttackKind.Slash, AttackRule.Choose(0f, 0f, Right));
            Assert.AreEqual(AttackKind.Slash, AttackRule.Choose(0.4f, -0.4f, Right));
        }

        [Test]
        public void 上下だけ倒したらステップ()
        {
            Assert.AreEqual(AttackKind.Step, AttackRule.Choose(0f, 0.9f, Right));
        }

        [Test]
        public void 閾値ちょうどはケサにもステップにもならない境目()
        {
            // 突きは閾値より大きいことを求め、ケサは小さいことを求める。
            // 閾値ちょうどはどちらの条件にも当たらず、残りのステップへ落ちる
            Assert.AreEqual(AttackKind.Step, AttackRule.Choose(AttackRule.Threshold, 0f, Right));
        }

        [Test]
        public void 突きの上下は閾値を超えたときだけ出る()
        {
            Assert.AreEqual(1, AttackRule.ThrustDirection(0.9f));
            Assert.AreEqual(-1, AttackRule.ThrustDirection(-0.9f));
            Assert.AreEqual(0, AttackRule.ThrustDirection(0f));
            Assert.AreEqual(0, AttackRule.ThrustDirection(AttackRule.Threshold));
        }
    }
}
