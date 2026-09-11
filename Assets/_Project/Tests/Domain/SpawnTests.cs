using System.Collections.Generic;
using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    /// <summary>決めた順に値を返す乱数。引かれた回数も数える</summary>
    internal sealed class FakeRandom : IRandom
    {
        private readonly Queue<int> _values;
        private readonly Queue<float> _floats = new Queue<float>();

        public int Draws { get; private set; }

        public FakeRandom(params int[] values)
        {
            _values = new Queue<int>(values);
        }

        /// <summary>小数を引いたときに返す値を積む</summary>
        public FakeRandom WithFloats(params float[] values)
        {
            foreach (float value in values) _floats.Enqueue(value);

            return this;
        }

        public int Next(int minInclusive, int maxExclusive)
        {
            Draws++;

            return _values.Count > 0 ? _values.Dequeue() : minInclusive;
        }

        public float NextFloat(float min, float max)
        {
            Draws++;

            return _floats.Count > 0 ? _floats.Dequeue() : min;
        }
    }

    public class ChanceTests
    {
        [Test]
        public void 割合が0以下なら常に偽で乱数を引かない()
        {
            FakeRandom random = new FakeRandom(0);

            Assert.IsFalse(Chance.Roll(0, random));
            Assert.IsFalse(Chance.Roll(-5, random));
            Assert.AreEqual(0, random.Draws);
        }

        [Test]
        public void 割合が100以上なら常に真で乱数を引かない()
        {
            FakeRandom random = new FakeRandom(99);

            Assert.IsTrue(Chance.Roll(100, random));
            Assert.IsTrue(Chance.Roll(150, random));
            Assert.AreEqual(0, random.Draws);
        }

        [Test]
        public void 割合未満で真になる()
        {
            Assert.IsTrue(Chance.Roll(10, new FakeRandom(9)));
            Assert.IsFalse(Chance.Roll(10, new FakeRandom(10)), "境界は含まない");
        }
    }

    public class ActionRuleTests
    {
        [Test]
        public void 割合どおりに行動が分かれる()
        {
            Assert.AreEqual(EnemyAction.Wait, ActionRule.Choose(new FakeRandom(0)));
            Assert.AreEqual(EnemyAction.Wait, ActionRule.Choose(new FakeRandom(59)));
            Assert.AreEqual(EnemyAction.Move, ActionRule.Choose(new FakeRandom(60)));
            Assert.AreEqual(EnemyAction.Move, ActionRule.Choose(new FakeRandom(79)));
            Assert.AreEqual(EnemyAction.Jump, ActionRule.Choose(new FakeRandom(80)));
            Assert.AreEqual(EnemyAction.Jump, ActionRule.Choose(new FakeRandom(99)));
        }

        [Test]
        public void 三つの割合が100になる()
        {
            int wait = 0, move = 0, jump = 0;
            for (int roll = 0; roll < 100; roll++)
            {
                switch (ActionRule.Choose(new FakeRandom(roll)))
                {
                    case EnemyAction.Wait: wait++; break;
                    case EnemyAction.Move: move++; break;
                    default: jump++; break;
                }
            }

            Assert.AreEqual(60, wait);
            Assert.AreEqual(20, move);
            Assert.AreEqual(20, jump);
        }
    }

    public class SpawnRuleTests
    {
        private static Root Progressed(int progress)
        {
            Root root = new Root("Root1", "根源", "seed", 0, 0f, 0f);
            root.Gain(progress);

            return root;
        }

        [Test]
        public void 攻略度が足りなければ抑制されない()
        {
            FakeRandom random = new FakeRandom(99);

            Assert.IsFalse(SpawnRule.ShouldSuppress(Progressed(49), 10, random));
            Assert.AreEqual(1, random.Draws, "攻略度の判定は引かず、混雑の判定だけが引かれる");
        }

        [Test]
        public void 誰も居なければ乱数を引かない()
        {
            FakeRandom random = new FakeRandom(0);

            Assert.IsFalse(SpawnRule.ShouldSuppress(Progressed(49), 0, random));
            Assert.AreEqual(0, random.Draws);
        }

        [Test]
        public void 攻略度が足りると半分の確率で見送る()
        {
            Assert.IsTrue(SpawnRule.ShouldSuppress(Progressed(50), 0, new FakeRandom(51)));
            Assert.IsFalse(SpawnRule.ShouldSuppress(Progressed(50), 0, new FakeRandom(50, 99)),
                           "50 ちょうどは見送らない");
        }

        [Test]
        public void 抑制が決まったら以降の乱数を引かない()
        {
            FakeRandom random = new FakeRandom(51);

            Assert.IsTrue(SpawnRule.ShouldSuppress(Progressed(50), 30, random));
            Assert.AreEqual(1, random.Draws);
        }

        [Test]
        public void 混雑しているほど見送りやすい()
        {
            // 10 体いれば 30%
            Assert.IsTrue(SpawnRule.ShouldSuppress(null, 10, new FakeRandom(29)));
            Assert.IsFalse(SpawnRule.ShouldSuppress(null, 10, new FakeRandom(30)));
        }

        [Test]
        public void 根源が無いシーンでは攻略度の抑制が掛からない()
        {
            FakeRandom random = new FakeRandom(99);

            Assert.IsFalse(SpawnRule.ShouldSuppress(null, 10, random));
            Assert.AreEqual(1, random.Draws, "混雑の判定だけが引かれる");
        }

        [Test]
        public void 攻略度30から弱くなる()
        {
            Assert.IsFalse(SpawnRule.ShouldWeaken(Progressed(29)));
            Assert.IsTrue(SpawnRule.ShouldWeaken(Progressed(30)));
            Assert.IsFalse(SpawnRule.ShouldWeaken(null));
        }
    }

    public class ActionDurationTests
    {
        [Test]
        public void 待機と移動は秒数を引く()
        {
            FakeRandom random = new FakeRandom().WithFloats(1.7f, 0.9f);

            Assert.AreEqual(1.7f, ActionRule.Duration(EnemyAction.Wait, random));
            Assert.AreEqual(0.9f, ActionRule.Duration(EnemyAction.Move, random));
            Assert.AreEqual(2, random.Draws);
        }

        [Test]
        public void 跳躍は乱数を引かず固定の秒数()
        {
            FakeRandom random = new FakeRandom();

            Assert.AreEqual(ActionRule.JumpSeconds, ActionRule.Duration(EnemyAction.Jump, random));
            Assert.AreEqual(0, random.Draws);
        }

        [Test]
        public void 行動ごとに決まった範囲で引く()
        {
            Assert.AreEqual(ActionRule.WaitSecondsMin, ActionRule.Duration(EnemyAction.Wait, new FakeRandom()));
            Assert.AreEqual(ActionRule.MoveSecondsMin, ActionRule.Duration(EnemyAction.Move, new FakeRandom()));
        }

        [Test]
        public void 待機のほうが移動より長く止まる()
        {
            Assert.Greater(ActionRule.WaitSecondsMin, ActionRule.MoveSecondsMin);
            Assert.Less(ActionRule.WaitSecondsMax, ActionRule.MoveSecondsMax);
        }
    }

    public class FallRuleTests
    {
        [Test]
        public void 閾値より下へ落ちたら場外()
        {
            Assert.IsTrue(FallRule.IsOutOfField(FallRule.OutOfFieldY - 0.1f));
        }

        [Test]
        public void 閾値そのものは場外ではない()
        {
            Assert.IsFalse(FallRule.IsOutOfField(FallRule.OutOfFieldY));
            Assert.IsFalse(FallRule.IsOutOfField(0f));
        }
    }
}
