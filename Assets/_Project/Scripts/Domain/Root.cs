using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>蓄積値の段階。閾値は RootsManager と RootUI に二重定義されていたものを1つにした</summary>
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
        public const int DailyAccumulationGain = 10;
        public const int DailyProgressLoss = 3;

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

        /// <summary>スポーン地点は初回入場時に決まる。元は座標 -100 を未設定の印にしていた</summary>
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
                if (Accumulation >= 100) return AccumulationLevel.Stampede;
                if (Accumulation >= 75) return AccumulationLevel.High;
                if (Accumulation >= 40) return AccumulationLevel.Medium;
                if (Accumulation >= 15) return AccumulationLevel.Small;
                return AccumulationLevel.Minimal;
            }
        }

        /// <summary>日付が変わったときの変化。蓄積値が増え、攻略度が減る</summary>
        public void AdvanceDay()
        {
            Accumulation += DailyAccumulationGain;
            Progress = Clamp0(Progress - DailyProgressLoss);
        }

        /// <summary>蓄積値を下げる。スライム撃破など</summary>
        public void Calm(int amount)
        {
            Accumulation = Clamp0(Accumulation - amount);
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

        /// <summary>同じ Id が既にあれば何もせず false を返す。
        /// シーン再入場で Start が再実行されても増えないようにするため</summary>
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
    }
}
