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
}
