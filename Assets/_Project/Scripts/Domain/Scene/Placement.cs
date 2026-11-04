using System;
using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>場所に置いた物1つ。アイテム ID と位置と向き。耐久のある物（木の柵）は、残りの耐久と控えた時刻と倒れているかも持つ</summary>
    public readonly struct Placement
    {
        /// <summary>耐久を控えていない。置き直すと満タンで立つ</summary>
        public const int NoDurability = -1;

        public Placement(string itemId, float x, float y, float z, float yaw = 0f,
                         int durability = NoDurability, float recordedAt = 0f, bool fallen = false)
        {
            ItemId = itemId;
            X = x;
            Y = y;
            Z = z;
            Yaw = yaw;
            Durability = durability;
            RecordedAt = recordedAt;
            Fallen = fallen;
        }

        public string ItemId { get; }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }

        /// <summary>y 軸まわりの向き（度）。向きを持たない物は 0</summary>
        public float Yaw { get; }

        /// <summary>残りの耐久。耐久を持たない物と、控えていない物は NoDurability</summary>
        public int Durability { get; }

        /// <summary>控えた時刻（遊んでいるあいだの秒）。置き直すときに、離れていたあいだのぶん耐久を戻すのに使う</summary>
        public float RecordedAt { get; }

        /// <summary>倒れている。倒れる耐久と起き上がる耐久が違うので、耐久だけでは決まらない</summary>
        public bool Fallen { get; }
    }

    /// <summary>
    /// 場所ごとの、置いた物の控え。場所を離れるときにその場所のぶんを丸ごと書き直し、
    /// 入るときに読む。ほかの場所のぶんには触れない。
    /// </summary>
    public sealed class PlacementLedger
    {
        private readonly Dictionary<string, List<Placement>> _byArea =
            new Dictionary<string, List<Placement>>(StringComparer.Ordinal);

        /// <summary>控えのある場所</summary>
        public IEnumerable<string> Areas
        {
            get { return _byArea.Keys; }
        }

        /// <summary>その場所の控えを書き直す。1つも無ければ控えごと消す</summary>
        public void Record(string areaId, IEnumerable<Placement> placements)
        {
            if (string.IsNullOrEmpty(areaId)) throw new ArgumentException("場所が無い", nameof(areaId));

            List<Placement> list = placements == null ? new List<Placement>() : new List<Placement>(placements);

            if (list.Count == 0) _byArea.Remove(areaId);
            else _byArea[areaId] = list;
        }

        /// <summary>その場所の控え。無ければ空</summary>
        public IReadOnlyList<Placement> In(string areaId)
        {
            if (areaId != null && _byArea.TryGetValue(areaId, out List<Placement> list)) return list;

            return Array.Empty<Placement>();
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _byArea.Clear();
        }
    }
}
