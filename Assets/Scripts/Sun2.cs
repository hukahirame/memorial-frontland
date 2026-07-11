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
        int i, j;
        for (i = 0; i < RootsManager.roots.Count; i++)
        {
            bool NoMainQuest = true;
            for (j = 0; j < QuestManager.quests.Count; j++) //前半：root[i]に紐つくquest[j]を探す
            {
                if (QuestManager.quests[j][1] == RootsManager.roots[i][2]) //根源対象クエ探査
                {
                    if (QuestManager.quests[j][0].IndexOfAny(new char[] { 'X', 'Y' }) == 0)
                    {
                        NoMainQuest = false;
                        break;
                    }
                }
            }
            if (NoMainQuest) //後半：無かった場合、保留→作成
            {
                if (questplan.Contains(RootsManager.roots[i][2]))
                {
                    QuestManager.CreateQuest("X", RootsManager.roots[i][2], "MainSpawner", "1");
                    QuestManager.rewards.Add(new string[] { "coin", "100", "progress", "15" });
                    questplan.Remove(RootsManager.roots[i][2]);

                    QuestManager.CreateQuest("S", RootsManager.roots[i][2], "Slime", "3");
                    QuestManager.rewards.Add(new string[] { "coin", "100" });

                    GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
                }
                else //夜明け1回目
                {
                    questplan.Add(RootsManager.roots[i][2]);
                }
            }
        }

    }
    private void Proto()
    {
        QuestManager.CreateQuest("X", "Root1", "MainSpawner", "1");
        QuestManager.rewards.Add(new string[] { "coin", "100", "progress", "15" });

        QuestManager.CreateQuest("S", "Root1", "Slime", "3");
        QuestManager.rewards.Add(new string[] { "coin", "100" });

        GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("新クエストが発生しました");
        Debug.Log("新クエスト発生:" + QuestManager.quests);
    }

}
