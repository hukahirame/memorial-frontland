using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 手に持つ剣の当たり。振ったときに剣の当たりを出し、回避攻撃では体のまわりに当たりを出す。
    /// 当てた相手に傷を入れ、吹き飛ばし、止めと揺れと音で手応えを出す。剣の物に付ける
    /// </summary>
    public sealed class Weapon : MonoBehaviour
    {
        /// <summary>倒したときの音の高さ。当てたときと同じ音を低くして重さを出す</summary>
        private const float KillPitch = 0.7f;

        /// <summary>回避攻撃の当たりの下端と上端。体の中心からの高さで、足もとの敵から頭の高さまで</summary>
        private const float SpinBottom = -0.5f;
        private const float SpinTop = 0.2f;

        private const string ImpactSound = "Fantasy_Game_Attack_Weapon_Impact";

        /// <summary>敵の体の当たりの親に付く札</summary>
        private const string EnemyTag = "Enemy";

        [SerializeField, FormerlySerializedAs("damage_set")] private GameObject _damageNumberPrefab;

        [Tooltip("持っているだけで攻撃力が上がる品を数える持ち物。持ち物の無い場面では空にして、足さない")]
        [SerializeField] private PlayerInventory _inventory;

        private BoxCollider _box;
        private HitStop _hitStop;
        private MainCamera _mainCamera;
        private PlayerMovement _movement;
        private PlayerAttack _attack;
        private PlayerEquipment _equipment;

        private void Awake()
        {
            _movement = GetComponentInParent<PlayerMovement>();
            _attack = GetComponentInParent<PlayerAttack>();
            _equipment = GetComponentInParent<PlayerEquipment>();
        }

        private void OnEnable()
        {
            if (_attack == null) return;

            _attack.Struck += BeginStrike;
            _attack.SpinStruck += BeginSpinStrike;
        }

        private void OnDisable()
        {
            if (_attack == null) return;

            _attack.Struck -= BeginStrike;
            _attack.SpinStruck -= BeginSpinStrike;
        }

        private void Start()
        {
            _box = GetComponent<BoxCollider>();
            _box.enabled = false;

            //時間止めはプレイヤーの一番上に1つだけ置く。武器を持ち替えても同じものを使う
            _hitStop = transform.root.GetComponent<HitStop>();
            if (_hitStop == null) _hitStop = transform.root.gameObject.AddComponent<HitStop>();
            _mainCamera = transform.root.GetComponentInChildren<MainCamera>();
        }

        private void BeginStrike(float seconds)
        {
            StartCoroutine(Strike(seconds));
        }

        private void BeginSpinStrike(float seconds, float radius)
        {
            StartCoroutine(SpinStrike(seconds, radius));
        }

        //剣の当たりを seconds のあいだ出す
        private IEnumerator Strike(float seconds)
        {
            _box.enabled = true;

            yield return new WaitForSeconds(seconds);

            _box.enabled = false;
        }

        /// <summary>
        /// 回避攻撃の当たり。seconds のあいだ、体のまわりの半径 radius の縦長の範囲にいる相手に当てる。
        /// 剣の当たりと違って 1 体では止めず、範囲の中の相手それぞれに 1 回ずつ当てる
        /// </summary>
        private IEnumerator SpinStrike(float seconds, float radius)
        {
            var struck = new HashSet<GameObject>();

            for (float until = Time.time + seconds; Time.time < until;)
            {
                Vector3 center = transform.root.position;
                Collider[] found = Physics.OverlapCapsule(center + Vector3.up * SpinBottom, center + Vector3.up * SpinTop,
                                                          radius, ~0, QueryTriggerInteraction.Collide);
                foreach (Collider other in found) Hit(other, struck);

                yield return null;
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            //剣の当たりは、1 体に当たったらその振りではもう当たらない
            if (Hit(other, null)) _box.enabled = false;
        }

        /// <summary>other に当てる。当てたら true。struck を渡すと、そこにある相手には当てず、当てた相手を足す</summary>
        private bool Hit(Collider other, HashSet<GameObject> struck)
        {
            //出ている当たりは振りの終わりまで残る。そのあいだに倒れたり移り始めたりしたら当てない
            if (_movement != null && !_movement.CanAct) return false;

            int power = _equipment.Equipped.Attack + (_inventory != null ? _inventory.HeldAttack : 0);

            if (other.TryGetComponent(out RootCore core)) return HitCore(core, other.gameObject, struck, power);

            if (other.TryGetComponent(out SpawnerCandidate candidate))
            {
                if (!Mark(struck, other.gameObject)) return false;

                candidate.Hit(power);
                TempAudio.TempAudioPlay(ImpactSound);
                return true;
            }

            //柵は剣では削れない。壊すときは、柵のそばで出る Allmaity ボタンで壊す
            if (other.GetComponentInParent<Fence>() != null) return false;

            if (other.transform.parent == null || !other.transform.parent.CompareTag(EnemyTag)) return false;

            return HitEnemy(other, struck, power);
        }

        private bool HitCore(RootCore core, GameObject target, HashSet<GameObject> struck, int power)
        {
            if (!Mark(struck, target)) return false;

            bool wasBroken = core.IsBroken;
            core.Hit(power);
            TempAudio.TempAudioPlay(ImpactSound);

            //壊した一撃は、敵を倒したときと同じだけ止めて揺らす
            if (!wasBroken && core.IsBroken)
            {
                _hitStop.Stop(HitStop.KillSeconds);
                if (_mainCamera != null) _mainCamera.Shake(MainCamera.KillShake);
                TempAudio.TempAudioPlay(ImpactSound, KillPitch);
            }
            return true;
        }

        private bool HitEnemy(Collider other, HashSet<GameObject> struck, int power)
        {
            GameObject target = other.transform.parent.gameObject;
            IDamageable damageable = target.GetComponent<IDamageable>();
            if (damageable == null || damageable.CurrentHp <= 0) return false; //倒れて弾けている最中の相手には当たらない
            if (!Mark(struck, target)) return false;

            TempAudio.TempAudioPlay(ImpactSound);

            Vector3 from = transform.root.position;
            Vector3 to = target.transform.position;
            Push push = KnockbackRule.Velocity(from.x, from.z, to.x, to.z, _equipment.Equipped.Knockback, transform.lossyScale.x);
            damageable.TakeHit(power, new Vector3(push.X, push.Y, push.Z));
            bool killed = damageable.CurrentHp <= 0;

            GameObject number = Instantiate(_damageNumberPrefab, other.transform.position, Quaternion.identity, other.transform.parent.Find("Canvas"));
            number.GetComponent<DamageNumber>().Show(power);

            _hitStop.Stop(killed ? HitStop.KillSeconds : HitStop.HitSeconds);
            if (_mainCamera != null) _mainCamera.Shake(killed ? MainCamera.KillShake : MainCamera.HitShake);
            if (killed) TempAudio.TempAudioPlay(ImpactSound, KillPitch);

            //耐久が尽きたら素手に戻る
            if (_equipment.Equipped.Wear(1)) _equipment.Unequip();
            return true;
        }

        //struck が無ければ、いつでも当てる
        private static bool Mark(HashSet<GameObject> struck, GameObject target)
        {
            return struck == null || struck.Add(target);
        }
    }
}
