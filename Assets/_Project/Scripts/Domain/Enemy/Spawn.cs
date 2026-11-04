using System;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 乱数の口。UnityEngine.Random を直に呼ぶと規則を試験できないため、
    /// 差し替えられる形にする。
    /// </summary>
    public interface IRandom
    {
        /// <summary>min 以上 max 未満の整数</summary>
        int Next(int minInclusive, int maxExclusive);

        /// <summary>min 以上 max 以下の小数。整数側と違い上限を含む</summary>
        float NextFloat(float min, float max);
    }

    /// <summary>確率の判定</summary>
    public static class Chance
    {
        /// <summary>percent% で真。0 以下は常に偽、100 以上は常に真</summary>
        public static bool Roll(int percent, IRandom random)
        {
            if (percent <= 0) return false;
            if (percent >= 100) return true;

            return random.Next(0, 100) < percent;
        }
    }

    /// <summary>敵が次にとる行動</summary>
    public enum EnemyAction
    {
        Wait,
        Move,
        Jump
    }

    /// <summary>敵の行動選択。待機が多く、移動と跳躍が同じだけ続く。プレイヤーを見つけているあいだは待機が減る</summary>
    public static class ActionRule
    {
        public const int WaitPercent = 60;
        public const int MovePercent = 20;

        /// <summary>プレイヤーを見つけているあいだの割合。待機を減らして寄っていく</summary>
        public const int ChaseWaitPercent = 30;
        public const int ChaseMovePercent = 40;

        /// <summary>待機を続ける秒数の下限と上限</summary>
        public const float WaitSecondsMin = 1.5f;
        public const float WaitSecondsMax = 2f;

        /// <summary>移動を続ける秒数の下限と上限</summary>
        public const float MoveSecondsMin = 0.5f;
        public const float MoveSecondsMax = 2.5f;

        /// <summary>跳躍のあと選び直すまでの秒数。着地を待つので幅を持たせない</summary>
        public const float JumpSeconds = 1.2f;

        /// <summary>攻撃を受けてから自分で動き出すまでの秒数。この間は吹き飛ばしに任せる</summary>
        public const float StaggerSeconds = 0.4f;

        /// <summary>残りは跳躍。chasing はプレイヤーを見つけているか</summary>
        public static EnemyAction Choose(IRandom random, bool chasing)
        {
            int wait = chasing ? ChaseWaitPercent : WaitPercent;
            int move = chasing ? ChaseMovePercent : MovePercent;
            int roll = random.Next(0, 100);

            if (roll < wait) return EnemyAction.Wait;
            if (roll < wait + move) return EnemyAction.Move;

            return EnemyAction.Jump;
        }

        /// <summary>選んだ行動を続ける秒数</summary>
        public static float Duration(EnemyAction action, IRandom random)
        {
            if (action == EnemyAction.Wait) return random.NextFloat(WaitSecondsMin, WaitSecondsMax);
            if (action == EnemyAction.Move) return random.NextFloat(MoveSecondsMin, MoveSecondsMax);

            return JumpSeconds;
        }
    }

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

    /// <summary>場外へ落ちたかどうか</summary>
    public static class FallRule
    {
        /// <summary>この高さより下へ落ちたら戻れない</summary>
        public const float OutOfFieldY = -10f;

        public static bool IsOutOfField(float y)
        {
            return y < OutOfFieldY;
        }
    }

    /// <summary>敵を湧かせるかどうかの規則</summary>
    public static class SpawnRule
    {
        /// <summary>この攻略度から湧きが抑えられる</summary>
        public const int SuppressProgress = 50;

        /// <summary>攻略度が足りているときに見送る確率</summary>
        public const int SuppressPercent = 50;

        /// <summary>既に居る1体あたり、見送る確率に足す量</summary>
        public const int CrowdPercentPerEnemy = 3;

        /// <summary>この攻略度から、湧いた敵が弱くなる。【解明】の段</summary>
        public const int WeakenProgress = Root.RevealProgress;

        /// <summary>湧いた敵の体力を減らす割合</summary>
        public const float WeakenRatio = 0.2f;

        /// <summary>敵を1体倒したときに根源の蓄積値が下がる量</summary>
        public const int CalmPerDefeat = 3;

        /// <summary>
        /// 湧きを見送るか。根源が無いシーンでは攻略度による抑制は掛からない。
        /// 抑制が決まった時点で以降の判定は引かない
        /// </summary>
        public static bool ShouldSuppress(Root root, int aliveCount, IRandom random)
        {
            if (root != null && root.Progress >= SuppressProgress
                && random.Next(0, 100) > SuppressPercent) return true;

            return Chance.Roll(CrowdPercentPerEnemy * aliveCount, random);
        }

        /// <summary>湧いた敵を弱くするか</summary>
        public static bool ShouldWeaken(Root root)
        {
            return root != null && root.Progress >= WeakenProgress;
        }
    }
}
