using System;

namespace MemorialFloor.Domain
{
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
