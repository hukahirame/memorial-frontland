using System;

namespace MemorialFloor.Domain
{
    /// <summary>敵を湧かせるかどうかの規則</summary>
    public static class SpawnRule
    {
        /// <summary>この攻略度から湧きが抑えられる。【抑制】の段</summary>
        public const int SuppressProgress = Root.SuppressProgress;

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
