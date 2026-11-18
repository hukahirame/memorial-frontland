using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 受注中の調査・決壊クエストを、画面の左上に小さく出す。受注していなければ出さない。
    /// 受注・解除・達成・受け取り・読み込みのどれで変わっても拾えるよう、毎コマ受注の様子を見て、変わったときだけ書き直す
    /// </summary>
    public sealed class QuestTracker : MonoBehaviour
    {
        /// <summary>出し入れにかける秒数</summary>
        private const float FadeSeconds = 0.25f;

        /// <summary>札の左右の、文字の端から札の端までの幅。札の両端はとがっている</summary>
        private const float Padding = 44f;

        /// <summary>札のいちばん短い幅</summary>
        private const float MinWidth = 360f;

        private const string StateColor = "#F2C46D";
        private const string ObjectiveColor = "#D9D2C3";

        [SerializeField] private QuestManager _quests;
        [SerializeField] private RootsManager _roots;
        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField, FormerlySerializedAs("text")] private TextMeshProUGUI _text;

        private CanvasGroup _group;
        private RectTransform _plate;
        private string _shownKey;
        private float _target;

        private void Awake()
        {
            _plate = (RectTransform)transform;

            //出ていないあいだも出ているあいだも、札の下のボタンを押せるようにする
            _group = GetComponent<CanvasGroup>();
            if (_group == null) _group = gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;
        }

        private void Update()
        {
            Quest quest = _quests.Quests.Find(_quests.OrderedId);
            string key = Key(quest);
            if (key != _shownKey)
            {
                _shownKey = key;
                if (quest != null) Write(quest);
                _target = quest != null ? 1f : 0f;
            }

            _group.alpha = Mathf.MoveTowards(_group.alpha, _target, Time.unscaledDeltaTime / FadeSeconds);
        }

        /// <summary>出す中身が変わったかを見るための鍵。受注・進み・居る根源のどれかが変われば変わる</summary>
        private string Key(Quest quest)
        {
            if (quest == null) return "";

            return quest.Id + "|" + quest.Progress + "|" + IsInside(quest);
        }

        private bool IsInside(Quest quest)
        {
            return _travel.Where.RootId == quest.RootId;
        }

        private void Write(Quest quest)
        {
            Root root = _roots.Roots.Find(quest.RootId);
            string place = root != null ? root.Name : quest.RootId;
            //決壊は核を壊したあとも、敵を倒しきるまで続く。倒した数を目的に添える
            string state = !quest.CoreBroken ? (IsInside(quest) ? "クエスト中" : "受注中")
                         : quest.IsComplete && quest.DefeatedAfterCore > 0 ? "討伐済み" : "破壊済み";
            string objective = QuestText.Objective(quest);
            if (quest.CoreBroken && !quest.IsComplete) objective += "（" + quest.DefeatedAfterCore + "/" + Quest.BreachDefeats + "）";

            _text.text = "<color=" + StateColor + "><b>" + state + "</b></color>　「" + place + "」" + QuestText.KindName(quest) + "クエスト\n"
                       + "<size=80%><color=" + ObjectiveColor + ">" + objective + "</color></size>";

            float width = Mathf.Max(MinWidth, _text.GetPreferredValues(_text.text, 10000f, 1000f).x + Padding * 2f);
            _plate.sizeDelta = new Vector2(width, _plate.sizeDelta.y);
        }
    }
}
