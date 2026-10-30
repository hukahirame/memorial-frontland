using System;
using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>場所に置いた物1つ。アイテム ID と位置</summary>
    public readonly struct Placement
    {
        public Placement(string itemId, float x, float y, float z)
        {
            ItemId = itemId;
            X = x;
            Y = y;
            Z = z;
        }

        public string ItemId { get; }

        public float X { get; }

        public float Y { get; }

        public float Z { get; }
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
