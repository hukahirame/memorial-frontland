using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 根源の核。根源のシーンを読み込んだとき、核の候補の 1 つに足す（SceneStarter）。
    /// まわりに敵を湧かせ続け、壊すと一度に大量に湧かせる。湧き口としては壊れたあとも残る
    /// </summary>
    public sealed class RootCore : MonoBehaviour
    {
        /// <summary>核の札と、調査・決壊クエストの対象の名前</summary>
        public const string Tag = QuestBook.CoreTarget;

        /// <summary>核の耐久</summary>
        private const int MaxHp = 100;

        /// <summary>湧く範囲の半径の幅。核ごとに決める</summary>
        private const float MinRange = 6f;
        private const float MaxRange = 12f;

        /// <summary>湧きの連鎖の 1 本が、次に湧かせるまで待つ秒数の幅</summary>
        private const int ChainMinSeconds = 15;
        private const int ChainMaxSeconds = 30;

        /// <summary>いつも走らせておく湧きの連鎖の本数。壊したときの大量発生でも増やさない</summary>
        private const int SpawnChains = 2;

        /// <summary>壊したときに湧かせる数の幅。ほかに拠点への出口の脇へ 2 体出す</summary>
        private const int StampedeMin = 4;
        private const int StampedeMax = 7;

        /// <summary>壊れてから見えなくなるまでの秒数。倒した敵と同じ見せ方にする</summary>
        private const float BurstSeconds = 0.25f;

        /// <summary>壊れる前に膨らむ大きさの倍率</summary>
        private const float BurstSwell = 1.3f;

        /// <summary>出口の脇の点が場からはみ出すとき、場に入る点を引き直す回数。尽きたら出口の位置を場へ丸める</summary>
        private const int BesideExitTries = 16;

        /// <summary>ここから出した敵。湧きを見送るかは、このうちいま居る数で決める</summary>
        private readonly List<GameObject> _spawned = new List<GameObject>();

        private readonly Health _health = new Health();

        /// <summary>保存から戻す敵。湧き始める前に出す</summary>
        private readonly List<KeyValuePair<Vector3, int>> _kept = new List<KeyValuePair<Vector3, int>>();

        /// <summary>壊れて見えなくした描画</summary>
        private readonly List<Renderer> _hidden = new List<Renderer>();

        private EnemyContext _context;
        private Vector4 _exposition;

        /// <summary>置かれた根源の Id</summary>
        private string _rootId;

        private float _range;
        private Seeker _seeker;
        private GameObject _enemy;

        /// <summary>湧く地面を探し始める高さと深さ。置かれた核の候補が持ち、2 階建ての場所では候補の立つ階に湧かせる</summary>
        private float _seekFrom = Seeker.DefaultFrom;
        private float _seekDepth = Seeker.DefaultDepth;

        /// <summary>壊れる前の大きさ。膨らんでいる途中で直したときに戻す</summary>
        private Vector3 _burstSize;

        private Coroutine _burst;

        /// <summary>この一撃で壊れた</summary>
        public event Action Broken;

        public bool IsBroken
        {
            get { return _health.IsDead; }
        }

        private void Awake()
        {
            _health.SetMax(MaxHp);
            _health.SetCurrent(MaxHp);
        }

        /// <summary>根源に置かれたとき、場の範囲と根源を受け取る。Start より前に呼ぶ</summary>
        public void Bind(Vector4 exposition, string rootId, EnemyContext context)
        {
            _exposition = exposition;
            _rootId = rootId;
            _context = context;
        }

        /// <summary>保存してあった敵を 1 体、湧き始める前に戻す。位置は世界の位置。Start より前に呼ぶ</summary>
        public void Keep(Vector3 at, int hp)
        {
            _kept.Add(new KeyValuePair<Vector3, int>(at, hp));
        }

        private void Start()
        {
            gameObject.tag = Tag;
            _range = Random.Range(MinRange, MaxRange);

            //地面を探す役は、湧き口と同じ場所のものを使う。ほかの場所も読み込んであると、名前で引けばそちらをつかむ
            Area here = Area.In(gameObject.scene);
            _seeker = here.Find<Seeker>();
            _seeker.transform.parent = transform;

            SpawnerCandidate candidate = GetComponent<SpawnerCandidate>();
            if (candidate != null)
            {
                _seekFrom = candidate.SeekFrom;
                _seekDepth = candidate.SeekDepth;
            }

            //湧く敵は場所が決める
            _enemy = here.Enemy;
            if (_enemy == null) Debug.LogError(here.Id + " の Area に湧く敵が入っていない");

            //体力を持たない前の保存の敵は、新しく湧いた敵として出す
            if (_kept.Count > 0)
            {
                foreach (KeyValuePair<Vector3, int> kept in _kept)
                {
                    if (kept.Value > 0) Revive(kept.Key, kept.Value);
                    else Put(kept.Key);
                }
                _kept.Clear();
            }
            else
            {
                for (int i = 0; i < SpawnChains; i++) Spawn(_range * 2);
            }

            for (int i = 0; i < SpawnChains; i++) StartCoroutine(Chain());
        }

        /// <summary>湧きの連鎖の 1 本。待っては 1 体湧かせる。根源を離れて物が無効になると止まる</summary>
        private IEnumerator Chain()
        {
            while (true)
            {
                yield return new WaitForSeconds(Random.Range(ChainMinSeconds, ChainMaxSeconds));

                //移動のあいだは出さない。離れる途中に出すと、根源のシーンと一緒に消える
                if (_context.Travel.IsTraveling) continue;

                Spawn(_range);
            }
        }

        /// <summary>核のまわり range の中に 1 体湧かせる。地面が見つからなければ湧かせない</summary>
        private void Spawn(float range)
        {
            _seeker.gameObject.SetActive(true);

            Vector3? ground = _seeker.FindGround(range, Exposition.ToBounds(_exposition), _seekFrom, _seekDepth);
            if (ground.HasValue) Put(ground.Value);
        }

        /// <summary>at に 1 体出す。攻略度によっては見送るか弱める。次の湧きは仕掛けない</summary>
        private void Put(Vector3 at)
        {
            //根源は保持せず使うときに引く。参照を握るとレジストリが唯一の窓口でなくなり、
            //中身を入れ替えたときに古い Root を指したまま生き残る
            Root root = _context.Roots.Roots.Find(_rootId);
            if (SpawnRule.ShouldSuppress(root, Alive(), UnityRandom.Shared)) return;

            GameObject spawned = Create(at);
            _seeker.gameObject.SetActive(false);

            IDamageable damageable = spawned.GetComponent<IDamageable>();
            if (SpawnRule.ShouldWeaken(root) && damageable != null)
            {
                damageable.TakeDamage(Mathf.RoundToInt(damageable.CurrentHp * SpawnRule.WeakenRatio));
            }
        }

        /// <summary>保存した敵を at に戻す。間引かず、体力は控えた値まで減らす。弱めた分も控えた体力に入っている</summary>
        private void Revive(Vector3 at, int hp)
        {
            GameObject spawned = Create(at);

            IDamageable damageable = spawned.GetComponent<IDamageable>();
            if (damageable != null && hp < damageable.CurrentHp) damageable.TakeDamage(damageable.CurrentHp - hp);
        }

        private GameObject Create(Vector3 at)
        {
            GameObject spawned = Instantiate(_enemy, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(spawned, gameObject.scene);

            Slime slime = spawned.GetComponent<Slime>();
            if (slime != null) slime.Bind(_context, _rootId);

            _spawned.Add(spawned);
            return spawned;
        }

        /// <summary>ここから出した敵のうち、いま居る数。倒されて消えたものと場外へ落ちたものは数えない</summary>
        private int Alive()
        {
            _spawned.RemoveAll(spawned => spawned == null || !spawned.activeSelf);
            return _spawned.Count;
        }

        /// <summary>剣が当たった。壊れたあとに当たっても、湧きや達成を重ねて起こさない</summary>
        public void Hit(int damage)
        {
            if (_health.IsDead) return;

            _health.Take(damage);
            if (!_health.IsDead) return;

            Broken?.Invoke();
            Stampede(Random.Range(StampedeMin, StampedeMax));
            _context.Quests.DefeatIn(Tag, _rootId);
            foreach (Collider part in GetComponents<Collider>()) part.enabled = false;
            _context.Roots.ReportStampede();
            _burst = StartCoroutine(Burst());
        }

        /// <summary>壊したときの大量発生。一度きりで、湧きの連鎖は足さない</summary>
        private void Stampede(int amount)
        {
            for (int i = 0; i < amount; i++) Spawn(_range * 2);

            Vector3 exit = Area.In(gameObject.scene).ExitOf(ExitKind.ToPrevious).transform.position;
            Put(BesideExit(exit));
            Put(BesideExit(exit));
        }

        /// <summary>拠点への出口の脇の点。場の外の点は、丸めると場の縁に一列に並ぶので引き直す。出口が場の角にある根源では外が多い</summary>
        private Vector3 BesideExit(Vector3 exit)
        {
            FieldBounds bounds = Exposition.ToBounds(_exposition);

            for (int tries = 0; tries < BesideExitTries; tries++)
            {
                Vector3 at = exit + new Vector3(Random.Range(1f, 6f), 0, Random.Range(-5f, 1f));
                if (bounds.Contains(at.x, at.z)) return at;
            }

            return new Vector3(bounds.ClampX(exit.x), exit.y, bounds.ClampZ(exit.z));
        }

        /// <summary>
        /// 膨らんでから縮み、見えなくなる。湧き口としては残すので物は消さず、描画だけ切る。
        /// 子の Seeker を縮めたままにしないよう、大きさは最後に戻す
        /// </summary>
        private IEnumerator Burst()
        {
            _burstSize = transform.localScale;
            for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                float k = t / BurstSeconds;
                float scale = k < 0.3f
                    ? Mathf.Lerp(1f, BurstSwell, k / 0.3f)
                    : Mathf.Lerp(BurstSwell, 0f, (k - 0.3f) / 0.7f);

                transform.localScale = _burstSize * scale;
                yield return null;
            }

            foreach (Renderer shown in GetComponentsInChildren<Renderer>())
            {
                if (!shown.enabled) continue;

                shown.enabled = false;
                _hidden.Add(shown);
            }
            transform.localScale = _burstSize;
            _burst = null;
        }

        /// <summary>
        /// 核の耐久を戻し、壊れていれば立て直す。プレイヤーが根源に入ったときに呼ぶ（SceneStarter）。
        /// 根源は隣として読み込んだまま出入りすることがあり、読み直さなくても入るたびに戻す
        /// </summary>
        public void Mend()
        {
            if (!_health.IsDead) return;

            if (_burst != null)
            {
                StopCoroutine(_burst);
                _burst = null;
                transform.localScale = _burstSize;
            }

            foreach (Renderer shown in _hidden)
            {
                if (shown != null) shown.enabled = true;
            }
            _hidden.Clear();
            foreach (Collider part in GetComponents<Collider>()) part.enabled = true;

            _health.SetCurrent(_health.Max);
        }
    }
}
