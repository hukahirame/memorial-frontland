using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>地図。根源ごとの札を地図の座標に並べ、拠点へ戻る近道のボタンを出す</summary>
    public sealed class RootMap : MonoBehaviour
    {
        /// <summary>札の 2 行目（危険度と攻略度）の字の大きさ。1 行目の名前は prefab の大きさで出す</summary>
        private const int StatsFontSize = 22;

        [SerializeField] private RootsManager _roots;
        [SerializeField] private RootDetail _detail;

        [Tooltip("根源 1 つの札")]
        [SerializeField, FormerlySerializedAs("rootset")] private GameObject _cardPrefab;

        [Tooltip("地図に出す、拠点へ戻る近道のボタン")]
        [SerializeField, FormerlySerializedAs("home")] private HomeShortcut _home;

        /// <summary>札を並べ直す。地図を開くたびに呼ぶ</summary>
        public void Show()
        {
            foreach (Root root in _roots.Roots.All)
            {
                Transform existing = transform.Find(root.Id);
                if (existing != null) Destroy(existing.gameObject);
            }

            //札は地図の中の座標で置く。画面の画素で置くと、画面が大きいほど札どうしが近づいて重なる
            foreach (Root root in _roots.Roots.All)
            {
                GameObject card = Instantiate(_cardPrefab, transform);
                ((RectTransform)card.transform).anchoredPosition = new Vector2(root.UiX, root.UiY);
                card.name = root.Id;
                card.transform.Find("Text").GetComponent<Text>().text = root.Name + "\n<size=" + StatsFontSize
                    + "><color=#F2E27A>危険度 " + root.Danger + "　攻略度 " + root.Progress + "％</color></size>";
                card.GetComponent<RootCard>().Bind(root.Id, _detail);
            }

            _home.Show();
        }
    }
}
