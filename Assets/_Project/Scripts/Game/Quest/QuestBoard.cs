using System.Collections.Generic;
using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>根源の詳細に出すクエストの枠。根源 1 つぶんのクエストを決壊、調査、サブの順に並べ、受注の様子を添える</summary>
    public sealed class QuestBoard : MonoBehaviour
    {
        /// <summary>一度に表示するクエストの数</summary>
        private const int ShownAtOnce = 4;

        /// <summary>表示順。決壊、調査、サブの順に並べる</summary>
        private static readonly QuestKind[] ShowOrder = { QuestKind.Breach, QuestKind.Main, QuestKind.Sub };

        /// <summary>受注中の枠の地と縁の色。ほかの枠と見分けがつくように</summary>
        private static readonly Color OrderedPaper = new Color(1f, 0.93f, 0.74f);
        private static readonly Color OrderedFrame = new Color(1f, 0.8f, 0.36f);

        /// <summary>ほかの根源のクエストを受注しているときの、状態の字の色</summary>
        private static readonly Color OtherOrderedColor = new Color(0.45f, 0.42f, 0.4f);

        [SerializeField] private QuestManager _quests;
        [SerializeField] private ItemBook _items;
        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField] private MiddleText _middleText;

        [Tooltip("クエスト 1 本の枠")]
        [SerializeField, FormerlySerializedAs("questUI")] private GameObject _cardPrefab;

        [Tooltip("枠を置く段。子を上から順に使う")]
        [SerializeField] private Transform _slots;

        private void OnEnable()
        {
            _quests.Removed += RemoveCard;
        }

        private void OnDisable()
        {
            _quests.Removed -= RemoveCard;
        }

        /// <summary>根源のクエストを並べる</summary>
        public void Show(string rootId)
        {
            //開くたびに作り直す。閉じるボタンは枠を消さないので、残すと開くたびに積もる
            foreach (QuestButton old in GetComponentsInChildren<QuestButton>(true)) Destroy(old.gameObject);

            List<Quest> shown = new List<Quest>();
            foreach (QuestKind kind in ShowOrder)
            {
                foreach (Quest quest in _quests.Quests.All)
                {
                    if (shown.Count >= ShownAtOnce) break;
                    if (quest.RootId != rootId || quest.Kind != kind) continue;

                    shown.Add(quest);
                }
            }

            //1 本ずつ別の段に置く。重ねると、後に置いた押せないサブの枠が調査の枠への入力を奪う
            for (int i = 0; i < shown.Count; i++) Put(shown[i], _slots.GetChild(i).position);
        }

        private void Put(Quest quest, Vector3 at)
        {
            GameObject card = Instantiate(_cardPrefab, at, Quaternion.identity, transform);
            card.name = quest.Id;
            card.GetComponent<QuestButton>().Bind(quest.Id, _quests, this, _middleText);

            //消した古い枠はこのフレームの終わりまで残り、同じ名前で先に見つかるので、名前で引かない
            Slider slider = card.transform.Find("Slider_IconType_03_basic_WhiteFill").GetComponent<Slider>();
            slider.maxValue = quest.Amount;
            slider.value = quest.Progress;

            TextMeshProUGUI type = card.transform.Find("TypeText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI body = card.transform.Find("BodyText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI reward = card.transform.Find("RewardText").GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI status = card.transform.Find("StatusText").GetComponent<TextMeshProUGUI>();

            type.text = TypeName(quest.Kind);
            body.text += QuestText.FirstObjective(quest);

            foreach (Reward paid in quest.Rewards) reward.text += QuestText.RewardName(paid.Kind, _items.Items) + " x " + paid.Amount + "\n";

            if (quest.Kind == QuestKind.Sub || quest.Kind == QuestKind.Common)
            {
                card.GetComponent<Button>().interactable = false;
                status.text = quest.IsComplete ? "達成済み" : "";
            }
            else if (_quests.OrderedId == quest.Id)
            {
                status.text = quest.IsComplete ? "達成済み" : _travel.Where.RootId == quest.RootId ? "クエスト中" : "受注中";
                Emphasize(card, status);
            }
            else if (quest.IsComplete) status.text = "達成済み";
            else if (_quests.Quests.Find(_quests.OrderedId) != null)
            {
                //受注できるのは全体で 1 本だけ。ほかの根源のものを受けているあいだは受けられない
                status.text = "ほかを受注中";
                status.color = OtherOrderedColor;
            }
        }

        private static string TypeName(QuestKind kind)
        {
            switch (kind)
            {
                case QuestKind.Main: return "【調査クエスト】";
                case QuestKind.Breach: return "【決壊クエスト】";
                case QuestKind.Sub: return "【サブクエスト】";
                default: return "【共通クエスト】";
            }
        }

        /// <summary>受注中の枠を目立たせる。縁を金に、地を暖かい色にし、状態を赤いリボンに白い字で載せる</summary>
        private static void Emphasize(GameObject card, TextMeshProUGUI status)
        {
            card.GetComponent<Image>().color = OrderedPaper;
            card.transform.Find("Frame").GetComponent<Image>().color = OrderedFrame;

            Transform badge = card.transform.Find("OrderedBadge");
            if (badge != null) badge.gameObject.SetActive(true);

            status.color = Color.white;
            status.fontStyle = FontStyles.Bold;
        }

        //払ったクエストの枠を消す
        private void RemoveCard(string questId)
        {
            Transform card = transform.Find(questId);
            if (card != null) Destroy(card.gameObject);
        }
    }
}
