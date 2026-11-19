using System;

namespace MemorialFloor.Domain
{
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
}
