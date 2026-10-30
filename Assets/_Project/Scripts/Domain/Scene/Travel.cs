using System;

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

    /// <summary>出口の種類。行き先そのものではなく、行き先の決まり方を表す</summary>
    public enum ExitKind
    {
        /// <summary>拠点へ戻る</summary>
        ToBase,

        /// <summary>受注中のクエストの根源へ向かう</summary>
        ToOrderedRoot,
    }

    /// <summary>着いたときに立つ地点</summary>
    public enum Arrival
    {
        /// <summary>出口を通ってきた。入口に立つ</summary>
        Entrance,

        /// <summary>始めたとき、倒れたあと。決まった地点に立つ</summary>
        Home,
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
            get { return new Route(null, Arrival.Entrance); }
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

        /// <summary>出口を通ったときの行き先。根源へは受注中のクエストが無いと行けない</summary>
        public static Route Through(ExitKind exit, Quest ordered)
        {
            if (exit == ExitKind.ToBase) return Route.To(BaseAreaId, Arrival.Entrance);
            if (ordered == null) return Route.Refused;

            return Route.To(AreaOfRoot(ordered.RootId), Arrival.Entrance);
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
