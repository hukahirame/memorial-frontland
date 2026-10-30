using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class MoveRuleTests
    {
        [Test]
        public void 遊びより小さく倒しても動かない()
        {
            Assert.IsTrue(MoveRule.Direction(0f, 0f).IsStill);
            Assert.IsTrue(MoveRule.Direction(0.1f, 0.1f).IsStill);
            Assert.IsTrue(MoveRule.Direction(-0.15f, 0f).IsStill);
        }

        [Test]
        public void 遊びを越えれば倒し具合によらず全速で進む()
        {
            Heading slight = MoveRule.Direction(0.3f, 0f);
            Heading full = MoveRule.Direction(1f, 0f);

            Assert.AreEqual(1f, slight.X, 1e-4f);
            Assert.AreEqual(full.X, slight.X, 1e-4f);
        }

        [Test]
        public void 斜めでも速さは変わらない()
        {
            Heading diagonal = MoveRule.Direction(1f, 1f);

            float length = (float)System.Math.Sqrt(diagonal.X * diagonal.X + diagonal.Z * diagonal.Z);
            Assert.AreEqual(1f, length, 1e-4f);
        }

        [Test]
        public void 遊びちょうどは動く()
        {
            Assert.IsFalse(MoveRule.Direction(MoveRule.DeadZone, 0f).IsStill);
        }
    }
}
