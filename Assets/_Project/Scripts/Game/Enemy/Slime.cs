using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// スライム。うろつき、プレイヤーを見つけたら追って体当たりし、柵にぶつかると削る。攻め込んだ先では目指す地点へ寄っていく。
    /// 倒れると弾けて取得物を落とし、討伐のクエストを進め、湧いた根源を鎮める
    /// </summary>
    public sealed class Slime : MonoBehaviour, IDamageable
    {
        /// <summary>当たって白く光る長さ。止めた時間の中でも見えるよう実時間で数える</summary>
        private const float FlashSeconds = 0.1f;

        /// <summary>倒れてから弾けて消えるまでの秒数</summary>
        private const float BurstSeconds = 0.25f;

        /// <summary>弾ける前に膨らむ大きさの倍率</summary>
        private const float BurstSwell = 1.3f;

        /// <summary>倒した一撃のダメージ表示を、本体が消えたあとも残す秒数</summary>
        private const float NumberLingerSeconds = 1f;

        /// <summary>跳ぶ勢い。重さに掛ける</summary>
        private const float JumpForce = 2f;

        /// <summary>落とし物を放る勢い</summary>
        private const float DropToss = 2f;

        private const string CloneSuffix = "(Clone)";

        [SerializeField, FormerlySerializedAs("speed")] private float _speed;
        [SerializeField, FormerlySerializedAs("power")] private float _power;

        [Tooltip("必ず落ちる取得物")]
        [SerializeField, FormerlySerializedAs("drops")] private List<string> _drops = new List<string>();

        [Tooltip("割合で落ちる取得物")]
        [SerializeField, FormerlySerializedAs("chanceDrops")]
        private List<DropEntry> _chanceDrops = new List<DropEntry>
        {
            new DropEntry { itemId = "Slimecore", percent = 5 }
        };

        [Tooltip("落とし物の prefab")]
        [SerializeField, FormerlySerializedAs("dropcapsule")] private GameObject _dropPrefab;

        [Tooltip("体力のバー。上限と初期値もここに置く")]
        [SerializeField, FormerlySerializedAs("hp")] private Slider _hpBar;

        [Tooltip("倒したときに進める討伐クエストの対象。空なら自分の名前。色違いは元の種類の名前にし、同じ討伐に数える")]
        [SerializeField, FormerlySerializedAs("questTarget")] private string _questTarget;

        [Tooltip("当たって白く光るときの塗り")]
        [SerializeField, FormerlySerializedAs("flashShader")] private Shader _flashShader;

        private readonly Health _health = new Health();

        private EnemyContext _context;

        /// <summary>湧いた根源の Id。根源の外で湧いたものは null</summary>
        private string _homeRootId;

        private Rigidbody _body;
        private SpriteRenderer _sprite;
        private Material _baseMaterial;
        private Material _flashMaterial;
        private Vector3 _heading;
        private bool _isDead;
        private bool _isMoving;

        /// <summary>この時刻までは吹き飛ばしに任せ、自分では動かない</summary>
        private float _staggerUntil;

        /// <summary>この時刻までは、柵にぶつかり続けていても削らない</summary>
        private float _strikeFenceAt;

        /// <summary>いまプレイヤーを見つけているか。見つけた瞬間を知るために持つ</summary>
        private bool _isNoticing;

        /// <summary>この時刻までは、見つけても「！」を出し直さない</summary>
        private float _alertAgainAt;

        /// <summary>攻め込んだ先で目指す地点。持たないものはうろつくだけ</summary>
        private Vector3 _goal;
        private bool _hasGoal;

        public int CurrentHp
        {
            get { return _health.Current; }
        }

        //上限と初期値は prefab の体力のバーが持つ。以後バーは表示
        private void Awake()
        {
            _health.SetMax(Mathf.RoundToInt(_hpBar.maxValue));
            _health.SetCurrent(Mathf.RoundToInt(_hpBar.value));

            _body = GetComponent<Rigidbody>();
            _sprite = GetComponent<SpriteRenderer>();
            _baseMaterial = _sprite.sharedMaterial;

            if (_flashShader != null) _flashMaterial = new Material(_flashShader);
        }

        private void OnDestroy()
        {
            if (_flashMaterial != null) Destroy(_flashMaterial);
        }

        /// <summary>湧かせた側が、常駐シーンのものと、どの根源の敵かを渡す。Start より前に呼ぶ</summary>
        public void Bind(EnemyContext context, string homeRootId)
        {
            _context = context;
            _homeRootId = homeRootId;
        }

        /// <summary>湧かせた側が、攻め込む先の地点を渡す</summary>
        public void SetGoal(Vector3 point)
        {
            _goal = point;
            _hasGoal = true;
        }

        public void TakeDamage(int amount)
        {
            _health.Take(amount);
            _hpBar.value = _health.Current;
        }

        public void TakeHit(int amount, Vector3 push)
        {
            TakeDamage(amount);

            _body.linearVelocity = push;
            _staggerUntil = Time.time + ActionRule.StaggerSeconds;

            if (_flashMaterial != null) StartCoroutine(Flash());
        }

        private void Start()
        {
            //討伐の対象は名前で突き合わせる
            if (gameObject.name.EndsWith(CloneSuffix)) gameObject.name = gameObject.name.Substring(0, gameObject.name.Length - CloneSuffix.Length);

            ChooseAction();
        }

        private void FixedUpdate()
        {
            if (_isDead) return;

            if (_isMoving && Time.time >= _staggerUntil)
            {
                //見つけているあいだは、動いている最中もプレイヤーへ向け直す
                if (Aim() == EnemyAim.Player) _heading = Direction(EnemyAim.Player);
                _body.linearVelocity = _heading * _speed;
            }

            if (FallRule.IsOutOfField(transform.position.y)) gameObject.SetActive(false);

            if (_health.IsDead)
            {
                _isDead = true;
                StartCoroutine(Burst());
            }
        }

        private IEnumerator Flash()
        {
            _sprite.sharedMaterial = _flashMaterial;
            yield return new WaitForSecondsRealtime(FlashSeconds);
            _sprite.sharedMaterial = _baseMaterial;
        }

        /// <summary>
        /// 膨らんでから縮んで消える。表示の Canvas は本体と一緒に縮むため切り離し、倒した一撃のダメージ表示だけを残す
        /// </summary>
        private IEnumerator Burst()
        {
            //倒れたらすぐ通り抜けられるようにし、その場で弾ける。床も抜けるので重力も切る。
            //当たり判定の子は prefab ごとに名前が違い、同じ名前の無効な子が先に並ぶものもあるので、名前で探さない
            foreach (Collider part in GetComponentsInChildren<Collider>()) part.enabled = false;
            _body.useGravity = false;
            _body.linearVelocity = Vector3.zero;

            //倒れた体はもう動かさない。跳んだ段に倒されると、溜まった跳ぶ力があとから掛かって浮いていく。
            //物理から外した体は連続の衝突検出を持てないので、先に戻す。当たり判定はもう切ってある
            _body.collisionDetectionMode = CollisionDetectionMode.Discrete;
            _body.isKinematic = true;

            _hpBar.gameObject.SetActive(false);

            Transform canvas = transform.Find("Canvas");
            if (canvas != null)
            {
                canvas.SetParent(null, true);
                Destroy(canvas.gameObject, NumberLingerSeconds);
            }

            Vector3 size = transform.localScale;
            for (float t = 0f; t < BurstSeconds; t += Time.deltaTime)
            {
                float k = t / BurstSeconds;
                float scale = k < 0.3f
                    ? Mathf.Lerp(1f, BurstSwell, k / 0.3f)
                    : Mathf.Lerp(BurstSwell, 0f, (k - 0.3f) / 0.7f);

                transform.localScale = size * scale;
                yield return null;
            }

            Die();
        }

        private void ChooseAction()
        {
            if (_isDead) return;

            EnemyAim aim = Aim();
            EnemyAction action = ActionRule.Choose(UnityRandom.Shared, aim == EnemyAim.Player);

            _isMoving = action == EnemyAction.Move;
            _heading = Direction(aim);
            if (action == EnemyAction.Jump && Time.time >= _staggerUntil)
            {
                _body.AddForce((_heading + Vector3.up) * _body.mass * JumpForce, ForceMode.Impulse);
            }

            Invoke(nameof(ChooseAction), ActionRule.Duration(action, UnityRandom.Shared));
        }

        /// <summary>いま何を目指すか。倒れているプレイヤーと、ほかの階のプレイヤーは見ない</summary>
        private EnemyAim Aim()
        {
            PlayerHealth player = Player();
            float toPlayer = player != null && !player.IsDead
                ? AimRule.SightDistance(Flat(player.transform.position).magnitude, player.transform.position.y - transform.position.y)
                : float.MaxValue;
            float toGoal = _hasGoal ? Flat(_goal).magnitude : 0f;

            EnemyAim aim = AimRule.Choose(toPlayer, _hasGoal, toGoal);
            Notice(aim == EnemyAim.Player);

            return aim;
        }

        private PlayerHealth Player()
        {
            return _context != null ? _context.Player : null;
        }

        /// <summary>見つけた瞬間に、頭上へ「！」を出す。見つけたり見失ったりを繰り返す境目で出し続けないよう、出してからしばらくは出さない</summary>
        private void Notice(bool sees)
        {
            if (sees && !_isNoticing && Time.time >= _alertAgainAt)
            {
                AlertMark.Show(transform.Find("Canvas"));
                _alertAgainAt = Time.time + AlertMark.CooldownSeconds;
            }

            _isNoticing = sees;
        }

        /// <summary>目指すものへの向き。うろつく向きは乱数で決める</summary>
        private Vector3 Direction(EnemyAim aim)
        {
            if (aim == EnemyAim.Player)
            {
                Vector3 toPlayer = Flat(Player().transform.position);
                return Along(AimRule.Toward(toPlayer.x, toPlayer.z));
            }

            Vector3 wander = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
            if (aim == EnemyAim.Wander) return wander;

            Vector3 toGoal = Flat(_goal);
            return Along(AimRule.Lean(new Heading(wander.x, wander.z), toGoal.x, toGoal.z));
        }

        /// <summary>ここから point への水平な差</summary>
        private Vector3 Flat(Vector3 point)
        {
            Vector3 diff = point - transform.position;
            return new Vector3(diff.x, 0f, diff.z);
        }

        private static Vector3 Along(Heading heading)
        {
            return new Vector3(heading.X, 0f, heading.Z);
        }

        private void OnCollisionEnter(Collision other)
        {
            HurtPlayer(other);
            StrikeFence(other);
        }

        //無敵の間は吹き飛ばさないので、離れずに触れ続けることがある。無敵が切れたらまた当てる
        private void OnCollisionStay(Collision other)
        {
            HurtPlayer(other);
            StrikeFence(other);
        }

        /// <summary>柵にぶつかっているあいだ、決まった間隔ごとに攻撃力のぶん削る</summary>
        private void StrikeFence(Collision other)
        {
            if (_isDead || Time.time < _strikeFenceAt) return;
            if (!other.collider.TryGetComponent(out Fence fence)) return;

            fence.Ram((int)_power, transform.position);
            _strikeFenceAt = Time.time + Fence.StrikeSeconds;
        }

        private void HurtPlayer(Collision other)
        {
            if (_isDead || Player() == null) return;
            if (!other.gameObject.CompareTag("Player")) return;

            Player().Hurt((int)_power, other.gameObject.transform.position - transform.position);
        }

        /// <summary>弾け終わった。取得物を落とし、討伐を進め、湧いた根源を鎮めて消える</summary>
        private void Die()
        {
            foreach (string itemId in DropRule.Roll(_drops, _chanceDrops.ConvertAll(entry => entry.ToChance()), UnityRandom.Shared))
            {
                GameObject drop = Instantiate(_dropPrefab, transform.position, Quaternion.identity);
                SceneManager.MoveGameObjectToScene(drop, gameObject.scene);
                drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
                Vector3 toss = new Vector3(Random.Range(0, 1f), Random.Range(0.5f, 1), Random.Range(0, 1f)) * DropToss;
                drop.GetComponent<Rigidbody>().AddForce(toss, ForceMode.Impulse);
            }

            if (_context != null)
            {
                string target = string.IsNullOrEmpty(_questTarget) ? gameObject.name : _questTarget;
                _context.Quests.DefeatIn(target, _homeRootId);

                Root root = _context.Roots.Roots.Find(_homeRootId);
                if (root != null) root.Calm(SpawnRule.CalmPerDefeat);
            }

            Destroy(gameObject);
        }
    }
}
