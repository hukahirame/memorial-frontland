using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 根源と拠点への攻め込みに起きたことを、知らせの文字で出す。
    /// 氾濫して攻め込まれた・次の零時に氾濫しそう・核が壊れて大量に湧いた・拠点に入り込んだ敵が出た
    /// </summary>
    public sealed class RootNotices : MonoBehaviour
    {
        [SerializeField] private RootsManager _roots;
        [SerializeField] private AreaTravel _travel;
        [SerializeField] private MiddleText _middleText;

        private void OnEnable()
        {
            _roots.Invaded += ShowInvaded;
            _roots.OverflowForeseen += ShowForeseen;
            _roots.Stampeded += ShowStampeded;
            _roots.RaidersReleased += ShowRaidersReleased;
        }

        private void OnDisable()
        {
            _roots.Invaded -= ShowInvaded;
            _roots.OverflowForeseen -= ShowForeseen;
            _roots.Stampeded -= ShowStampeded;
            _roots.RaidersReleased -= ShowRaidersReleased;
        }

        private void ShowInvaded(Root root)
        {
            _middleText.Show(root.Name + "が氾濫し、拠点に敵が攻め込みました");
        }

        private void ShowForeseen(Root root)
        {
            _middleText.Show(root.Name + "が次の零時に氾濫しそうです");
        }

        private void ShowStampeded()
        {
            _middleText.Show("敵が大量発生しています");
        }

        private void ShowRaidersReleased(int count)
        {
            StartCoroutine(ShowAfterTravel("拠点に敵が " + count + " 体入り込んでいます"));
        }

        /// <summary>暗転が明けてから知らせる。拠点を読み込むのは移る途中で、覆われているあいだに出すと見えない</summary>
        private IEnumerator ShowAfterTravel(string message)
        {
            while (_travel.IsTraveling) yield return null;

            _middleText.Show(message);
        }
    }
}
