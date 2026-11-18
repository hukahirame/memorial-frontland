using TMPro;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>画面に出す所持金の額</summary>
    public sealed class CoinLabel : MonoBehaviour
    {
        [SerializeField] private Purse _purse;
        [SerializeField] private TextMeshProUGUI _label;

        private void OnEnable()
        {
            _purse.Coins.Changed += Show;
            Show();
        }

        private void OnDisable()
        {
            _purse.Coins.Changed -= Show;
        }

        private void Show()
        {
            _label.text = _purse.Coins.Amount.ToString();
        }
    }
}
