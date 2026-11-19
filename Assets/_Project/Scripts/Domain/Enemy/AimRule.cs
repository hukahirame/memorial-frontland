using System;

namespace MemorialFloor.Domain
{
    /// <summary>敵が動くときに目指すもの</summary>
    public enum EnemyAim
    {
        /// <summary>向きを乱数で決める</summary>
        Wander,

        /// <summary>見つけたプレイヤーへ寄っていく</summary>
        Player,

        /// <summary>うろつきながら行き先へ寄っていく</summary>
        Goal
    }

    /// <summary>
    /// 敵が動く向き。見えるところにプレイヤーが居れば寄っていく。
    /// 行き先を持つ敵は、うろつく向きを行き先の側へ傾ける
    /// </summary>
    public static class AimRule
    {
        /// <summary>この距離までプレイヤーが近づくと見つける。離れれば見失う。壁は見ない</summary>
        public const float SightRange = 4f;

        /// <summary>上下にこれより離れたプレイヤーは見つけない。2 階建ての場所で、ほかの階のプレイヤーを追わない</summary>
        public const float SightHeight = 2f;

        /// <summary>行き先からこの距離の内に入ったら、もう寄らずにうろつく。狭いと一か所に固まる</summary>
        public const float GoalRadius = 6f;

        /// <summary>足し合わせた長さの 2 乗がこれ未満なら、打ち消し合ったとみなす</summary>
        private const float CancelledSquared = 1e-6f;

        /// <summary>プレイヤーを見つけていれば寄る。でなければ、行き先が遠いときだけ行き先へ寄る</summary>
        public static EnemyAim Choose(float toPlayer, bool hasGoal, float toGoal)
        {
            if (toPlayer <= SightRange) return EnemyAim.Player;
            if (hasGoal && toGoal > GoalRadius) return EnemyAim.Goal;

            return EnemyAim.Wander;
        }

        /// <summary>
        /// 見つけるかを決めるときの、プレイヤーまでの距離。flat は水平な距離、rise は上下の差。
        /// 上下に離れすぎていればほかの階に居るので、どれだけ近くても見つけない遠さにする
        /// </summary>
        public static float SightDistance(float flat, float rise)
        {
            return Math.Abs(rise) > SightHeight ? float.PositiveInfinity : flat;
        }

        /// <summary>差の向き。差が無ければ止まる</summary>
        public static Heading Toward(float x, float z)
        {
            float length = (float)Math.Sqrt(x * x + z * z);
            if (length == 0f) return new Heading(0f, 0f);

            return new Heading(x / length, z / length);
        }

        /// <summary>
        /// うろつく向きを行き先の側へ傾ける。足し合わせて向きだけを取るので、
        /// 行き先から 90 度より外れはしないが、まっすぐにも向かわない
        /// </summary>
        public static Heading Lean(Heading wander, float towardX, float towardZ)
        {
            Heading goal = Toward(towardX, towardZ);
            float x = wander.X + goal.X;
            float z = wander.Z + goal.Z;

            //真逆を向いていると打ち消し合い、残った誤差の向きへ逸れる。そのときは行き先へまっすぐ
            if (x * x + z * z < CancelledSquared) return goal;

            return Toward(x, z);
        }
    }
}
