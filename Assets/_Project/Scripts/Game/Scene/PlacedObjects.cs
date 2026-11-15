using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所に置いた物。置くときはここを通す。場所のシーンを手放すときに控えへ書き、読み込んだときに置き直す。
    /// 置いた物は場所のシーンに入れ、場所と一緒に消える。
    /// </summary>
    public sealed class PlacedObjects : MonoBehaviour
    {
        /// <summary>置いた物の prefab は Resources に「アイテム ID + この接尾辞」の名前で置く</summary>
        public const string PrefabSuffix = "_obj";

        [SerializeField] private AreaTravel travel;

        /// <summary>読み込んである場所ごとの、置いてある物とそのアイテム ID。置いた物が無い場所も入れる</summary>
        private readonly Dictionary<string, List<KeyValuePair<string, GameObject>>> _placed =
            new Dictionary<string, List<KeyValuePair<string, GameObject>>>();

        /// <summary>読み込んである場所。控えの位置は場所の原点からの位置で持つので、書くときに原点を引く</summary>
        private readonly Dictionary<string, Area> _areas = new Dictionary<string, Area>();

        public PlacementLedger Ledger { get; } = new PlacementLedger();

        private void Awake()
        {
            travel.Unloading += Record;
            travel.Loaded += Restore;
        }

        private void OnDestroy()
        {
            travel.Unloading -= Record;
            travel.Loaded -= Restore;
        }

        /// <summary>置いた物の prefab。置けない品なら null</summary>
        public static GameObject Prefab(string itemId)
        {
            return Resources.Load<GameObject>(itemId + PrefabSuffix);
        }

        /// <summary>
        /// いま居る場所に、y 軸まわりに yaw 度回して置く。場所に居ないとき、居る場所を手放している途中、prefab が無いときは置かずに false
        /// </summary>
        public bool Place(string itemId, Vector3 position, float yaw = 0f)
        {
            Area area = travel.Current;
            if (area == null || !_placed.ContainsKey(area.Id)) return false;

            return Spawn(area, itemId, position, yaw) != null;
        }

        private GameObject Spawn(Area area, string itemId, Vector3 position, float yaw)
        {
            GameObject prefab = Prefab(itemId);
            if (prefab == null) return null;

            GameObject placed = Instantiate(prefab, position, Quaternion.Euler(0f, yaw, 0f));
            placed.name = prefab.name;
            SceneManager.MoveGameObjectToScene(placed, area.Scene);
            In(area.Id).Add(new KeyValuePair<string, GameObject>(itemId, placed));

            return placed;
        }

        private List<KeyValuePair<string, GameObject>> In(string areaId)
        {
            if (!_placed.TryGetValue(areaId, out List<KeyValuePair<string, GameObject>> placed))
            {
                placed = new List<KeyValuePair<string, GameObject>>();
                _placed[areaId] = placed;
            }

            return placed;
        }

        /// <summary>読み込んである場所に置いてある物を控えへ書く。保存の前に呼ぶ</summary>
        public void Flush()
        {
            foreach (KeyValuePair<string, List<KeyValuePair<string, GameObject>>> area in _placed)
            {
                Ledger.Record(area.Key, Remaining(area.Value, _areas[area.Key]));
            }
        }

        private void Record(Area area)
        {
            Ledger.Record(area.Id, Remaining(In(area.Id), area));
            _placed.Remove(area.Id);
            _areas.Remove(area.Id);
        }

        //柵は控えた耐久に、離れていたあいだに戻るぶんを足して戻す。倒れていたかも戻す。控えていなければ満タンで立つ
        private void Restore(Area area)
        {
            In(area.Id);
            _areas[area.Id] = area;

            foreach (Placement placement in Ledger.In(area.Id))
            {
                Vector3 position = area.ToWorld(new Vector3(placement.X, placement.Y, placement.Z));
                GameObject placed = Spawn(area, placement.ItemId, position, placement.Yaw);
                if (placed == null || placement.Durability == Placement.NoDurability) continue;

                if (placed.TryGetComponent(out Fence fence))
                    fence.Restore(placement.Durability, placement.Fallen, Time.time - placement.RecordedAt);
            }
        }

        /// <summary>
        /// 置いた物のうち、まだ在るもの。壊されて消えたものは入れない。柵は残りの耐久と控えた時刻と倒れているかも控える。
        /// 位置は場所の原点からの位置で控える
        /// </summary>
        private static List<Placement> Remaining(List<KeyValuePair<string, GameObject>> placed, Area area)
        {
            List<Placement> remaining = new List<Placement>();

            foreach (KeyValuePair<string, GameObject> pair in placed)
            {
                if (pair.Value == null) continue;

                Transform shown = pair.Value.transform;
                Vector3 position = area.ToLocal(shown.position);
                bool isFence = pair.Value.TryGetComponent(out Fence fence);
                remaining.Add(new Placement(pair.Key, position.x, position.y, position.z, shown.eulerAngles.y,
                                            isFence ? fence.Durability : Placement.NoDurability, Time.time, isFence && fence.IsFallen));
            }

            return remaining;
        }
    }
}
