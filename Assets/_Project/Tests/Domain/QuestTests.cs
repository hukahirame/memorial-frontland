using System.Linq;
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

    public class QuestRegistryTests
    {
        private static readonly Reward[] NoReward = new Reward[0];

        [Test]
        public void 採番は種別の文字と連番になる()
        {
            QuestRegistry registry = new QuestRegistry();

            Assert.AreEqual("X0", registry.Create(QuestKind.Main, "Root1", "MainSpawner", 1, NoReward).Id);
            Assert.AreEqual("X1", registry.Create(QuestKind.Main, "Root2", "MainSpawner", 1, NoReward).Id);
            Assert.AreEqual("S0", registry.Create(QuestKind.Sub, "Root1", "Slime", 3, NoReward).Id);
        }

        [Test]
        public void 消したあとに作ると空き番号が埋まる()
        {
            // Legacy は「その種別の現在数」を番号にしていたため、X0 を消したあとに
            // 作るとまた X0 になり、既にある X1 と衝突する順序があった
            QuestRegistry registry = new QuestRegistry();
            registry.Create(QuestKind.Main, "Root1", "MainSpawner", 1, NoReward);
            registry.Create(QuestKind.Main, "Root2", "MainSpawner", 1, NoReward);

            Assert.IsTrue(registry.Remove("X0"));

            Assert.AreEqual("X0", registry.Create(QuestKind.Main, "Root3", "MainSpawner", 1, NoReward).Id);
            Assert.AreEqual(1, registry.All.Count(q => q.Id == "X0"), "Id が重複した");
        }

        [Test]
        public void 報酬はクエストと一緒に消える()
        {
            QuestRegistry registry = new QuestRegistry();
            registry.Create(QuestKind.Main, "Root1", "MainSpawner", 1,
                            new[] { new Reward("coin", 100), new Reward("progress", 15) });

            Assert.AreEqual(2, registry.Find("X0").Rewards.Count);

            registry.Remove("X0");
            Assert.IsNull(registry.Find("X0"));
            Assert.AreEqual(0, registry.Count);
        }

        [Test]
        public void 根源ごとに調査か決壊があるかを見る()
        {
            QuestRegistry registry = new QuestRegistry();
            registry.Create(QuestKind.Sub, "Root1", "Slime", 3, NoReward);

            Assert.IsFalse(registry.HasMainFor("Root1"));

            registry.Create(QuestKind.Breach, "Root1", "MainSpawner", 1, NoReward);
            Assert.IsTrue(registry.HasMainFor("Root1"));
            Assert.IsFalse(registry.HasMainFor("Root2"));
        }

        [Test]
        public void 進捗は目標で頭打ちになる()
        {
            QuestRegistry registry = new QuestRegistry();
            Quest quest = registry.Create(QuestKind.Sub, "Root1", "Slime", 2, NoReward);

            quest.Advance();
            Assert.IsFalse(quest.IsComplete);

            quest.Advance();
            Assert.IsTrue(quest.IsComplete);

            quest.Advance();
            Assert.AreEqual(2, quest.Progress);
        }

        [Test]
        public void 種別は_Id_から決まる()
        {
            QuestRegistry registry = new QuestRegistry();

            Assert.AreEqual(QuestKind.Breach, registry.Create(QuestKind.Breach, "Root1", "MainSpawner", 1, NoReward).Kind);
            Assert.AreEqual(QuestKind.Common, registry.Create(QuestKind.Common, "Common", "Slime", 5, NoReward).Kind);
        }
    }
}
