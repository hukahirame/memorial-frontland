using MemorialFloor.Domain;
using UnityEngine;
using System.Collections;

public class Sun2 : MonoBehaviour
{
    /// <summary>1日の経過。所有者はここ1つ（QuestManager.Quests と同じ形）</summary>
    public static readonly DayCycle Cycle = new DayCycle();

    /// <summary>調査クエストの発行計画。シーンを跨いで持ち越す</summary>
    public static readonly DayPlan Plan = new DayPlan();

    // 名前を変えるとシーンに保存済みの値が外れる。公開をやめても名前は据え置くこと
    [SerializeField] private float aroundtime = 120; // 1日のゲーム内時間(秒)

    [SerializeField] private Light lighting;

    private float maxIntensity = 1.8f;
    private float minIntensity = 0.3f;

    private void Start()
    {
        StartCoroutine(SunAround());
    }

    private IEnumerator SunAround()
    {
        while (true)
        {
            if (Cycle.IsDayOver(aroundtime)) //零時
            {
                Cycle.Reset();
                GameObject.FindWithTag("RootsManager").GetComponent<RootsManager>().Dayover();
            }

            lighting.intensity = DayClock.LightIntensity(
                Cycle.HourAt(aroundtime), minIntensity, maxIntensity);

            yield return new WaitForSeconds(1f);
            Cycle.Advance();

            //5時の境界を跨いだ刻みだけ。5時台の毎秒ではない
            if (Cycle.Entered(DayClock.MorningHour, aroundtime)) DayStart();
        }
    }

    private void DayStart() //夜明け後
    {
        bool issued = false;

        foreach (Root root in RootsManager.Roots.All)
        {
            //サブは調査と別に、無ければ足す。保留も挟まない
            if (QuestManager.IssueSub(root.Id)) issued = true;

            //その根源に調査か決壊が既にあるなら出さない。無ければ 1 度保留してから出す。初回の夜明けでは積むだけ
            if (QuestManager.Quests.HasMainFor(root.Id) || !Plan.ShouldIssue(root.Id)) continue;

            QuestManager.IssueMain(root, QuestKind.Main);
            issued = true;
        }

        if (issued) GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
    }
}
