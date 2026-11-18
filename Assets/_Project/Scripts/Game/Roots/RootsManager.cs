using System;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 根源の持ち主。根源の並びと、拠点に攻め込んだ敵の控えを持ち、零時ごとに根源を動かす（蓄積・氾濫・攻略度の減り）。
    /// 常駐シーンに 1 つ置く
    /// </summary>
    public sealed class RootsManager : MonoBehaviour
    {
        [SerializeField] private Sun _sun;
        [SerializeField] private MiddleText _middleText;

        public RootRegistry Roots { get; } = new RootRegistry();

        /// <summary>拠点に攻め込んだ敵の控え。氾濫で足し、拠点の MS_Spawner が出す</summary>
        public BaseRaid Raid { get; } = new BaseRaid();

        /// <summary>根源が氾濫した。氾濫の知らせを出した直後に知らせる</summary>
        public event Action<Root> Overflowed;

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
                _middleText.Midtxt_Anim(root.Name + "が氾濫し、拠点に敵が攻め込みました");
                Overflowed?.Invoke(root);
            }

            foreach (Root root in Roots.All) root.DecayProgressDaily(UnityRandom.Shared);

            //予兆は攻略度を減らしたあとで見る。次の零時に増える量は、そのときの攻略度で決まる
            foreach (Root root in Roots.All)
            {
                if (root.MayOverflowNextDay) _middleText.Midtxt_Anim(root.Name + "が次の零時に氾濫しそうです");
            }
        }

        /// <summary>gainLow から gainHigh は、攻略度 0 のときに日ごとに増える蓄積値の幅。既に同じ Id があれば何もしない</summary>
        private void Create(string name, string seed, string id, float mapX, float mapY, int danger,
            int gainLow = Root.DailyGainLow, int gainHigh = Root.DailyGainHigh)
        {
            Roots.TryAdd(new Root(id, name, seed, danger, mapX, mapY, gainLow, gainHigh));
        }
    }
}
