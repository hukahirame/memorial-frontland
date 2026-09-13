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

    /// <summary>敵の行動選択。待機が多く、移動と跳躍が同じだけ続く</summary>
    public static class ActionRule
    {
        public const int WaitPercent = 60;
        public const int MovePercent = 20;

        /// <summary>待機を続ける秒数の下限と上限</summary>
        public const float WaitSecondsMin = 1.5f;
        public const float WaitSecondsMax = 2f;

        /// <summary>移動を続ける秒数の下限と上限</summary>
        public const float MoveSecondsMin = 0.5f;
        public const float MoveSecondsMax = 2.5f;

        /// <summary>跳躍のあと選び直すまでの秒数。着地を待つので幅を持たせない</summary>
        public const float JumpSeconds = 1.2f;

        /// <summary>残りは跳躍</summary>
        public static EnemyAction Choose(IRandom random)
        {
            int roll = random.Next(0, 100);

            if (roll < WaitPercent) return EnemyAction.Wait;
            if (roll < WaitPercent + MovePercent) return EnemyAction.Move;

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

        /// <summary>この攻略度から、湧いた敵が弱くなる</summary>
        public const int WeakenProgress = 30;

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
