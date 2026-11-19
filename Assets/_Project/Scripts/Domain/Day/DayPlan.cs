using System.Collections.Generic;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 調査クエストの発行計画。根源ごとに、1度保留してから発行する。
    /// 保留を挟むぶん、発行は次の呼び出しまで遅れる。
    /// </summary>
    public sealed class DayPlan
    {
        private readonly List<string> _pending = new List<string>();

        /// <summary>発行を待っている根源 Id</summary>
        public IReadOnlyList<string> Pending
        {
            get { return _pending; }
        }

        public int Count
        {
            get { return _pending.Count; }
        }

        /// <summary>
        /// その根源の計画を 1 つ進め、発行する番になったら true を返す。
        /// 保留していれば保留を外して true、していなければ保留にして false
        /// </summary>
        public bool Advance(string rootId)
        {
            if (string.IsNullOrEmpty(rootId)) return false;

            if (_pending.Remove(rootId)) return true;

            _pending.Add(rootId);
            return false;
        }

        /// <summary>保存から戻す。発行を待っている根源 Id を入れ直す。空と重複は捨てる</summary>
        public void Restore(IEnumerable<string> pending)
        {
            _pending.Clear();
            if (pending == null) return;

            foreach (string rootId in pending)
            {
                if (string.IsNullOrEmpty(rootId) || _pending.Contains(rootId)) continue;

                _pending.Add(rootId);
            }
        }

        /// <summary>全て捨てる。新規開始とテストの後始末で使う</summary>
        public void Clear()
        {
            _pending.Clear();
        }
    }
}
