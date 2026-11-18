using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// Allmaity ボタン。そばにある物に応じて出るボタン（作業台を使う・柵を壊すなど）。
    /// 子に並べた空きの枠を使い回し、出しているあいだは枠の名前で引く。空きが無ければ出さない
    /// </summary>
    public sealed class Allmaity : MonoBehaviour
    {
        /// <summary>空きの枠の名前の頭。空けると、これに並びの番号を付けた名前へ戻す</summary>
        private const string FreePrefix = "AllmaityButton";

        [Tooltip("画面を開くボタンで、開いた画面を置く所")]
        [SerializeField, FormerlySerializedAs("rect")] private RectTransform _center;

        private readonly Dictionary<string, Action> _presses = new Dictionary<string, Action>();

        /// <summary>同じ親の下にある panel という名前の画面を、開くボタンを出す</summary>
        public void ShowOpener(string panel, string label)
        {
            Show(panel, label, () => Open(panel));
        }

        /// <summary>id の名前で、字が label のボタンを出す。押すと press が走る。出ていれば何もしない</summary>
        public void Show(string id, string label, Action press)
        {
            Transform free = null;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name == id) return;
                if (free == null && child.name.StartsWith(FreePrefix)) free = child;
            }
            if (free == null) return;

            free.localScale = Vector3.one;
            free.name = id;
            free.Find("Text").GetComponent<Text>().text = label;
            _presses[id] = press;
        }

        public void Hide(string id)
        {
            Transform button = transform.Find(id);
            if (button == null) return;

            button.localScale = Vector3.zero;
            button.name = FreePrefix + "_" + button.GetSiblingIndex();
            _presses.Remove(id);
        }

        /// <summary>並びの index 番目のボタンが押された。枠の OnClick から呼ぶ</summary>
        public void Press(int index)
        {
            string id = transform.GetChild(index).name;
            if (_presses.TryGetValue(id, out Action press)) press();
        }

        private void Open(string panel)
        {
            transform.parent.Find(panel).position = _center.position;
        }
    }
}
