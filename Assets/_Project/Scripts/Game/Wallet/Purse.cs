using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>所持金の持ち主。常駐シーンに 1 つ置く</summary>
    public sealed class Purse : MonoBehaviour
    {
        [SerializeField] private int _startingCoins = 1000;

        public Wallet Coins { get; } = new Wallet();

        private void Awake()
        {
            Coins.SetAmount(_startingCoins);
        }
    }
}
