using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>根源の核の候補。根源のシーンに置き、読み込んだときに 1 つが核（RootCore）になる。核にならなかったものは叩くと壊れる</summary>
    public sealed class SpawnerCandidate : MonoBehaviour
    {
        /// <summary>体力のゲージの色</summary>
        private static readonly Color GaugeColor = new Color(0.9f, 0.3f, 0.3f);

        [SerializeField, FormerlySerializedAs("maxHp")] private int _maxHp = 100;

        [Tooltip("核になったとき、湧く地面をこの高さから真下へ探す。2 階建ての場所では、候補の立つ階の天井より低くする")]
        [SerializeField, FormerlySerializedAs("seekFrom")] private float _seekFrom = Seeker.DefaultFrom;

        [Tooltip("湧く地面を探す深さ。2 階建ての上の階では、床の穴から下の階に湧かないよう、下の階に届かない深さにする")]
        [SerializeField, FormerlySerializedAs("seekDepth")] private float _seekDepth = Seeker.DefaultDepth;

        private readonly Health _health = new Health();

        /// <summary>残りの体力を、絵のてっぺんの上に見せる</summary>
        private WorldGauge _gauge;

        /// <summary>核になったとき、湧く地面を探し始める高さ</summary>
        public float SeekFrom
        {
            get { return _seekFrom; }
        }

        /// <summary>核になったとき、湧く地面を探す深さ</summary>
        public float SeekDepth
        {
            get { return _seekDepth; }
        }

        private void Awake()
        {
            _health.SetMax(_maxHp);
            _health.SetCurrent(_maxHp);
            _gauge = WorldGauge.Put(transform, GaugeColor);
        }

        /// <summary>剣が当たった。体力が尽きたら消える</summary>
        public void Hit(int damage)
        {
            _health.Take(damage);
            _gauge.Show((float)_health.Current / _health.Max, GaugeColor);

            if (_health.IsDead) gameObject.SetActive(false);
        }
    }
}
