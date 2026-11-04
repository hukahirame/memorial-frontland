using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;

/// <summary>根源の詳細に出す近道のボタン。拠点に居るときだけ見える</summary>
public class RootShortcut : MonoBehaviour
{
    [SerializeField] private AreaTravel travel;

    [SerializeField] private Text label;

    [Tooltip("押して移るときに閉じる画面。根源の詳細と地図")]
    [SerializeField] private CloseManager[] closes;

    /// <summary>詳細の枠は根源どうしで使い回すので、開くたびに差し替える</summary>
    private string rootId;

    /// <summary>出している額。押したときの額と違えば、払わせずに出し直す</summary>
    private int shownFee;

    /// <summary>根源の詳細を開いたとき。払う額は開いたときの攻略度で出す</summary>
    public void Show(string id)
    {
        rootId = id;

        shownFee = TravelRule.ShortcutFeeTo(RootsManager.Roots.Find(id));
        label.text = "近道で向かう（" + (shownFee == 0 ? "無料" : shownFee + " コイン") + "）";
        gameObject.SetActive(!TravelRule.Shortcut(travel.Where.AreaId, id, RootsManager.RootIds()).IsRefused);
    }

    /// <summary>ボタンから呼ぶ。出している額だけを払う。開いたあとに拠点を出ていたら移らない</summary>
    public void Go()
    {
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;

        //開いたまま零時を越えると、攻略度が段を割って額が変わる。見せていない額は取らない
        int fee = TravelRule.ShortcutFeeTo(RootsManager.Roots.Find(rootId));
        if (fee != shownFee)
        {
            Show(rootId);
            Notice("近道の料金が変わりました");
            return;
        }

        if (!GameManager.Coins.CanAfford(fee))
        {
            Notice("所持金が足りません");
            return;
        }

        if (!travel.Shortcut(rootId, RootsManager.RootIds())) return;

        GameManager.Coins.Spend(fee);
        foreach (CloseManager close in closes) close.CloseClick();
    }

    private static void Notice(string text)
    {
        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim(text);
    }
}
