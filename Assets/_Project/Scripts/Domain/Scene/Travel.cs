using System;
using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>場所の種類</summary>
    public enum AreaKind
    {
        /// <summary>拠点</summary>
        Base,

        /// <summary>根源</summary>
        Root,
    }

    /// <summary>
    /// 出口の種類。行き先そのものではなく、行き先の決まり方を表す。
    /// 場所は拠点、根源の順に一列に並び、出口はどちらかの隣へ通じる。値はシーンに保存されているので変えない
    /// </summary>
    public enum ExitKind
    {
        /// <summary>手前の場所へ戻る。拠点に近い側</summary>
        ToPrevious = 0,

        /// <summary>奥の場所へ進む</summary>
        ToNext = 1,
    }

    /// <summary>着いたときに立つ地点</summary>
    public enum Arrival
    {
        /// <summary>手前の場所から出口を通ってきた。手前の出口の前に立つ</summary>
        FromPrevious = 0,

        /// <summary>始めたとき、倒れたあと。決まった地点に立つ</summary>
        Home = 1,

        /// <summary>奥の場所から出口を通ってきた。奥の出口の前に立つ</summary>
        FromNext = 2,
    }

    /// <summary>行き先と、着いたときに立つ地点。行けないときは行き先を持たない</summary>
    public readonly struct Route
    {
        private Route(string areaId, Arrival arrival)
        {
            AreaId = areaId;
            Arrival = arrival;
        }

        /// <summary>行き先の場所。行けないときは null</summary>
        public string AreaId { get; }

        public Arrival Arrival { get; }

        public bool IsRefused
        {
            get { return AreaId == null; }
        }

        public static Route Refused
        {
            get { return new Route(null, Arrival.FromPrevious); }
        }

        public static Route To(string areaId, Arrival arrival)
        {
            if (string.IsNullOrEmpty(areaId)) throw new ArgumentException("行き先の場所が無い", nameof(areaId));

            return new Route(areaId, arrival);
        }
    }

    /// <summary>
    /// 移動の規則。場所の Id はシーン名で、根源の場所は根源の Id と同じ名前を持つ。
    /// この対応を知っているのはここだけにする。
    /// </summary>
    public static class TravelRule
    {
        /// <summary>拠点の場所</summary>
        public const string BaseAreaId = "MainSite";

        /// <summary>
        /// 出口を通ったときの行き先。場所は拠点、根源の並べた順に一列に並び、出口は隣の場所へ通じる。
        /// クエストを受けていなくても通れる。並びの端より先と、並びに無い場所からは行けない
        /// </summary>
        public static Route Through(ExitKind exit, string fromAreaId, IReadOnlyList<string> rootIds)
        {
            int count = rootIds == null ? 0 : rootIds.Count;
            int at = IndexInLine(fromAreaId, rootIds);
            if (at < 0) return Route.Refused;

            int to = exit == ExitKind.ToNext ? at + 1 : at - 1;
            if (to < 0 || to > count) return Route.Refused;

            string areaId = to == 0 ? BaseAreaId : AreaOfRoot(rootIds[to - 1]);
            return Route.To(areaId, exit == ExitKind.ToNext ? Arrival.FromPrevious : Arrival.FromNext);
        }

        /// <summary>一列の並びでの位置。拠点が 0、根源は並べた順に 1 から。並びに無ければ -1</summary>
        private static int IndexInLine(string areaId, IReadOnlyList<string> rootIds)
        {
            if (string.IsNullOrEmpty(areaId)) return -1;
            if (areaId == BaseAreaId) return 0;
            if (rootIds == null) return -1;

            for (int i = 0; i < rootIds.Count; i++)
            {
                if (AreaOfRoot(rootIds[i]) == areaId) return i + 1;
            }

            return -1;
        }

        /// <summary>倒れたあとの行き先。どこで倒れても拠点の決まった地点に戻る</summary>
        public static Route Revive
        {
            get { return Route.To(BaseAreaId, Arrival.Home); }
        }

        /// <summary>始めるときの行き先。保存した場所が無ければ拠点</summary>
        public static Route Begin(string savedAreaId)
        {
            return Route.To(string.IsNullOrEmpty(savedAreaId) ? BaseAreaId : savedAreaId, Arrival.Home);
        }

        /// <summary>根源の場所</summary>
        public static string AreaOfRoot(string rootId)
        {
            return rootId;
        }

        /// <summary>場所にある根源の Id。根源でない場所なら null</summary>
        public static string RootOfArea(string areaId, AreaKind kind)
        {
            return kind == AreaKind.Root ? areaId : null;
        }
    }

    /// <summary>
    /// 今いる場所。書き換えるのは移動の入口だけで、ほかは読むだけにする。
    /// 着く前はどこにも居ない。
    /// </summary>
    public sealed class Whereabouts
    {
        /// <summary>着く前は null</summary>
        public string AreaId { get; private set; }

        public AreaKind Kind { get; private set; }

        public bool HasArrived
        {
            get { return AreaId != null; }
        }

        public bool IsInBase
        {
            get { return HasArrived && Kind == AreaKind.Base; }
        }

        public bool IsInRoot
        {
            get { return HasArrived && Kind == AreaKind.Root; }
        }

        /// <summary>いま居る根源の Id。根源の外なら null</summary>
        public string RootId
        {
            get { return HasArrived ? TravelRule.RootOfArea(AreaId, Kind) : null; }
        }

        public void Arrive(string areaId, AreaKind kind)
        {
            if (string.IsNullOrEmpty(areaId)) throw new ArgumentException("着いた場所が無い", nameof(areaId));

            AreaId = areaId;
            Kind = kind;
        }
    }
}
