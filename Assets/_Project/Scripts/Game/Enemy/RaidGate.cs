using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 拠点の湧き口。根源の氾濫で拠点に攻め込んだ敵を出す。拠点のシーンにだけ置き、読み込んだときに SceneStarter が Bind する。
    /// 出した敵は拠点の中心を目指す。拠点のシーンを手放すときに生き残っている敵は位置と体力を控え、読み込み直したときに同じところへ出す
    /// </summary>
    public sealed class RaidGate : MonoBehaviour
    {
        /// <summary>円が場からはみ出すとき、場に入る点を引き直す回数。尽きたら円の中心を場へ丸める</summary>
        private const int PickTries = 16;

        /// <summary>地面を探し始める高さと深さ。湧き口のまわりに出す敵の足元を探す</summary>
        private const float SeekFrom = 5f;
        private const float SeekDepth = 10f;

        /// <summary>見つけた地面から、敵を出す高さ</summary>
        private const float SpawnLift = 0.6f;

        [Tooltip("湧き口からのばらつき。氾濫で来た敵は、この半径の中に出す")]
        [SerializeField, FormerlySerializedAs("scatter")] private float _scatter = 1.5f;

        [Tooltip("攻め込んでくる敵。どの根源の氾濫でも同じ")]
        [SerializeField, FormerlySerializedAs("enemy")] private GameObject _enemy;

        /// <summary>ここから出した敵。倒されて消えたものは null になる</summary>
        private readonly List<GameObject> _raiders = new List<GameObject>();

        private EnemyContext _context;

        /// <summary>場の範囲。拠点の場所が見つからなければ null</summary>
        private FieldBounds _bounds;

        /// <summary>攻め込んだ敵が目指す、拠点の中心。場の範囲の真ん中</summary>
        private Vector3 _center;

        /// <summary>拠点の場所。控える敵の位置は、ここの原点からの位置で持つ。見つからなければ null</summary>
        private Area _home;

        /// <summary>
        /// 拠点のシーンを読み込んだとき。常駐シーンのものを受け取り、留守のあいだに攻め込んだ敵と、離れる前に生き残っていた敵を出す
        /// </summary>
        public void Bind(EnemyContext context)
        {
            _context = context;

            _home = Area.In(gameObject.scene);
            if (_home != null) _bounds = Exposition.ToBounds(_home.Exposition);
            _center = _bounds != null ? new Vector3(_bounds.CenterX, transform.position.y, _bounds.CenterZ) : transform.position;

            _context.Travel.Unloading += Retreat;

            int arrived = Release();
            if (arrived > 0) StartCoroutine(Announce(arrived));
        }

        private void OnDestroy()
        {
            if (_context != null) _context.Travel.Unloading -= Retreat;
        }

        private void Update()
        {
            if (_context == null) return;

            //移動のあいだは出さない。離れる途中に出すと、拠点のシーンと一緒に消える
            if (_context.Travel.IsTraveling) return;

            //居るあいだに氾濫で攻め込んだ敵は、すぐ出す
            if (_context.Roots.Raid.Kept.Count > 0) Release();
        }

        /// <summary>暗転が明けてから知らせる。覆われているあいだに出すと見えない</summary>
        private IEnumerator Announce(int count)
        {
            while (_context.Travel.IsTraveling) yield return null;

            _context.MiddleText.Midtxt_Anim("拠点に敵が " + count + " 体入り込んでいます");
        }

        /// <summary>控えている敵をすべて出す。出した数を返す</summary>
        private int Release()
        {
            List<Raider> kept = _context.Roots.Raid.TakeAll();
            foreach (Raider raider in kept) Spawn(raider);

            return kept.Count;
        }

        /// <summary>拠点のシーンを手放す。生き残っている敵はシーンと一緒に消えるので、位置と体力を控える</summary>
        private void Retreat(Area area)
        {
            if (area.Scene != gameObject.scene) return;

            foreach (Raider raider in Survivors()) _context.Roots.Raid.Keep(raider);
            _raiders.Clear();
        }

        /// <summary>生き残っている敵の位置と体力。拠点に居るあいだに保存するときも写す。位置は拠点の原点からの位置</summary>
        public List<Raider> Survivors()
        {
            List<Raider> alive = new List<Raider>();
            foreach (GameObject raider in _raiders)
            {
                if (!IsAlive(raider)) continue;

                Vector3 at = _home != null ? _home.ToLocal(raider.transform.position) : raider.transform.position;
                IDamageable damageable = raider.GetComponent<IDamageable>();
                alive.Add(new Raider(at.x, at.y, at.z, damageable != null ? damageable.CurrentHp : Raider.FullHp));
            }

            return alive;
        }

        /// <summary>倒されて消えたもの、倒れて消える途中のもの、場外へ落ちたものは数えない</summary>
        private static bool IsAlive(GameObject raider)
        {
            if (raider == null || !raider.activeSelf) return false;

            IDamageable damageable = raider.GetComponent<IDamageable>();
            return damageable == null || damageable.CurrentHp > 0;
        }

        /// <summary>控えていた 1 体を出す。位置を持つものはその位置に、持たないものは湧き口のまわりに出す。体力は控えた値まで減らす</summary>
        private void Spawn(Raider kept)
        {
            Vector3 at;
            if (kept.HasPosition)
            {
                at = new Vector3(kept.X, kept.Y, kept.Z);
                if (_home != null) at = _home.ToWorld(at);
            }
            else
            {
                at = PickInField(transform.position, _scatter);

                //地面が見つからなければ高さを変えずに出す。攻め込んだ敵を取りこぼさない
                if (Physics.Raycast(at + Vector3.up * SeekFrom, Vector3.down, out RaycastHit hit, SeekDepth)) at = hit.point + Vector3.up * SpawnLift;
            }

            //拠点に対応する根源は無いため、攻略度による弱体化は行わない
            GameObject raider = Instantiate(_enemy, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(raider, gameObject.scene);

            Slime slime = raider.GetComponent<Slime>();
            slime.Bind(_context, null);
            slime.SetGoal(_center);

            IDamageable damageable = raider.GetComponent<IDamageable>();
            if (damageable != null && kept.Hp > Raider.FullHp && kept.Hp < damageable.CurrentHp)
            {
                damageable.TakeDamage(damageable.CurrentHp - kept.Hp);
            }

            _raiders.Add(raider);
        }

        /// <summary>around から radius の円の中で、場に入る点。はみ出した点を丸めると場の縁に一列に並ぶので、引き直す</summary>
        private Vector3 PickInField(Vector3 around, float radius)
        {
            for (int tries = 0; tries < PickTries; tries++)
            {
                Vector2 offset = Random.insideUnitCircle * radius;
                Vector3 at = around + new Vector3(offset.x, 0, offset.y);
                if (_bounds == null || _bounds.Contains(at.x, at.z)) return at;
            }

            return new Vector3(_bounds.ClampX(around.x), around.y, _bounds.ClampZ(around.z));
        }
    }
}
