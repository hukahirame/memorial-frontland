using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>倒れたことを大きい文字で知らせる</summary>
    public sealed class PlayerNotices : MonoBehaviour
    {
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private BigText _bigText;

        private void OnEnable()
        {
            _health.Died += ShowDied;
        }

        private void OnDisable()
        {
            _health.Died -= ShowDied;
        }

        private void ShowDied()
        {
            _bigText.Show("体力が0になりました", "失敗");
        }
    }
}
