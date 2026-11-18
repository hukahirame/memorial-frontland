using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 当てた所に出す、与えた量の数字。ゆっくり昇って消える。量が 100 増えるごとに 1 秒長く残る
    /// </summary>
    public sealed class DamageNumber : MonoBehaviour
    {
        /// <summary>1 秒に昇る高さ</summary>
        private const float RiseSpeed = 0.1f;

        [SerializeField, FormerlySerializedAs("txt")] private Text _text;

        private Vector3 _origin;
        private float _elapsed;
        private float _lifetime;
        private bool _isShown;

        public void Show(int damage)
        {
            _origin = transform.position;
            _lifetime = 1 + damage / 100;
            _text.text = damage.ToString();
            _isShown = true;
        }

        private void Update()
        {
            if (!_isShown) return;

            _elapsed += Time.deltaTime;
            transform.position = _origin + new Vector3(0f, _elapsed * RiseSpeed, 0f);

            if (_elapsed > _lifetime) Destroy(gameObject);
        }
    }
}
