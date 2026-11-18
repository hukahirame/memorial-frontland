using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>作業台の画面の品のボタン。物の名前が品の ID</summary>
    public sealed class CraftButton : MonoBehaviour
    {
        [SerializeField, FormerlySerializedAs("craft")] private CraftMenu _menu;

        /// <summary>ボタンから呼ぶ</summary>
        public void Select()
        {
            _menu.Show(gameObject.name);
        }
    }
}
