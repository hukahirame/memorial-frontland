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

    public class AttackTimingTests
    {
        [Test]
        public void 当たる時間は振り終わる前に終わる()
        {
            Assert.Less(AttackTiming.WindupSeconds + AttackTiming.ActiveSeconds, AttackTiming.SwingSeconds);
        }

        [Test]
        public void 振りの終わり際に来た入力は預かる()
        {
            float duration = AttackTiming.SwingSeconds;

            Assert.IsTrue(AttackTiming.IsBuffered(duration - 0.1f, duration));
            Assert.IsTrue(AttackTiming.IsBuffered(duration - AttackTiming.BufferSeconds, duration));
        }

        [Test]
        public void 振りの序盤に来た入力は捨てる()
        {
            float duration = AttackTiming.SwingSeconds;

            Assert.IsFalse(AttackTiming.IsBuffered(0f, duration));
            Assert.IsFalse(AttackTiming.IsBuffered(duration - AttackTiming.BufferSeconds - 0.01f, duration));
        }
    }

    public class KnockbackRuleTests
    {
        private const float Right = 1f;
        private const float Left = -1f;

        [Test]
        public void 当てた側から相手へ向かう向きに飛ぶ()
        {
            Push push = KnockbackRule.Velocity(0f, 0f, 3f, 4f, 5f, Right);

            Assert.AreEqual(3f, push.X, 1e-4f);
            Assert.AreEqual(4f, push.Z, 1e-4f);
        }

        [Test]
        public void 左にいる相手は左へ飛ぶ()
        {
            Push push = KnockbackRule.Velocity(0f, 0f, -2f, 0f, 3f, Right);

            Assert.AreEqual(-3f, push.X, 1e-4f);
            Assert.AreEqual(0f, push.Z, 1e-4f);
        }

        [Test]
        public void 強さの一部ぶん浮く()
        {
            Push push = KnockbackRule.Velocity(0f, 0f, 1f, 0f, 3f, Right);

            Assert.AreEqual(3f * KnockbackRule.LiftRatio, push.Y, 1e-4f);
        }

        [Test]
        public void 重なっていたら当てた側の向きへ飛ぶ()
        {
            Assert.AreEqual(2f, KnockbackRule.Velocity(1f, 1f, 1f, 1f, 2f, Right).X, 1e-4f);
            Assert.AreEqual(-2f, KnockbackRule.Velocity(1f, 1f, 1f, 1f, 2f, Left).X, 1e-4f);
        }

        [Test]
        public void 強さが0なら飛ばない()
        {
            Push push = KnockbackRule.Velocity(0f, 0f, 1f, 0f, 0f, Right);

            Assert.AreEqual(0f, push.X);
            Assert.AreEqual(0f, push.Y);
            Assert.AreEqual(0f, push.Z);
        }
    }
}
