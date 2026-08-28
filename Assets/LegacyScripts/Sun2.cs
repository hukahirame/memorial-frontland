using MemorialFloor.Domain;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Sun2 : MonoBehaviour
{
    public static int daytime = 60;//零時からの経過時間(秒)
    public float aroundtime = 120; // 1日のゲーム内時間(秒)
    private float now = 0;
    private float state = 0;

    public static List<string> questplan = new List<string>();
    [SerializeField] private Light lighting;

    private float maxIntensity = 1.8f;
    private float minIntensity = 0.3f;

    private void Start()
    {
        StartCoroutine(SunAround());
    }

    IEnumerator SunAround()
    {
        while (true)
        {
            now = daytime / (aroundtime / 24); //0～24.0時

            if (daytime >= aroundtime) //零時
            {
                daytime = 0;
                GameObject.FindWithTag("RootsManager").GetComponent<RootsManager>().Dayover();
            }
            else if (Mathf.FloorToInt(now) == 4) //夜明け
            {
                SunEvent(true);
            }
            else if (Mathf.FloorToInt(now) == 5) //デイリー更新 + 夜明け終了
            {
                lighting.intensity = maxIntensity;
                DayStart();
            }
            else if (Mathf.FloorToInt(now) == 16) //日没
            {
                SunEvent(false);
            }
            else if (Mathf.FloorToInt(now) == 17) //日没終了
            {
                lighting.intensity = minIntensity;
            }
            yield return new WaitForSeconds(1f);
            daytime++;
        }
    }

    private void SunEvent(bool sunbreak) //毎秒呼出
    {
        state = now - Mathf.Floor(now); //nowの小数部分

        if (sunbreak) lighting.intensity = 1.5f * state + minIntensity;
        else lighting.intensity = -1.5f * state + maxIntensity;
    }
   /* IEnumerator SunEvent(bool sunbreak)
    {
        state = now - Mathf.Floor(now);

        if (sunbreak) lighting.intensity = 1.5f * state + minIntensity;
        else          lighting.intensity = -1.5f * state + maxIntensity;

        if (state > 0.79f)//最後分
        {
            yield return new WaitForSeconds(1f);
            lighting.intensity = sunbreak ? maxIntensity : minIntensity;
        }
        yield return null;
    }*/

    private void DayStart() //夜明け後
    {
        foreach (var root in RootsManager.Roots.All)
        {
            if (!QuestManager.Quests.HasMainFor(root.Id)) //前半：rootに紐つく調査か決壊を探す
            {
                if (questplan.Contains(root.Id)) //後半：無かった場合、保留→作成
                {
                    QuestManager.Quests.Create(QuestKind.Main, root.Id, "MainSpawner", 1,
                        new[] { new Reward("coin", 100), new Reward("progress", 15) });
                    questplan.Remove(root.Id);

                    QuestManager.Quests.Create(QuestKind.Sub, root.Id, "Slime", 3,
                        new[] { new Reward("coin", 100) });

                    GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
                }
                else //夜明け1回目
                {
                    questplan.Add(root.Id);
                }
            }
        }

    }
    private void Proto()
    {
        QuestManager.Quests.Create(QuestKind.Main, "Root1", "MainSpawner", 1,
            new[] { new Reward("coin", 100), new Reward("progress", 15) });

        QuestManager.Quests.Create(QuestKind.Sub, "Root1", "Slime", 3,
            new[] { new Reward("coin", 100) });

        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
        Debug.Log("新クエスト発生:" + QuestManager.Quests.Count);
    }

}
