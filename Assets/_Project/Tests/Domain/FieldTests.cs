using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class FieldBoundsTests
    {
        /// <summary>x は -10..10、z は -5..5 の場</summary>
        private static FieldBounds Square()
        {
            return new FieldBounds(maxX: 10f, minX: -10f, maxZ: 5f, minZ: -5f);
        }

        [Test]
        public void 境界そのものは中に入る()
        {
            FieldBounds bounds = Square();

            Assert.IsTrue(bounds.Contains(10f, 5f));
            Assert.IsTrue(bounds.Contains(-10f, -5f));
            Assert.IsTrue(bounds.Contains(0f, 0f));
        }

        [Test]
        public void 一辺でも外なら中ではない()
        {
            FieldBounds bounds = Square();

            Assert.IsFalse(bounds.Contains(10.1f, 0f));
            Assert.IsFalse(bounds.Contains(0f, -5.1f));
        }

        [Test]
        public void 範囲の外は端まで丸める()
        {
            FieldBounds bounds = Square();

            Assert.AreEqual(10f, bounds.ClampX(999f));
            Assert.AreEqual(-10f, bounds.ClampX(-999f));
            Assert.AreEqual(3f, bounds.ClampX(3f));
            Assert.AreEqual(5f, bounds.ClampZ(7f));
            Assert.AreEqual(-5f, bounds.ClampZ(-7f));
        }

        [Test]
        public void 中に居るなら押し戻さない()
        {
            Assert.IsTrue(Square().PushBack(0f, 0f).None);
        }

        [Test]
        public void 外へ出た向きと逆へ押し戻す()
        {
            FieldBounds bounds = Square();

            Nudge right = bounds.PushBack(11f, 0f);
            Assert.AreEqual(-1f, right.X);
            Assert.AreEqual(0f, right.Z);

            Nudge left = bounds.PushBack(-11f, 0f);
            Assert.AreEqual(1f, left.X);
        }

        [Test]
        public void 二辺を同時に出たら両方へ押し戻す()
        {
            Nudge nudge = Square().PushBack(11f, -6f);

            Assert.AreEqual(-1f, nudge.X);
            Assert.AreEqual(1f, nudge.Z);
            Assert.IsFalse(nudge.None);
        }

        [Test]
        public void 猶予のうちは連れ戻さない()
        {
            FieldBounds bounds = Square();

            Assert.IsFalse(bounds.HasEscaped(10f + FieldBounds.EscapeMargin, 0f, 0f));
            Assert.IsTrue(bounds.HasEscaped(10f + FieldBounds.EscapeMargin + 0.01f, 0f, 0f));
        }

        [Test]
        public void 上下に離れすぎても連れ戻す()
        {
            FieldBounds bounds = Square();

            Assert.IsFalse(bounds.HasEscaped(0f, FieldBounds.EscapeHeight, 0f));
            Assert.IsTrue(bounds.HasEscaped(0f, FieldBounds.EscapeHeight + 0.01f, 0f));
            Assert.IsTrue(bounds.HasEscaped(0f, -FieldBounds.EscapeHeight - 0.01f, 0f));
        }
    }
}
