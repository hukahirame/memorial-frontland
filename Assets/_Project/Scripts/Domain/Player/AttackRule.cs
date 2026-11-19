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
        /// <summary>ステップ。体の向きと逆へ倒したとき</summary>
        Step,
        /// <summary>回避攻撃。上下だけ倒したとき</summary>
        Roll
    }

    /// <summary>攻撃の案内に並べる行動。突きは上下の振りが付くかで分ける</summary>
    public enum AttackMove
    {
        /// <summary>ケサ</summary>
        Slash,
        /// <summary>突き。上下の振りが付かない</summary>
        Thrust,
        /// <summary>斜め突き。上下にも倒した突きで、上下の振りが付く</summary>
        DiagonalThrust,
        /// <summary>ステップ</summary>
        Step,
        /// <summary>回避攻撃</summary>
        Roll,
        /// <summary>構え歩き。攻撃を構えたまま、体の向きを変えずに歩く。離すまで攻撃は出ない</summary>
        StanceWalk
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
            if (Math.Abs(horizontal) < Threshold && Math.Abs(vertical) > Threshold) return AttackKind.Roll;

            return AttackKind.Step;
        }

        /// <summary>突きの上下。どちらにも倒れていなければ 0 で、上下の振りは出ない</summary>
        public static int ThrustDirection(float vertical)
        {
            if (vertical > Threshold) return 1;
            if (vertical < -Threshold) return -1;

            return 0;
        }

        /// <summary>倒し方から出る行動。facing は Choose と同じ</summary>
        public static AttackMove MoveOf(float horizontal, float vertical, float facing)
        {
            switch (Choose(horizontal, vertical, facing))
            {
                case AttackKind.Thrust:
                    return ThrustDirection(vertical) != 0 ? AttackMove.DiagonalThrust : AttackMove.Thrust;
                case AttackKind.Slash:
                    return AttackMove.Slash;
                case AttackKind.Roll:
                    return AttackMove.Roll;
                default:
                    return AttackMove.Step;
            }
        }
    }
}
