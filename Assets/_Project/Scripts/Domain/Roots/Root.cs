using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>蓄積値の段階</summary>
    public enum AccumulationLevel
    {
        /// <summary>微</summary>
        Minimal,
        /// <summary>小</summary>
        Small,
        /// <summary>中</summary>
        Medium,
        /// <summary>高</summary>
        High,
        /// <summary>氾濫</summary>
        Stampede,
    }

    /// <summary>根源1つ。生成後に変わらない素性と、遊ぶうちに動く3つの値を持つ</summary>
    public sealed class Root
    {
        /// <summary>攻略度 0 のとき、日ごとに増える蓄積値</summary>
        public const int DailyAccumulationGain = 10;

        /// <summary>攻略度がこれだけ上がるごとに、日ごとに増える蓄積値が 1 減る</summary>
        public const int ProgressPerGainStep = 10;

        /// <summary>攻略度がいくら高くても、日ごとにこれだけは増える</summary>
        public const int MinDailyAccumulationGain = 3;

        public const int DailyProgressLoss = 3;

        /// <summary>この蓄積値で氾濫する</summary>
        public const int StampedeAccumulation = 100;

        /// <summary>氾濫1回で拠点へ向かう敵の数</summary>
        public const int StampedeRaiders = 5;

        public string Id { get; }
        public string Name { get; }
        public string Seed { get; }

        /// <summary>危険度。現状どこからも書き換えられない</summary>
        public int Danger { get; }

        /// <summary>攻略度。上限は設けない（元のコードにも無い）</summary>
        public int Progress { get; private set; }

        /// <summary>蓄積値</summary>
        public int Accumulation { get; private set; }

        /// <summary>マップ上での UI 位置</summary>
        public float UiX { get; }
        public float UiY { get; }

        /// <summary>スポーン地点は初回入場時に決まる。未設定を座標で表さない</summary>
        public bool HasSpawnPoint { get; private set; }
        public float SpawnX { get; private set; }
        public float SpawnY { get; private set; }
        public float SpawnZ { get; private set; }

        public Root(string id, string name, string seed, int danger, float uiX, float uiY)
        {
            Id = id;
            Name = name;
            Seed = seed;
            Danger = danger;
            UiX = uiX;
            UiY = uiY;
        }

        public AccumulationLevel Level
        {
            get
            {
                if (Accumulation >= StampedeAccumulation) return AccumulationLevel.Stampede;
                if (Accumulation >= 75) return AccumulationLevel.High;
                if (Accumulation >= 40) return AccumulationLevel.Medium;
                if (Accumulation >= 15) return AccumulationLevel.Small;
                return AccumulationLevel.Minimal;
            }
        }

        /// <summary>次の零時に増える蓄積値。攻略度が高いほど少ない</summary>
        public int DailyGain
        {
            get
            {
                int gain = DailyAccumulationGain - Progress / ProgressPerGainStep;
                return gain < MinDailyAccumulationGain ? MinDailyAccumulationGain : gain;
            }
        }

        /// <summary>次の零時に氾濫する。いま氾濫しているものは含めない</summary>
        public bool WillOverflowNextDay
        {
            get { return Accumulation < StampedeAccumulation && Accumulation + DailyGain >= StampedeAccumulation; }
        }

        /// <summary>日付が変わったときの変化。蓄積値が増え、攻略度が減る</summary>
        public void AdvanceDay()
        {
            AccumulateDaily();
            DecayProgressDaily();
        }

        /// <summary>日次の蓄積値の増加。氾濫判定はこの直後に行われる</summary>
        public void AccumulateDaily()
        {
            Accumulation += DailyGain;
        }

        /// <summary>日次の攻略度の減少</summary>
        public void DecayProgressDaily()
        {
            Progress = Clamp0(Progress - DailyProgressLoss);
        }

        /// <summary>蓄積値を下げる。スライム撃破など</summary>
        public void Calm(int amount)
        {
            Accumulation = Clamp0(Accumulation - amount);
        }

        /// <summary>氾濫していれば蓄積値を空にし、拠点へ向かう敵の数を返す。していなければ 0</summary>
        public int Overflow()
        {
            if (Level != AccumulationLevel.Stampede) return 0;

            Accumulation = 0;
            return StampedeRaiders;
        }

        /// <summary>攻略度を上げる。クエスト報酬など</summary>
        public void Gain(int amount)
        {
            Progress += amount;
        }

        public void PlaceSpawnPoint(float x, float y, float z)
        {
            SpawnX = x;
            SpawnY = y;
            SpawnZ = z;
            HasSpawnPoint = true;
        }

        private static int Clamp0(int value)
        {
            return value < 0 ? 0 : value;
        }
    }

    /// <summary>根源の集合。追加・検索・日付更新の唯一の窓口</summary>
    public sealed class RootRegistry
    {
        private readonly List<Root> _roots = new List<Root>();

        public IReadOnlyList<Root> All
        {
            get { return _roots; }
        }

        public int Count
        {
            get { return _roots.Count; }
        }

        /// <summary>同じ Id が既にあれば何もせず false を返す</summary>
        public bool TryAdd(Root root)
        {
            if (root == null) return false;
            if (Find(root.Id) != null) return false;

            _roots.Add(root);
            return true;
        }

        /// <summary>見つからなければ null</summary>
        public Root Find(string id)
        {
            for (int i = 0; i < _roots.Count; i++)
            {
                if (_roots[i].Id == id) return _roots[i];
            }

            return null;
        }

        public void AdvanceDay()
        {
            for (int i = 0; i < _roots.Count; i++) _roots[i].AdvanceDay();
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _roots.Clear();
        }
    }

    /// <summary>
    /// 拠点へ向かっている敵。根源の氾濫で積まれ、拠点で出る。拠点を離れるときに生き残っていた敵も積み戻される。
    /// 留守のあいだに積まれた分は着いたときにまとめて、居るあいだに積まれた分は間を空けて1体ずつ出す
    /// </summary>
    public sealed class BaseRaid
    {
        /// <summary>拠点に居るあいだ、1体出してから次を出すまでの秒数</summary>
        public const float IntervalSeconds = 6f;

        private float _wait;

        /// <summary>まだ出ていない数</summary>
        public int Pending { get; private set; }

        public void Add(int count)
        {
            if (count > 0) Pending += count;
        }

        /// <summary>拠点に着いた。積まれていた分を全て渡し、次の1体までの間を空ける</summary>
        public int Arrive()
        {
            int all = Pending;
            Pending = 0;
            _wait = IntervalSeconds;
            return all;
        }

        /// <summary>拠点に居るあいだの経過。いま出す数を返す。間が空いていれば積まれた直後に1体出る</summary>
        public int Tick(float seconds)
        {
            _wait = _wait > seconds ? _wait - seconds : 0f;
            if (_wait > 0f || Pending == 0) return 0;

            Pending--;
            _wait = IntervalSeconds;
            return 1;
        }

        public void Clear()
        {
            Pending = 0;
            _wait = 0f;
        }
    }
}
