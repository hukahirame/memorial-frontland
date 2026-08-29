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
        foreach (Root root in RootsManager.Roots.All)
        {
            //その根源に調査か決壊が既にあるなら何もしない
            if (QuestManager.Quests.HasMainFor(root.Id)) continue;

            //1度保留してから発行する。初回の夜明けでは積むだけ
            if (!Plan.ShouldIssue(root.Id)) continue;

            QuestManager.Quests.Create(QuestKind.Main, root.Id, "MainSpawner", 1,
                new[] { new Reward("coin", 100), new Reward("progress", 15) });

            QuestManager.Quests.Create(QuestKind.Sub, root.Id, "Slime", 3,
                new[] { new Reward("coin", 100) });

            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
        }
    }
}
