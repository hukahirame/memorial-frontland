using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>持ち物と地図を横に滑らせて入れ替えるボタン。持ち物の側と地図の側に 1 つずつ置き、互いを知る</summary>
    public sealed class ExchangeButton : MonoBehaviour
    {
        /// <summary>持ち物と地図が入れ替わりきるまでの秒数</summary>
        public const float SlideSeconds = 0.3f;

        /// <summary>入れ替えたあと、出ていった画面を置いておく所</summary>
        private static readonly Vector3 InventoryAway = new Vector3(0, 5000, 0);
        private static readonly Vector3 MapAway = new Vector3(-1000, 5000, 0);

        [SerializeField, FormerlySerializedAs("pi")] private RectTransform _inventory;
        [SerializeField, FormerlySerializedAs("rm")] private RectTransform _map;
        [SerializeField, FormerlySerializedAs("center")] private RectTransform _center;
        [SerializeField] private RootMap _rootMap;

        [Tooltip("地図の上に開く根源の詳細を閉じるもの。持ち物へ移るときに閉じる")]
        [SerializeField] private CloseManager _detailClose;

        [Tooltip("持ち物の側のボタンなら真。押すと地図へ移る")]
        [SerializeField, FormerlySerializedAs("is_pi")] private bool _fromInventory = true;

        [Tooltip("もう一方の側のボタン。重ねて動かすと、持ち物も地図も画面の外へ送られる")]
        [SerializeField] private ExchangeButton _other;

        private bool _isMoving;

        /// <summary>ボタンから呼ぶ</summary>
        public void Exchange()
        {
            if (_isMoving || (_other != null && _other._isMoving && _other.isActiveAndEnabled)) return;

            if (_fromInventory)
            {
                StartCoroutine(Run(_map, _inventory, Vector3.right, InventoryAway));
                _rootMap.Show();
                TempAudio.TempAudioPlay("Fantasy_Game_Action_Book_Page_Turn_2");
            }
            else
            {
                //根源の詳細は地図の上に開く。開いたまま持ち物へ移ると、詳細だけが持ち物の上に残るので、地図と一緒に閉じる
                if (_detailClose != null) _detailClose.CloseClick();
                StartCoroutine(Run(_inventory, _map, Vector3.left, MapAway));
                TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
            }
        }

        private IEnumerator Run(RectTransform coming, RectTransform going, Vector3 direction, Vector3 away)
        {
            _isMoving = true;
            yield return Slide(coming, going, direction);
            going.position = away;
            _isMoving = false;
        }

        //入ってくる方を画面 1 つ分外から真ん中へ、出ていく方を真ん中から画面の外へ送る。
        //描画の速さに関わらず同じ秒数で終え、終わり際をゆっくりにする。時間止めの間も進める
        private IEnumerator Slide(RectTransform coming, RectTransform going, Vector3 direction)
        {
            Vector3 distance = direction * Screen.width;
            Vector3 comingFrom = _center.position - distance;
            Vector3 goingFrom = going.position;

            for (float elapsed = 0f; elapsed < SlideSeconds; elapsed += Time.unscaledDeltaTime)
            {
                float eased = 1f - Mathf.Pow(1f - elapsed / SlideSeconds, 3f);
                coming.position = comingFrom + distance * eased;
                going.position = goingFrom + distance * eased;
                yield return null;
            }

            coming.position = _center.position;
        }
    }
}
