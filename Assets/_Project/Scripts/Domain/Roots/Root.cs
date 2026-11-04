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
        /// <summary>攻略度 0 のとき、日ごとに増える蓄積値の幅の既定。両端を含み、零時ごとにこの中から引く</summary>
        public const int DailyGainLow = 3;
        public const int DailyGainHigh = 9;

        /// <summary>攻略度がこれだけ上がるごとに、日ごとに増える蓄積値が 1 減る</summary>
        public const int ProgressPerGainStep = 10;

        /// <summary>【抑制】で割る前の下限。【抑制】に達するまでは、攻略度がいくら高くても日ごとにこれだけは増える</summary>
        public const int MinDailyAccumulationGain = 1;

        /// <summary>日ごとに減る攻略度の幅。両端を含み、零時ごとに根源ごとにこの中から引く</summary>
        public const int DailyProgressLossLow = 2;
        public const int DailyProgressLossHigh = 3;

        /// <summary>この蓄積値で氾濫する</summary>
        public const int StampedeAccumulation = 100;

        /// <summary>氾濫1回で拠点に攻め込む敵の数</summary>
        public const int StampedeRaiders = 5;

        /// <summary>【解明】の段。この攻略度から、湧いた敵が弱くなり、拠点からの近道が無料になる</summary>
        public const int RevealProgress = 30;

        /// <summary>【抑制】の段。この攻略度から、敵の湧きが抑えられ、日ごとに増える蓄積値が減る</summary>
        public const int SuppressProgress = 50;

        /// <summary>【抑制】に達しているとき、日ごとに増える蓄積値をこれで割る。端数は切り捨てるので、増えない夜もある</summary>
        public const int SuppressGainDivisor = 2;

        /// <summary>攻略度の上限。画面では % で出す</summary>
        public const int MaxProgress = 100;

        public string Id { get; }
        public string Name { get; }
        public string Seed { get; }

        /// <summary>危険度。現状どこからも書き換えられない</summary>
        public int Danger { get; }

        /// <summary>攻略度。0 から MaxProgress まで</summary>
        public int Progress { get; private set; }

        /// <summary>蓄積値</summary>
        public int Accumulation { get; private set; }

        /// <summary>攻略度 0 のとき、日ごとに増える蓄積値の幅。両端を含む</summary>
        public int GainLow { get; }
        public int GainHigh { get; }

        /// <summary>地図の札の位置。地図の中心からの距離で、画面の画素ではない</summary>
        public float UiX { get; }
        public float UiY { get; }

        /// <summary>スポーン地点は初回入場時に決まる。未設定を座標で表さない</summary>
        public bool HasSpawnPoint { get; private set; }
        public float SpawnX { get; private set; }
        public float SpawnY { get; private set; }
        public float SpawnZ { get; private set; }

        public Root(string id, string name, string seed, int danger, float uiX, float uiY,
            int gainLow = DailyGainLow, int gainHigh = DailyGainHigh)
        {
            Id = id;
            Name = name;
            Seed = seed;
            Danger = danger;
            UiX = uiX;
            UiY = uiY;
            GainLow = gainLow;
            GainHigh = gainHigh;
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

        /// <summary>次の零時に増えうる蓄積値のうち、いちばん多い量。攻略度が高いほど少ない</summary>
        public int MaxDailyGain
        {
            get { return GainFrom(GainHigh); }
        }

        /// <summary>次の零時に氾濫しうる。いちばん多く増えた場合で見るので、外れて翌晩に持ち越すことがある。いま氾濫しているものは含めない</summary>
        public bool MayOverflowNextDay
        {
            get { return Accumulation < StampedeAccumulation && Accumulation + MaxDailyGain >= StampedeAccumulation; }
        }

        /// <summary>日付が変わったときの変化。蓄積値が増え、攻略度が減る</summary>
        public void AdvanceDay(IRandom random)
        {
            AccumulateDaily(random);
            DecayProgressDaily(random);
        }

        /// <summary>日次の蓄積値の増加。増える量は幅の中から引く。氾濫判定はこの直後に行われる</summary>
        public void AccumulateDaily(IRandom random)
        {
            Accumulation += GainFrom(random.Next(GainLow, GainHigh + 1));
        }

        /// <summary>攻略度 0 なら baseGain 増える日に、いまの攻略度で実際に増える量</summary>
        private int GainFrom(int baseGain)
        {
            int gain = baseGain - Progress / ProgressPerGainStep;
            if (gain < MinDailyAccumulationGain) gain = MinDailyAccumulationGain;

            return Progress >= SuppressProgress ? gain / SuppressGainDivisor : gain;
        }

        /// <summary>日次の攻略度の減少。減る量は幅の中から引く</summary>
        public void DecayProgressDaily(IRandom random)
        {
            Progress = Clamp0(Progress - random.Next(DailyProgressLossLow, DailyProgressLossHigh + 1));
        }

        /// <summary>蓄積値を下げる。スライム撃破など</summary>
        public void Calm(int amount)
        {
            Accumulation = Clamp0(Accumulation - amount);
        }

        /// <summary>氾濫していれば蓄積値を空にし、拠点に攻め込む敵の数を返す。していなければ 0</summary>
        public int Overflow()
        {
            if (Level != AccumulationLevel.Stampede) return 0;

            Accumulation = 0;
            return StampedeRaiders;
        }

        /// <summary>攻略度を上げる。クエスト報酬など。MaxProgress で止まる</summary>
        public void Gain(int amount)
        {
            Progress = ProgressAfterGain(amount);
        }

        /// <summary>上げたらいくつになるか。上げる前に結果の画面で見せる</summary>
        public int ProgressAfterGain(int amount)
        {
            return CapProgress(Progress + amount);
        }

        /// <summary>保存から戻す。攻略度は上限で止め、どちらも 0 を下回らない</summary>
        public void Restore(int progress, int accumulation)
        {
            Progress = CapProgress(progress);
            Accumulation = Clamp0(accumulation);
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

        private static int CapProgress(int value)
        {
            return value > MaxProgress ? MaxProgress : Clamp0(value);
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

        public void AdvanceDay(IRandom random)
        {
            for (int i = 0; i < _roots.Count; i++) _roots[i].AdvanceDay(random);
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _roots.Clear();
        }
    }

    /// <summary>拠点に攻め込んだ敵 1 体の控え。位置を持たないものは氾濫で来たばかりで、拠点の湧き口のまわりに満タンで出る</summary>
    public readonly struct Raider
    {
        /// <summary>体力を控えていない。出すときは満タン</summary>
        public const int FullHp = 0;

        /// <summary>氾濫で来たばかりの 1 体</summary>
        public static Raider Fresh
        {
            get { return default(Raider); }
        }

        public Raider(float x, float y, float z, int hp)
        {
            HasPosition = true;
            X = x;
            Y = y;
            Z = z;
            Hp = hp;
        }

        /// <summary>位置を持つ。拠点を離れるときに生き残っていた敵</summary>
        public bool HasPosition { get; }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        /// <summary>残りの体力。FullHp なら満タン</summary>
        public int Hp { get; }
    }

    /// <summary>
    /// 拠点に攻め込んだ敵の控え。拠点のシーンが無いあいだ（留守のあいだと、保存から戻して拠点に着くまで）の敵を持つ。
    /// 拠点に居るあいだはシーンの敵が正で、氾濫で来た敵も拠点の MS_Spawner がすぐ出して控えを空にする
    /// </summary>
    public sealed class BaseRaid
    {
        private readonly List<Raider> _kept = new List<Raider>();

        public IReadOnlyList<Raider> Kept
        {
            get { return _kept; }
        }

        /// <summary>氾濫で count 体攻め込んだ。位置を持たない敵として控える</summary>
        public void Invade(int count)
        {
            for (int i = 0; i < count; i++) _kept.Add(Raider.Fresh);
        }

        /// <summary>1 体控える。拠点を離れるときと、保存から戻すときに使う</summary>
        public void Keep(Raider raider)
        {
            _kept.Add(raider);
        }

        /// <summary>拠点に出す。控えていた敵をすべて渡し、控えを空にする</summary>
        public List<Raider> TakeAll()
        {
            List<Raider> all = new List<Raider>(_kept);
            _kept.Clear();

            return all;
        }

        public void Clear()
        {
            _kept.Clear();
        }
    }
}
