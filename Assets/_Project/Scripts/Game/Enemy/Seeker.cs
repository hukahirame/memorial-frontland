using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>湧く地面を探す目印。根源のシーンに 1 つ置き、核（RootCore）が子にして使う</summary>
    public sealed class Seeker : MonoBehaviour
    {
        /// <summary>湧く地面を探し始める高さ。核の候補が決めていなければこれ（1 階建ての場所）</summary>
        public const float DefaultFrom = 5f;

        /// <summary>湧く地面を探す深さ。核の候補が決めていなければこれ</summary>
        public const float DefaultDepth = 10f;

        /// <summary>真下へ探す前に下げる量。探し始めの高さに置いた物に当たらないように</summary>
        private const float StartStep = 0.05f;

        /// <summary>見つけた地面から、敵を出す高さ</summary>
        private const float SpawnLift = 0.6f;

        private void Start()
        {
            transform.position += Vector3.up;
        }

        /// <summary>
        /// 親の核のまわり range の中で、高さ from から真下へ depth まで地面を探す。見つかれば敵を出す点、無ければ null。
        /// 起点は親の核。自分の位置は前に探した所に残っている
        /// </summary>
        public Vector3? FindGround(float range, FieldBounds bounds, float from, float depth)
        {
            Vector3 origin = transform.parent.position;
            float x = Random.Range(bounds.ClampX(origin.x - range), bounds.ClampX(origin.x + range));
            float z = Random.Range(bounds.ClampZ(origin.z - range), bounds.ClampZ(origin.z + range));

            transform.position = new Vector3(x, from, z) + Vector3.down * StartStep;

            //地面に当たらなければ湧かせない。外れても hit.point は原点で埋まっている
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, depth)) return hit.point + Vector3.up * SpawnLift;

            Debug.Log("スポーン接地失敗");
            return null;
        }
    }
}
