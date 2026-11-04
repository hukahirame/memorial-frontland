using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;

/// <summary>地図に出す、拠点へ戻る近道のボタン。根源に居るときだけ見え、受けたクエストの対象の根源では押せない</summary>
public class HomeShortcut : MonoBehaviour
{
    [SerializeField] private AreaTravel travel;

    [SerializeField] private Button button;

    [SerializeField] private Text label;

    [Tooltip("押して移るときに閉じる画面。地図と根源の詳細")]
    [SerializeField] private CloseManager[] closes;

    /// <summary>地図を開いたとき</summary>
    public void Show()
    {
        bool questHere = QuestManager.IsUnderwayIn(travel.Where.RootId);
        label.text = questHere ? "クエスト中は戻れません" : "拠点へ戻る（無料）";
        button.interactable = !questHere;
        gameObject.SetActive(!TravelRule.ShortcutHome(travel.Where.AreaId, RootsManager.RootIds()).IsRefused);
    }

    /// <summary>ボタンから呼ぶ。地図を開いたあとに受けたクエストの根源へ入っていたら戻らない</summary>
    public void Go()
    {
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;
        if (QuestManager.IsUnderwayIn(travel.Where.RootId)) return;
        if (!travel.ShortcutHome(RootsManager.RootIds())) return;

        foreach (CloseManager close in closes) close.CloseClick();
    }
}
