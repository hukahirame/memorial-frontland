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

        /// <summary>いま居る場所に置く。場所に居ないときと prefab が無いときは置かずに false</summary>
        public bool Place(string itemId, Vector3 position)
        {
            Area area = travel.Current;
            if (area == null) return false;

            GameObject prefab = Resources.Load<GameObject>(itemId + PrefabSuffix);
            if (prefab == null) return false;

            GameObject placed = Instantiate(prefab, position, Quaternion.identity);
            placed.name = prefab.name;
            SceneManager.MoveGameObjectToScene(placed, area.Scene);
            _placed.Add(new KeyValuePair<string, GameObject>(itemId, placed));

            return true;
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

        private void Restore(Area area)
        {
            foreach (Placement placement in Ledger.In(area.Id))
            {
                Place(placement.ItemId, new Vector3(placement.X, placement.Y, placement.Z));
            }
        }

        /// <summary>置いた物のうち、まだ在るもの。壊されて消えたものは入れない</summary>
        private List<Placement> Remaining()
        {
            List<Placement> remaining = new List<Placement>();

            foreach (KeyValuePair<string, GameObject> pair in _placed)
            {
                if (pair.Value == null) continue;

                Vector3 position = pair.Value.transform.position;
                remaining.Add(new Placement(pair.Key, position.x, position.y, position.z));
            }

            return remaining;
        }
    }
}
