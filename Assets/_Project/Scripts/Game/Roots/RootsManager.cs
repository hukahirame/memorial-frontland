using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 根源の持ち主。根源の並びと、拠点に攻め込んだ敵の控えを持ち、零時ごとに根源を動かす（蓄積・氾濫・攻略度の減り）。
    /// 常駐シーンに 1 つ置く。知らせの文字は出さず、起きたことをイベントで伝える（RootNotices が出す）
    /// </summary>
    public sealed class RootsManager : MonoBehaviour
    {
        [SerializeField] private Sun _sun;

        public RootRegistry Roots { get; } = new RootRegistry();

        /// <summary>拠点に攻め込んだ敵の控え。氾濫で足し、拠点の MS_Spawner が出す</summary>
        public BaseRaid Raid { get; } = new BaseRaid();

        /// <summary>根源が氾濫して、拠点に敵が攻め込んだ。同じ根源の Overflowed より先に知らせる</summary>
        public event Action<Root> Invaded;

        /// <summary>
        /// 根源が氾濫した。決壊はこれを受けて出る。Invaded と分けて後に知らせるのは、
        /// 攻め込まれた知らせを決壊が出た知らせより先に出すため（同じイベントの受け手どうしは順番が決まらない）
        /// </summary>
        public event Action<Root> Overflowed;

        /// <summary>根源が次の零時に氾濫しそう。零時の終わりに、攻略度を減らしたあとで知らせる</summary>
        public event Action<Root> OverflowForeseen;

        /// <summary>根源の核が壊れ、敵が大量に湧いた</summary>
        public event Action Stampeded;

        /// <summary>拠点を読み込み、控えていた攻め込んだ敵を出した。数は出した敵の数</summary>
        public event Action<int> RaidersReleased;

        //始めのクエストと保存の戻しが根源を引くので、どの Start よりも先に作る
        private void Awake()
        {
            Create("はじまりの根源", "01010101", "Root1", -80f, -300f, 1000);
            Create("水の根源", "02020202", "Root2", 420f, -300f, 2000);
            Create("沈んだ街の根源", "03030303", "Root3", -420f, 60f, 3000, 2, 5);
        }

        private void OnEnable()
        {
            _sun.Midnight += PassMidnight;
        }

        private void OnDisable()
        {
            _sun.Midnight -= PassMidnight;
        }

        /// <summary>根源の Id を作った順に。場所は拠点のあとにこの順で一列に並ぶ</summary>
        public IReadOnlyList<string> RootIds()
        {
            List<string> ids = new List<string>();
            foreach (Root root in Roots.All) ids.Add(root.Id);

            return ids;
        }

        /// <summary>
        /// 零時。蓄積を増やし、溢れた根源は拠点に敵を攻め込ませる。そのあと攻略度を減らし、次の零時に溢れそうな根源を知らせる。
        /// どこに居ても拠点に攻め込ませる。拠点に居れば MS_Spawner がすぐ、留守なら戻ったときに出す
        /// </summary>
        public void PassMidnight()
        {
            //氾濫判定は蓄積値の増加直後、攻略度の減少より前に行う
            foreach (Root root in Roots.All)
            {
                root.AccumulateDaily(UnityRandom.Shared);

                int raiders = root.Overflow();
                if (raiders == 0) continue;

                Raid.Invade(raiders);
                Invaded?.Invoke(root);
                Overflowed?.Invoke(root);
            }

            foreach (Root root in Roots.All) root.DecayProgressDaily(UnityRandom.Shared);

            //予兆は攻略度を減らしたあとで見る。次の零時に増える量は、そのときの攻略度で決まる
            foreach (Root root in Roots.All)
            {
                if (root.MayOverflowNextDay) OverflowForeseen?.Invoke(root);
            }
        }

        /// <summary>根源の核が壊れて敵が大量に湧いた。場所のシーンに居る核（RootCore）が呼ぶ</summary>
        public void ReportStampede()
        {
            Stampeded?.Invoke();
        }

        /// <summary>拠点を読み込み、控えていた敵を出した。拠点のシーンに居る湧き口（RaidGate）が呼ぶ</summary>
        public void ReportRaidersReleased(int count)
        {
            RaidersReleased?.Invoke(count);
        }

        /// <summary>gainLow から gainHigh は、攻略度 0 のときに日ごとに増える蓄積値の幅。既に同じ Id があれば何もしない</summary>
        private void Create(string name, string seed, string id, float mapX, float mapY, int danger,
            int gainLow = Root.DailyGainLow, int gainHigh = Root.DailyGainHigh)
        {
            Roots.TryAdd(new Root(id, name, seed, danger, mapX, mapY, gainLow, gainHigh));
        }
    }
}
