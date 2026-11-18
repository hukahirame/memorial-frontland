using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>地図の根源 1 つの札。押すと根源の詳細を開く</summary>
    public sealed class RootCard : MonoBehaviour
    {
        private string _rootId;
        private RootDetail _detail;

        /// <summary>札を並べる側が渡す</summary>
        public void Bind(string rootId, RootDetail detail)
        {
            _rootId = rootId;
            _detail = detail;
        }

        /// <summary>ボタンから呼ぶ</summary>
        public void Open()
        {
            _detail.Show(_rootId);
        }
    }
}
