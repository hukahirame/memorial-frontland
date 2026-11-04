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

        [Tooltip("場の範囲。XL, XS, ZL, ZS の順")]
        [SerializeField] private Vector4 exposition;

        [Tooltip("根源の核から湧く敵。空なら緑のスライム（Resources の Slime）")]
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

        public Vector4 Exposition
        {
            get { return exposition; }
        }

        /// <summary>プレイヤーを場の外とみなす高さ</summary>
        public float Ceiling
        {
            get { return ceiling; }
        }

        /// <summary>根源の核から湧く敵の prefab。決めていない場所は null</summary>
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
