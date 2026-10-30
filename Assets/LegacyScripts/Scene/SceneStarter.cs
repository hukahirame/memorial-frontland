using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;

/// <summary>場所に着いたときの処理全般。常駐シーンに置き、移動の入口から呼ばれる</summary>
public class SceneStarter : MonoBehaviour
{
    /// <summary>拠点の BGM に 1 曲目を選ぶ確率（%）</summary>
    private const int BaseBgmPercent = 50;

    [SerializeField] private AreaTravel travel;
    [SerializeField] private Player2 player;
    [SerializeField] private Slider hpView;
    [SerializeField] private Button jumpButton;
    [SerializeField] private AudioSource bgm;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private RewardUI rewardUI;
    [SerializeField] private Transform rewardPoint;

    private void Awake()
    {
        travel.Entered += Enter;
    }

    private void OnDestroy()
    {
        travel.Entered -= Enter;
    }

    private void Enter(Area area)
    {
        player.expos = area.Exposition;
        Player2.BindHpView(hpView);

        if (area.Kind == AreaKind.Root)
        {
            SpawnerDecide(area);
            JumpReset();
            PlayBgm("solarisnoame");
            questManager.StartQuest();
        }
        else
        {
            PlayBgm(Random.Range(0, 100) < BaseBgmPercent
                ? "music.dream"
                : "雨雫 @ フリーBGM DOVA-SYNDROME OFFICIAL YouTube CHANNEL");

            if (RewardUI.rewardUI_show != 0) //RewardUIの唯一のスタートポイント
            {
                rewardUI.transform.position = rewardPoint.position;
                rewardUI.InputInfo();
            }
        }
    }

    private void PlayBgm(string clip)
    {
        bgm.clip = (AudioClip)Resources.Load(clip);
        bgm.Play();
    }

    private void SpawnerDecide(Area area)
    {
        Root root = RootsManager.Roots.Find(travel.Where.RootId);
        if (root == null) return;

        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("SpawnerCandidate"))
        {
            if (candidate.scene == area.Scene) candidates.Add(candidate);
        }

        if (candidates.Count == 0) return;

        //足した部品は場所を離れると消えるので、入るたびに置く。位置は初回に選んで控え、2 回目からはそこに置き直す
        GameObject chosen = root.HasSpawnPoint
            ? Nearest(candidates, new Vector3(root.SpawnX, root.SpawnY, root.SpawnZ))
            : candidates[Random.Range(0, candidates.Count)];
        chosen.AddComponent<OF_Spawner>().Bind(area.Exposition, root.Id);

        if (root.HasSpawnPoint) return;

        Vector3 targetpos = chosen.transform.position;
        root.PlaceSpawnPoint(targetpos.x, targetpos.y, targetpos.z);
    }

    private static GameObject Nearest(List<GameObject> candidates, Vector3 point)
    {
        GameObject nearest = candidates[0];
        foreach (GameObject candidate in candidates)
        {
            if ((candidate.transform.position - point).sqrMagnitude < (nearest.transform.position - point).sqrMagnitude) nearest = candidate;
        }

        return nearest;
    }

    private void JumpReset()
    {
        jumpButton.onClick.RemoveAllListeners();
        jumpButton.onClick.AddListener(player.Jump);
    }
}
