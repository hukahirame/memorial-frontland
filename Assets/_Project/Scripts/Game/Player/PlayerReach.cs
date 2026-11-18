using System.Collections.Generic;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 手の届くもの。作業台と柵のそばでは Allmaity にボタンを出し、出口に入ったら隣の場所へ移り、落ちた品は持ち物へ入れる。
    /// 剣や視界確保の当たりもプレイヤーの子で、その出入りもここへ届く。作業台と出口は体の固い当たりが重なったときだけ数える
    /// </summary>
    public sealed class PlayerReach : MonoBehaviour
    {
        private const string CraftBenchName = "CraftBench";

        /// <summary>作業台のボタンで開く画面。Allmaity のボタンの名前も兼ねる</summary>
        private const string CraftPanel = "CraftInventory";

        /// <summary>柵を壊す Allmaity ボタンの名前</summary>
        private const string FenceButton = "Fence";

        [SerializeField] private Allmaity _allmaity;
        [SerializeField] private AreaTravel _travel;

        [Tooltip("落ちた品を拾って入れる先")]
        [SerializeField] private PlayerInventory _inventory;

        /// <summary>手の届く範囲にある柵。「柵を壊す」で、いちばん近いものを壊す</summary>
        private readonly List<Fence> _fencesInReach = new List<Fence>();

        private PlayerMovement _movement;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
        }

        private void Update()
        {
            ForgetGoneFences();
        }

        /// <summary>落ちた品を拾って持ち物に入れる。入る枠が無ければ拾わずに false</summary>
        public bool PickUp(string itemId, int durability)
        {
            return _inventory.LoadInventory(itemId, durability) == 1;
        }

        //入るときは外接する箱で見る（入った瞬間の浅い重なりも取りこぼさない）。出るときは形で見る（剣だけが抜けても消さない）
        private void OnTriggerEnter(Collider other)
        {
            Collider body = _movement.Body;
            if (body == null || !body.enabled || !body.bounds.Intersects(other.bounds)) return;

            if (other.gameObject.name == CraftBenchName) _allmaity.ShowOpener(CraftPanel, "作業台を使う");
            if (other.TryGetComponent(out Exit exit)) EnterExit(exit);

            Fence fence = FenceOf(other);
            if (fence != null && !_fencesInReach.Contains(fence))
            {
                _fencesInReach.Add(fence);
                _allmaity.Show(FenceButton, "柵を壊す", DemolishFence);
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.gameObject.name == CraftBenchName && !IsBodyInside(other)) _allmaity.Hide(CraftPanel);

            Fence fence = FenceOf(other);
            if (fence != null && !IsBodyInside(other))
            {
                _fencesInReach.Remove(fence);
                if (_fencesInReach.Count == 0) _allmaity.Hide(FenceButton);
            }
        }

        /// <summary>other が柵の手の届く範囲の当たりなら、その柵</summary>
        private static Fence FenceOf(Collider other)
        {
            Fence fence = other.GetComponentInParent<Fence>();
            return fence != null && fence.Reach == other ? fence : null;
        }

        /// <summary>手の届く柵のうち、いちばん近いものを壊す。Allmaity の「柵を壊す」から呼ぶ</summary>
        private void DemolishFence()
        {
            ForgetGoneFences();

            Fence nearest = null;
            foreach (Fence fence in _fencesInReach)
            {
                if (nearest == null || FlatDistance(fence) < FlatDistance(nearest)) nearest = fence;
            }
            if (nearest == null) return;

            nearest.Demolish();
            _fencesInReach.Remove(nearest);
            if (_fencesInReach.Count == 0) _allmaity.Hide(FenceButton);
        }

        private float FlatDistance(Fence fence)
        {
            Vector3 offset = fence.transform.position - transform.position;
            return new Vector2(offset.x, offset.z).magnitude;
        }

        //場所を移ると、手の届いていた柵は場所ごと消え、出たことは届かない。消えた柵は忘れ、無くなったらボタンも消す
        private void ForgetGoneFences()
        {
            if (_fencesInReach.Count == 0 || _fencesInReach.RemoveAll(fence => fence == null) == 0) return;

            if (_fencesInReach.Count == 0) _allmaity.Hide(FenceButton);
        }

        private bool IsBodyInside(Collider other)
        {
            Collider body = _movement.Body;
            return body != null && body.enabled && Physics.ComputePenetration(
                body, body.transform.position, body.transform.rotation,
                other, other.transform.position, other.transform.rotation, out _, out _);
        }

        /// <summary>
        /// 出口に入ったら、受注にかかわらず隣の場所へ移る。隣が読み込んであれば、そのまま歩いて境目を越える（AreaTravel）。
        /// まだ読み込めていなければ、覆って移る
        /// </summary>
        private void EnterExit(Exit exit)
        {
            //根源の並びは、常駐シーンの SceneStarter が起動のときに渡す
            IReadOnlyList<string> rootIds = _travel.Lineup?.Invoke();
            if (rootIds == null || _travel.CanWalk(exit.Kind, rootIds)) return;

            _travel.Through(exit.Kind, rootIds);
        }
    }
}
