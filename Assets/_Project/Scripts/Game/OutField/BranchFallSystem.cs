using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所のどこかに、ときどき枝を落とす。落ちる範囲は場所の原点からの位置で決める（場所は世界のどこかへずらして置いてある）。
    /// 落とした枝は場所のシーンに入れ、場所と一緒に消える
    /// </summary>
    public sealed class BranchFallSystem : MonoBehaviour
    {
        /// <summary>枝を落とす高さ。場所の原点からの高さ</summary>
        private const float DropHeight = 5f;

        [Tooltip("落ちる範囲の小さい側の角。x と z だけを使う")]
        [SerializeField, FormerlySerializedAs("minusrange")] private Vector3 _rangeMin;

        [Tooltip("落ちる範囲の大きい側の角。x と z だけを使う")]
        [SerializeField, FormerlySerializedAs("plusrange")] private Vector3 _rangeMax;

        [Tooltip("拾える品の入れ物")]
        [SerializeField, FormerlySerializedAs("dropcapsule")] private GameObject _dropPrefab;

        [Tooltip("枝の絵。絵の名前が品の ID になる")]
        [SerializeField, FormerlySerializedAs("branch")] private Sprite _branch;

        //Invoke ではなくコルーチンで回す。場所を手放すときは物を止めてからシーンを手放すので、止まったあいだは落とさない
        //（Invoke は止まった物でも呼ばれ、手放している最中のシーンへは物を入れられない）
        private void OnEnable()
        {
            StartCoroutine(DropAtIntervals());
        }

        private IEnumerator DropAtIntervals()
        {
            yield return new WaitForSeconds(Random.Range(10, 20));

            while (true)
            {
                Drop();
                yield return new WaitForSeconds(Random.Range(20, 36));
            }
        }

        private void Drop()
        {
            Area area = Area.In(gameObject.scene);
            Vector3 origin = area != null ? area.Origin : Vector3.zero;
            Vector3 at = origin + new Vector3(Random.Range(_rangeMin.x, _rangeMax.x), DropHeight, Random.Range(_rangeMin.z, _rangeMax.z));

            GameObject drop = Instantiate(_dropPrefab, at, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(drop, gameObject.scene);
            drop.GetComponent<SpriteRenderer>().sprite = _branch;
        }
    }
}
