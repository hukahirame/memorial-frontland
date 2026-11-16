using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.UI;

/// <summary>場所を読み込んだときと、場所に着いたときの処理全般。常駐シーンに置き、移動の入口から呼ばれる</summary>
public class SceneStarter : MonoBehaviour
{
    /// <summary>拠点の BGM に 1 曲目を選ぶ確率（%）</summary>
    private const int BaseBgmPercent = 50;

    [SerializeField] private AreaTravel travel;
    [SerializeField] private Player2 player;
    [SerializeField] private Slider hpView;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Music music;
    [SerializeField] private QuestManager questManager;
    [SerializeField] private RewardUI rewardUI;
    [SerializeField] private Transform rewardPoint;

    private void Awake()
    {
        //隣の場所を先に読み込むのに、根源の並びを渡す
        travel.Lineup = RootsManager.RootIds;
        travel.Loaded += Load;
        travel.Entered += Enter;
        travel.Leaving += Leave;
    }

    private void OnDestroy()
    {
        travel.Loaded -= Load;
        travel.Entered -= Enter;
        travel.Leaving -= Leave;
    }

    private void Leave(Area area)
    {
        questManager.Leave(area.RootId);
    }

    private void Load(Area area)
    {
        if (area.Kind == AreaKind.Root) SpawnerDecide(area);
    }

    private void Enter(Area area)
    {
        Player2.BindHpView(hpView);

        if (area.Kind == AreaKind.Root)
        {
            //根源へ入ると核の耐久が戻る。隣として読み込んだまま入り直したときも
            OF_Spawner core = area.Find<OF_Spawner>();
            if (core != null) core.Mend();

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
                if (rewardUI.InputInfo()) rewardUI.transform.position = rewardPoint.position;
            }
        }

        questManager.Arrived(area.Kind, rewardUI);
    }

    /// <summary>鳴らそうとしている曲。読み終える前に次の曲を頼まれたら、前の曲は鳴らさない</summary>
    private string wantedBgm;

    /// <summary>
    /// 曲を替える。曲は大きく、同じコマで読むと数百ミリ秒止まるので、裏で読み、読み終えてから替える。
    /// 読み終えるまでは前の曲が鳴り続け、替えるときは重ねて移る（境目を歩いて越えたとき）。
    /// 同じ曲が鳴っていれば流し続ける。覆って移るときは、読み終えるまで覆いを開けず、覆いの裏ですぐ替える
    /// </summary>
    public void PlayBgm(string clip)
    {
        wantedBgm = clip;
        if (music.IsPlaying && music.Clip != null && music.Clip.name == clip) return;

        ResourceRequest loading = Resources.LoadAsync<AudioClip>(clip);
        if (travel.IsTraveling) travel.HoldVeilFor(loading);

        loading.completed += _ =>
        {
            if (wantedBgm != clip || music == null) return;

            music.Play((AudioClip)loading.asset, travel.IsTraveling);
        };
    }

    private void SpawnerDecide(Area area)
    {
        Root root = RootsManager.Roots.Find(area.RootId);
        if (root == null) return;

        List<GameObject> candidates = new List<GameObject>();
        foreach (GameObject candidate in GameObject.FindGameObjectsWithTag("SpawnerCandidate"))
        {
            if (candidate.scene == area.Scene) candidates.Add(candidate);
        }

        if (candidates.Count == 0) return;

        //足した部品はシーンと一緒に消えるので、読み込むたびに置く。位置は初回に選んで控え、2 回目からはそこに置き直す。
        //控える位置は場所の原点からの位置
        GameObject chosen = root.HasSpawnPoint
            ? Nearest(candidates, area.ToWorld(new Vector3(root.SpawnX, root.SpawnY, root.SpawnZ)))
            : candidates[Random.Range(0, candidates.Count)];
        chosen.AddComponent<OF_Spawner>().Bind(area.Exposition, root.Id);

        if (root.HasSpawnPoint) return;

        Vector3 targetpos = area.ToLocal(chosen.transform.position);
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
