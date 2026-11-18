using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所1つ。場所のシーンのルートに1つだけ置く。場所の Id はシーン名。
    /// </summary>
    public sealed class Area : MonoBehaviour
    {
        [SerializeField] private AreaKind kind;

        [Tooltip("出口を通ってきたときに立つ地点。空ならこの GameObject の位置")]
        [SerializeField] private Transform entrance;

        [Tooltip("奥の出口を通ってきたときに立つ地点。出口が 1 つの場所は空のままでよく、どちらから来ても入口に立つ")]
        [SerializeField] private Transform farEntrance;

        [Tooltip("始めたときと倒れたあとに立つ地点。空なら入口")]
        [SerializeField] private Transform home;

        [Tooltip("場所を世界のどこに置いたか。場所の物はこれだけずらして置いてある。場所を並べる道具（Tools/World）が書く")]
        [SerializeField] private Vector3 origin;

        [Tooltip("場の範囲。XL, XS, ZL, ZS の順。場所の原点からの位置")]
        [SerializeField] private Vector4 exposition;

        [Tooltip("根源の核から湧く敵。根源では空にしない")]
        [SerializeField] private GameObject enemy;

        [Tooltip("プレイヤーがこれより高く上がったら場の外とみなし、連れ戻す。2 階建ての場所では、上の階に立てる高さにする")]
        [SerializeField] private float ceiling = FieldBounds.EscapeHeight;

        /// <summary>常駐シーンが来るまで止めているルート。止める前から非アクティブだったものは入れない</summary>
        private readonly List<GameObject> _held = new List<GameObject>();

        public string Id
        {
            get { return gameObject.scene.name; }
        }

        public AreaKind Kind
        {
            get { return kind; }
        }

        /// <summary>この場所にある根源の Id。根源でない場所なら null</summary>
        public string RootId
        {
            get { return TravelRule.RootOfArea(Id, kind); }
        }

        /// <summary>場所を世界のどこに置いたか。保存する座標は、ここからの位置で持つ</summary>
        public Vector3 Origin
        {
            get { return origin; }
        }

        /// <summary>場の範囲。世界の座標で、XL, XS, ZL, ZS の順</summary>
        public Vector4 Exposition
        {
            get { return new Vector4(exposition.x + origin.x, exposition.y + origin.x, exposition.z + origin.z, exposition.w + origin.z); }
        }

        /// <summary>プレイヤーを場の外とみなす高さ。世界の座標</summary>
        public float Ceiling
        {
            get { return ceiling + origin.y; }
        }

        /// <summary>世界の座標を、この場所の原点からの位置にする。保存に書くときに使う</summary>
        public Vector3 ToLocal(Vector3 world)
        {
            return world - origin;
        }

        /// <summary>この場所の原点からの位置を、世界の座標にする。保存から戻すときに使う</summary>
        public Vector3 ToWorld(Vector3 local)
        {
            return local + origin;
        }

        /// <summary>世界の座標の点が、場の範囲（横と奥行き）の中にあるか</summary>
        public bool Covers(Vector3 world)
        {
            Vector4 bounds = Exposition;
            return world.x <= bounds.x && world.x >= bounds.y && world.z <= bounds.z && world.z >= bounds.w;
        }

        /// <summary>根源の核から湧く敵の prefab。根源でない場所は null</summary>
        public GameObject Enemy
        {
            get { return enemy; }
        }

        public Scene Scene
        {
            get { return gameObject.scene; }
        }

        /// <summary>着いたときに立つ位置</summary>
        public Vector3 PointFor(Arrival arrival)
        {
            Transform point = entrance;
            if (arrival == Arrival.Home && home != null) point = home;
            if (arrival == Arrival.FromNext && farEntrance != null) point = farEntrance;

            return (point != null ? point : transform).position;
        }

        /// <summary>この場所のシーンにある、その種類の出口。無ければ null</summary>
        public Exit ExitOf(ExitKind exitKind)
        {
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                foreach (Exit exit in root.GetComponentsInChildren<Exit>(true))
                {
                    if (exit.Kind == exitKind) return exit;
                }
            }

            return null;
        }

        /// <summary>この場所のシーンにある T。止めてある物も探す。無ければ null</summary>
        public T Find<T>() where T : Component
        {
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                T found = root.GetComponentInChildren<T>(true);
                if (found != null) return found;
            }

            return null;
        }

        /// <summary>この場所のシーンにある T すべて。止めてある物も探す</summary>
        public List<T> FindAll<T>() where T : Component
        {
            List<T> found = new List<T>();
            foreach (GameObject root in Scene.GetRootGameObjects()) found.AddRange(root.GetComponentsInChildren<T>(true));

            return found;
        }

        /// <summary>シーンのルートに置かれた場所。無ければ null</summary>
        public static Area In(Scene scene)
        {
            if (!scene.IsValid() || !scene.isLoaded) return null;

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Area area = root.GetComponent<Area>();
                if (area != null) return area;
            }

            return null;
        }

        /// <summary>シーンのほかのルートを止める。プレイヤーの居ないうちに Start が走るのを防ぐ</summary>
        public void Hold()
        {
            foreach (GameObject root in Scene.GetRootGameObjects())
            {
                if (root == gameObject || !root.activeSelf) continue;

                root.SetActive(false);
                _held.Add(root);
            }
        }

        /// <summary>止めたルートを戻す</summary>
        public void Release()
        {
            foreach (GameObject root in _held)
            {
                if (root != null) root.SetActive(true);
            }

            _held.Clear();
        }
    }
}
