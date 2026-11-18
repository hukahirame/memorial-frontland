using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所を読み込んだとき、着いたとき、離れるときの処理。常駐シーンに置き、移動の入口（AreaTravel）の知らせを受ける。
    /// 根源を読み込んだら核を置き、拠点を読み込んだら湧き口を起こし、置かれた敵に常駐シーンのものを渡す。着いたら曲を替え、クエストを始め、結果を出す
    /// </summary>
    public sealed class SceneStarter : MonoBehaviour
    {
        /// <summary>拠点の BGM に 1 曲目を選ぶ確率（%）</summary>
        private const int BaseBgmPercent = 50;

        private const string BaseBgm = "music.dream";
        private const string BaseBgmOther = "雨雫 @ フリーBGM DOVA-SYNDROME OFFICIAL YouTube CHANNEL";
        private const string RootBgm = "solarisnoame";

        /// <summary>核を壊したあとの曲</summary>
        private const string StampedeBgm = "dark city";

        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField, FormerlySerializedAs("music")] private Music _music;
        [SerializeField, FormerlySerializedAs("questManager")] private QuestManager _quests;
        [SerializeField, FormerlySerializedAs("roots")] private RootsManager _roots;
        [SerializeField, FormerlySerializedAs("rewardUI")] private RewardUI _rewardUI;
        [SerializeField] private SaveSystem _save;
        [SerializeField] private PlayerHealth _player;
        [SerializeField] private MiddleText _middleText;

        [Tooltip("結果の画面を出す所")]
        [SerializeField, FormerlySerializedAs("rewardPoint")] private Transform _rewardPoint;

        private EnemyContext _enemies;

        private void Awake()
        {
            _enemies = new EnemyContext(_travel, _roots, _quests, _player, _middleText);

            //隣の場所を先に読み込むのに、根源の並びを渡す
            _travel.Lineup = _roots.RootIds;
            _travel.Loaded += Load;
            _travel.Entered += Enter;
            _travel.Leaving += Leave;
        }

        private void OnDestroy()
        {
            _travel.Loaded -= Load;
            _travel.Entered -= Enter;
            _travel.Leaving -= Leave;
        }

        private void Leave(Area area)
        {
            _quests.Leave(area.RootId);
        }

        private void Load(Area area)
        {
            //場に置いてある敵は、湧かせた側が居ないので、ここで渡す
            foreach (Slime slime in area.FindAll<Slime>()) slime.Bind(_enemies, area.RootId);

            if (area.Kind == AreaKind.Root) PlaceCore(area);

            RaidGate gate = area.Find<RaidGate>();
            if (gate != null) gate.Bind(_enemies);
        }

        private void Enter(Area area)
        {
            if (area.Kind == AreaKind.Root)
            {
                //根源へ入ると核の耐久が戻る。隣として読み込んだまま入り直したときも
                RootCore core = area.Find<RootCore>();
                if (core != null) core.Mend();

                PlayBgm(RootBgm);
                _quests.StartOrdered();
            }
            else
            {
                PlayBgm(Random.Range(0, 100) < BaseBgmPercent ? BaseBgm : BaseBgmOther);

                //結果の画面を出すのはここだけ
                _rewardUI.ShowPending(_rewardPoint.position);
            }

            _quests.Arrive(area.Kind);
        }

        /// <summary>
        /// 曲を替える。曲は裏で読み、読み終えてから替える（Music）。
        /// 覆って移るときは、読み終えるまで覆いを開けず、覆いの裏ですぐ替える
        /// </summary>
        public void PlayBgm(string clip)
        {
            _music.Play(clip, _travel.IsTraveling);
            if (_travel.IsTraveling) _travel.HoldVeilUntil(() => !_music.IsLoading);
        }

        /// <summary>
        /// 根源の核を、核の候補の 1 つに足す。足した部品はシーンと一緒に消えるので、読み込むたびに置く。
        /// 位置は初回に選んで控え、2 回目からはそこに置き直す。控える位置は場所の原点からの位置
        /// </summary>
        private void PlaceCore(Area area)
        {
            Root root = _roots.Roots.Find(area.RootId);
            if (root == null) return;

            List<SpawnerCandidate> candidates = new List<SpawnerCandidate>();
            foreach (SpawnerCandidate candidate in area.FindAll<SpawnerCandidate>())
            {
                if (candidate.gameObject.activeInHierarchy) candidates.Add(candidate);
            }

            if (candidates.Count == 0) return;

            SpawnerCandidate chosen = root.HasSpawnPoint
                ? Nearest(candidates, area.ToWorld(new Vector3(root.SpawnX, root.SpawnY, root.SpawnZ)))
                : candidates[Random.Range(0, candidates.Count)];

            RootCore core = chosen.gameObject.AddComponent<RootCore>();
            core.Bind(area.Exposition, root.Id, _enemies);
            core.Broken += () => PlayBgm(StampedeBgm);
            _save.RestoreEnemies(area, core);

            if (root.HasSpawnPoint) return;

            Vector3 at = area.ToLocal(chosen.transform.position);
            root.PlaceSpawnPoint(at.x, at.y, at.z);
        }

        private static SpawnerCandidate Nearest(List<SpawnerCandidate> candidates, Vector3 point)
        {
            SpawnerCandidate nearest = candidates[0];
            foreach (SpawnerCandidate candidate in candidates)
            {
                if ((candidate.transform.position - point).sqrMagnitude < (nearest.transform.position - point).sqrMagnitude) nearest = candidate;
            }

            return nearest;
        }
    }
}
