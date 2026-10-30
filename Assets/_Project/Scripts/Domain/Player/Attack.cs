using System;

namespace MemorialFloor.Domain
{
    /// <summary>攻撃の種類</summary>
    public enum AttackKind
    {
        /// <summary>突き。体の向きと同じ側へ強く倒したとき</summary>
        Thrust,
        /// <summary>ケサ。どちらへも倒れていないとき</summary>
        Slash,
        /// <summary>ステップ。体の向きと逆、または上下だけ倒したとき</summary>
        Step
    }

    /// <summary>攻撃スティックの傾きから何が出るかを決める</summary>
    public static class AttackRule
    {
        /// <summary>倒したとみなす傾き</summary>
        public const float Threshold = 0.5f;

        /// <summary>facing は体の向き。正なら右、負なら左を向いている</summary>
        public static AttackKind Choose(float horizontal, float vertical, float facing)
        {
            if (Math.Abs(horizontal) > Threshold && horizontal * facing > 0f) return AttackKind.Thrust;
            if (Math.Abs(horizontal) < Threshold && Math.Abs(vertical) < Threshold) return AttackKind.Slash;

            return AttackKind.Step;
        }

        /// <summary>突きの上下。どちらにも倒れていなければ 0 で、上下の振りは出ない</summary>
        public static int ThrustDirection(float vertical)
        {
            if (vertical > Threshold) return 1;
            if (vertical < -Threshold) return -1;

            return 0;
        }
    }

    /// <summary>
    /// 攻撃の時間割。振りかぶり、当たる、戻りの順に進む。
    /// 当たる時間は、振りのアニメの打点（振り始めから約 0.32 秒）を挟む
    /// </summary>
    public static class AttackTiming
    {
        /// <summary>振り始めから当たり始めるまで</summary>
        public const float WindupSeconds = 0.2f;

        /// <summary>当たる長さ</summary>
        public const float ActiveSeconds = 0.25f;

        /// <summary>突きとケサの全体。これが過ぎるまで次は出ない</summary>
        public const float SwingSeconds = 0.7f;

        /// <summary>ステップの全体</summary>
        public const float StepSeconds = 0.8f;

        /// <summary>終わる前のこの長さに入った入力は捨てずに預かり、終わってすぐ出す</summary>
        public const float BufferSeconds = 0.25f;

        /// <summary>duration の攻撃が elapsed 経ったところで来た入力を、次の攻撃として預かるか</summary>
        public static bool IsBuffered(float elapsed, float duration)
        {
            return elapsed >= duration - BufferSeconds;
        }
    }

    /// <summary>当てた相手を飛ばす速さ</summary>
    public readonly struct Push
    {
        public Push(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        /// <summary>浮かせる上向きの成分</summary>
        public float Y { get; }

        public float Z { get; }
    }

    /// <summary>吹き飛ばし。当てた側から相手へ向かう水平の向きに飛ばし、少し浮かせる</summary>
    public static class KnockbackRule
    {
        /// <summary>浮かせる強さ。水平の強さに対する割合</summary>
        public const float LiftRatio = 1f / 3f;

        /// <summary>これより近いと向きが決まらない</summary>
        private const float MinDistance = 0.0001f;

        /// <summary>
        /// strength の強さで飛ばす速さ。重なっていて向きが決まらないときは、当てた側の向きへ飛ばす。
        /// facing は当てた側の向き。正なら右、負なら左
        /// </summary>
        public static Push Velocity(float fromX, float fromZ, float toX, float toZ, float strength, float facing)
        {
            if (strength <= 0f) return new Push(0f, 0f, 0f);

            float dx = toX - fromX;
            float dz = toZ - fromZ;
            float length = (float)Math.Sqrt(dx * dx + dz * dz);

            if (length < MinDistance)
            {
                dx = facing < 0f ? -1f : 1f;
                dz = 0f;
                length = 1f;
            }

            return new Push(dx / length * strength, strength * LiftRatio, dz / length * strength);
        }
    }
}
