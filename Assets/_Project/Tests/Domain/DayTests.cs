using MemorialFloor.Domain;
using NUnit.Framework;

namespace MemorialFloor.Domain.Tests
{
    public class DayClockTests
    {
        // 1日120秒 = 1時間あたり実5秒。Sun2 の既定値
        private const float Around = 120f;

        [Test]
        public void 時刻は経過秒と1日の長さから決まる()
        {
            Assert.AreEqual(0f, DayClock.HourAt(0, Around), 0.0001f);
            Assert.AreEqual(5f, DayClock.HourAt(25, Around), 0.0001f);
            Assert.AreEqual(12f, DayClock.HourAt(60, Around), 0.0001f);
            Assert.AreEqual(24f, DayClock.HourAt(120, Around), 0.0001f);
        }

        [Test]
        public void 長さが0以下でも落ちない()
        {
            Assert.AreEqual(0f, DayClock.HourAt(60, 0f), 0.0001f);
            Assert.AreEqual(0f, DayClock.HourAt(60, -1f), 0.0001f);
        }

        [Test]
        public void 零時は1日の長さで判定する()
        {
            Assert.IsFalse(DayClock.IsDayOver(119, Around));
            Assert.IsTrue(DayClock.IsDayOver(120, Around));
            Assert.IsTrue(DayClock.IsDayOver(200, Around));
        }

        [Test]
        public void 境界を跨ぐのは1日に1度だけ()
        {
            int crossed = 0;
            for (int t = 0; t < 120; t++)
            {
                if (DayClock.Entered(DayClock.MorningHour, t, t + 1, Around)) crossed++;
            }

            Assert.AreEqual(1, crossed);
        }

        [Test]
        public void 跨ぐのは境界を越えた刻みだけ()
        {
            Assert.IsTrue(DayClock.Entered(DayClock.MorningHour, 24, 25, Around));
            Assert.IsFalse(DayClock.Entered(DayClock.MorningHour, 25, 26, Around));
            Assert.IsFalse(DayClock.Entered(DayClock.MorningHour, 23, 24, Around));
        }

        [Test]
        public void 明るさは夜明けと日没で線形に動く()
        {
            const float min = 0.3f;
            const float max = 1.8f;

            Assert.AreEqual(min, DayClock.LightIntensity(0f, min, max), 0.0001f);
            Assert.AreEqual(min, DayClock.LightIntensity(4f, min, max), 0.0001f);
            Assert.AreEqual(1.05f, DayClock.LightIntensity(4.5f, min, max), 0.0001f);
            Assert.AreEqual(max, DayClock.LightIntensity(5f, min, max), 0.0001f);
            Assert.AreEqual(max, DayClock.LightIntensity(16f, min, max), 0.0001f);
            Assert.AreEqual(1.05f, DayClock.LightIntensity(16.5f, min, max), 0.0001f);
            Assert.AreEqual(min, DayClock.LightIntensity(17f, min, max), 0.0001f);
            Assert.AreEqual(min, DayClock.LightIntensity(23f, min, max), 0.0001f);
        }
    }

    public class DayCycleTests
    {
        private const float Around = 120f;

        [Test]
        public void 進めると1秒増える()
        {
            DayCycle cycle = new DayCycle();

            cycle.Advance();
            Assert.AreEqual(1, cycle.ElapsedSeconds);
            Assert.AreEqual(0, cycle.PreviousSeconds);
        }

        [Test]
        public void 零時に戻すと0になる()
        {
            DayCycle cycle = new DayCycle();
            cycle.SetElapsed(119);

            cycle.Advance();
            Assert.IsTrue(cycle.IsDayOver(Around));

            cycle.Reset();
            Assert.AreEqual(0, cycle.ElapsedSeconds);
            Assert.IsFalse(cycle.IsDayOver(Around));
        }

        [Test]
        public void 読み込みで負の値は0になる()
        {
            DayCycle cycle = new DayCycle();

            cycle.SetElapsed(-5);
            Assert.AreEqual(0, cycle.ElapsedSeconds);

            cycle.SetElapsed(42);
            Assert.AreEqual(42, cycle.ElapsedSeconds);
        }

        [Test]
        public void 丸1日で跨ぐのは1度だけ()
        {
            DayCycle cycle = new DayCycle();
            int crossed = 0;

            for (int t = 0; t < 120; t++)
            {
                cycle.Advance();
                if (cycle.Entered(DayClock.MorningHour, Around)) crossed++;
            }

            Assert.AreEqual(1, crossed);
        }
    }

    public class DayPlanTests
    {
        [Test]
        public void 初回は保留し次で発行する()
        {
            DayPlan plan = new DayPlan();

            Assert.IsFalse(plan.ShouldIssue("Root1"), "1回目で発行してはいけない");
            Assert.AreEqual(1, plan.Count);

            Assert.IsTrue(plan.ShouldIssue("Root1"), "2回目で発行する");
            Assert.AreEqual(0, plan.Count, "発行したら予約は消える");
        }

        [Test]
        public void 発行したあとはまた保留から始まる()
        {
            DayPlan plan = new DayPlan();
            plan.ShouldIssue("Root1");
            plan.ShouldIssue("Root1");

            Assert.IsFalse(plan.ShouldIssue("Root1"));
        }

        [Test]
        public void 根源ごとに独立している()
        {
            DayPlan plan = new DayPlan();

            Assert.IsFalse(plan.ShouldIssue("Root1"));
            Assert.IsFalse(plan.ShouldIssue("Root2"));
            Assert.AreEqual(2, plan.Count);

            Assert.IsTrue(plan.ShouldIssue("Root1"));
            Assert.AreEqual(1, plan.Count);
            Assert.AreEqual("Root2", plan.Pending[0]);
        }

        [Test]
        public void 空の_Id_は発行しない()
        {
            DayPlan plan = new DayPlan();

            Assert.IsFalse(plan.ShouldIssue(""));
            Assert.IsFalse(plan.ShouldIssue(null));
            Assert.AreEqual(0, plan.Count, "予約もしない");
        }

        [Test]
        public void 捨てると空になる()
        {
            DayPlan plan = new DayPlan();
            plan.ShouldIssue("Root1");
            plan.ShouldIssue("Root2");

            plan.Clear();
            Assert.AreEqual(0, plan.Count);
        }
    }
}
