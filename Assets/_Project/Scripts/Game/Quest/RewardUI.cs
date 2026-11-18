using System.Collections.Generic;
using MemorialFloor.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>拠点に戻ったときに出す、受けたクエストの結果の画面。成功なら報酬をここで受け取る</summary>
    public sealed class RewardUI : MonoBehaviour
    {
        /// <summary>成功の字の色</summary>
        private static readonly Color SuccessColor = new Color(170, 0, 0, 255);

        [SerializeField] private QuestManager _quests;
        [SerializeField] private RootsManager _roots;
        [SerializeField, FormerlySerializedAs("toptxt")] private TextMeshProUGUI _title;
        [SerializeField, FormerlySerializedAs("sidetxt")] private TextMeshProUGUI _details;
        [SerializeField, FormerlySerializedAs("resulttxt")] private TextMeshProUGUI _result;

        /// <summary>置いたときの文字色が失敗の色。成功で塗り替えたあと、失敗で戻す</summary>
        private Color _failColor;

        private void Awake()
        {
            _failColor = _result.color;
        }

        /// <summary>
        /// 出す結果があれば書き込み、at に出す。結果は書き込めなくても持ち越さない。
        /// 持ち越すと、あとで関係の無い帰還のときに古い結果が出る
        /// </summary>
        public void ShowPending(Vector3 at)
        {
            QuestOutcome outcome = _quests.TakeOutcome();
            if (outcome == QuestOutcome.None) return;

            Quest quest = _quests.Quests.Find(_quests.ResultQuestId);
            if (quest == null) return;

            Root root = _roots.Roots.Find(quest.RootId);
            if (root == null) return;

            _title.text = "「" + root.Name + "」のクエスト結果";
            _details.text = "◎危険度：" + root.Danger + "\n\n";

            if (outcome == QuestOutcome.Failed)
            {
                _result.text = "失敗";
                _result.color = _failColor;

                //達成していても、根源を出る前に倒れたらやり直し。受注は残すので、もう一度挑める
                quest.SetProgress(0);
            }
            else
            {
                _result.text = "成功";
                _result.color = SuccessColor;

                //受け取るのは達成済みのすべて。表示もそれに合わせる。攻略度は上限で止まる
                List<Quest> cleared = _quests.Cleared();
                _details.text += string.Format("◎攻略度　{0} % → {1} %\n\n", root.Progress, root.ProgressAfterGain(QuestManager.ProgressGain(cleared, root.Id)));
                _details.text += "【報酬】\n";

                foreach (Reward reward in QuestManager.Total(cleared))
                    _details.text += "・" + _quests.RewardName(reward.Kind) + "　x　" + reward.Amount + "\n";

                _quests.PayCleared();
            }

            transform.position = at;
        }
    }
}
