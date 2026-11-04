using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所に置いた物。置くときはここを通す。場所を離れるときに控えへ書き、入るときに置き直す。
    /// 置いた物は場所のシーンに入れ、場所と一緒に消える。
    /// </summary>
    public sealed class PlacedObjects : MonoBehaviour
    {
        /// <summary>置いた物の prefab は Resources に「アイテム ID + この接尾辞」の名前で置く</summary>
        public const string PrefabSuffix = "_obj";

        [SerializeField] private AreaTravel travel;

        /// <summary>いま居る場所に置いてある物と、そのアイテム ID</summary>
        private readonly List<KeyValuePair<string, GameObject>> _placed =
            new List<KeyValuePair<string, GameObject>>();

        public PlacementLedger Ledger { get; } = new PlacementLedger();

        private void Awake()
        {
            travel.Leaving += Record;
            travel.Entered += Restore;
        }

        private void OnDestroy()
        {
            travel.Leaving -= Record;
            travel.Entered -= Restore;
        }

        /// <summary>置いた物の prefab。置けない品なら null</summary>
        public static GameObject Prefab(string itemId)
        {
            return Resources.Load<GameObject>(itemId + PrefabSuffix);
        }

        /// <summary>いま居る場所に、y 軸まわりに yaw 度回して置く。場所に居ないときと prefab が無いときは置かずに false</summary>
        public bool Place(string itemId, Vector3 position, float yaw = 0f)
        {
            return Spawn(itemId, position, yaw) != null;
        }

        private GameObject Spawn(string itemId, Vector3 position, float yaw)
        {
            Area area = travel.Current;
            if (area == null) return null;

            GameObject prefab = Prefab(itemId);
            if (prefab == null) return null;

            GameObject placed = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            placed.name = prefab.name;
            SceneManager.MoveGameObjectToScene(placed, area.Scene);
            _placed.Add(new KeyValuePair<string, GameObject>(itemId, placed));

            return placed;
        }

        /// <summary>いま居る場所に置いてある物を控えへ書く。保存の前に呼ぶ</summary>
        public void Flush()
        {
            if (travel.Current == null) return;

            Ledger.Record(travel.Current.Id, Remaining());
        }

        private void Record(Area area)
        {
            Ledger.Record(area.Id, Remaining());
            _placed.Clear();
        }

        //柵は控えた耐久に、離れていたあいだに戻るぶんを足して戻す。倒れていたかも戻す。控えていなければ満タンで立つ
        private void Restore(Area area)
        {
            foreach (Placement placement in Ledger.In(area.Id))
            {
                GameObject placed = Spawn(placement.ItemId, new Vector3(placement.X, placement.Y, placement.Z), placement.Yaw);
                if (placed == null || placement.Durability == Placement.NoDurability) continue;

                if (placed.TryGetComponent(out Fence fence))
                    fence.Restore(placement.Durability, placement.Fallen, Time.time - placement.RecordedAt);
            }
        }

        /// <summary>置いた物のうち、まだ在るもの。壊されて消えたものは入れない。柵は残りの耐久と控えた時刻と倒れているかも控える</summary>
        private List<Placement> Remaining()
        {
            List<Placement> remaining = new List<Placement>();

            foreach (KeyValuePair<string, GameObject> pair in _placed)
            {
                if (pair.Value == null) continue;

                Transform placed = pair.Value.transform;
                Vector3 position = placed.position;
                bool isFence = pair.Value.TryGetComponent(out Fence fence);
                remaining.Add(new Placement(pair.Key, position.x, position.y, position.z, placed.eulerAngles.y,
                                            isFence ? fence.Durability : Placement.NoDurability, Time.time, isFence && fence.IsFallen));
            }

            return remaining;
        }
    }
}
