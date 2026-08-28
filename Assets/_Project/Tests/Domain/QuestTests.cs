using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class QuestIdTests
    {
        [Test]
        public void 先頭の文字で種別が決まる()
        {
            Assert.IsTrue(QuestId.TryReadKind("X0", out var main));
            Assert.AreEqual(QuestKind.Main, main);

            Assert.IsTrue(QuestId.TryReadKind("Y12", out var breach));
            Assert.AreEqual(QuestKind.Breach, breach);

            Assert.IsTrue(QuestId.TryReadKind("S3", out var sub));
            Assert.AreEqual(QuestKind.Sub, sub);

            Assert.IsTrue(QuestId.TryReadKind("C0", out var common));
            Assert.AreEqual(QuestKind.Common, common);
        }

        [Test]
        public void 種別と文字は往復する()
        {
            foreach (QuestKind kind in System.Enum.GetValues(typeof(QuestKind)))
            {
                string id = QuestId.LetterOf(kind) + "7";

                Assert.IsTrue(QuestId.TryReadKind(id, out var read), id);
                Assert.AreEqual(kind, read, id);
            }
        }

        [Test]
        public void 知らない文字と空の文字列は読めない()
        {
            Assert.IsFalse(QuestId.TryReadKind("Z0", out _));
            Assert.IsFalse(QuestId.TryReadKind("", out _));
            Assert.IsFalse(QuestId.TryReadKind(null, out _));
            Assert.IsFalse(QuestId.TryReadKind("0X", out _));
        }

        [Test]
        public void 種別の判定は先頭だけを見る()
        {
            // Legacy は部分一致で見ていた。連番が数字である限り同じ答えになるが、
            // 規則としては先頭だけが正しい
            Assert.IsTrue(QuestId.Is("S3", QuestKind.Sub));
            Assert.IsFalse(QuestId.Is("X3", QuestKind.Sub));
            Assert.IsFalse(QuestId.Is("", QuestKind.Main));
        }
    }

    public class QuestProgressTests
    {
        [Test]
        public void 進めると1つ増える()
        {
            Assert.AreEqual(1, QuestProgress.Advance(0, 3));
            Assert.AreEqual(3, QuestProgress.Advance(2, 3));
        }

        [Test]
        public void 目標を超えて進まない()
        {
            Assert.AreEqual(3, QuestProgress.Advance(3, 3));
            Assert.AreEqual(3, QuestProgress.Advance(9, 3));
        }

        [Test]
        public void 達成は目標以上で判定する()
        {
            Assert.IsFalse(QuestProgress.IsComplete(2, 3));
            Assert.IsTrue(QuestProgress.IsComplete(3, 3));

            // 切り詰めを通っていない値でも達成と判定する。Legacy の UI 側は
            // 文字列の等値で見ていたため、ここが崩れると達成が消えていた
            Assert.IsTrue(QuestProgress.IsComplete(4, 3));
        }

        [Test]
        public void 切り詰めは負の値を0にする()
        {
            Assert.AreEqual(0, QuestProgress.Clamp(-1, 3));
            Assert.AreEqual(2, QuestProgress.Clamp(2, 3));
            Assert.AreEqual(3, QuestProgress.Clamp(5, 3));
        }
    }
}
