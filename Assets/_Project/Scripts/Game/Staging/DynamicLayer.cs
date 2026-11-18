using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 動く物の描く順を奥行き（z）から決める。z が 1 増えるごとに 100 ずつ奥へ描く。
    /// 動かない物には付けない。子どうしの順は、付けたときの描く順を足して保つ
    /// </summary>
    public sealed class DynamicLayer : MonoBehaviour
    {
        /// <summary>描く順を見直す間隔の秒数。毎コマでなくてよい</summary>
        private const float IntervalSeconds = 0.05f;

        /// <summary>z が 1 増えるごとの描く順</summary>
        private const int OrderPerDepth = -100;

        [Tooltip("描く順を変える絵。まとめて変えるときは使わない")]
        [SerializeField, FormerlySerializedAs("sr")] private SpriteRenderer _renderer;

        [Tooltip("自分には絵が無く、子の絵をまとめて変える")]
        [SerializeField, FormerlySerializedAs("isGroup")] private bool _isGroup;

        private float _lastZ;

        /// <summary>付けたときの描く順。奥行きから決めた順に足す</summary>
        private int _baseOrder;

        /// <summary>まとめて変えるときの、子の絵ごとの付けたときの描く順</summary>
        private int[] _baseOrders;

        private void Start()
        {
            _lastZ = transform.position.z;

            if (!_isGroup) _baseOrder = _renderer.sortingOrder;
            else
            {
                SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
                _baseOrders = new int[renderers.Length];
                for (int i = 0; i < renderers.Length; i++) _baseOrders[i] = renderers[i].sortingOrder;
            }

            StartCoroutine(KeepOrder());
        }

        private IEnumerator KeepOrder()
        {
            while (true)
            {
                Reorder();
                yield return new WaitForSeconds(IntervalSeconds);
            }
        }

        private void Reorder()
        {
            if (_lastZ == transform.position.z) return;

            _lastZ = transform.position.z;
            int order = Mathf.RoundToInt(_lastZ * OrderPerDepth);

            if (!_isGroup)
            {
                _renderer.sortingOrder = order + _baseOrder;
                return;
            }

            //子の絵は出し入れで数が変わる。付けたときより多い分は、付けたときの順を 0 として並べる
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = order + (i < _baseOrders.Length ? _baseOrders[i] : 0);
        }
    }
}
