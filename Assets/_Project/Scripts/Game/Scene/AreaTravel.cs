using System;
using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所の移動の入口。場所のシーンの読み込みと「今いる場所」の書き換えはここだけで行う。
    /// 常駐シーンに1つだけ置き、場所のシーンを Additive で足し引きする。
    /// 場所は 1 つの世界に並んでいて（[D-42]）、今いる場所と隣の場所を読み込んでおく。隣へは歩いて境目を越えれば移る。
    /// 近道・倒れたとき・始めたときと、隣がまだ読み込めていない出口では、画面を覆って移る
    /// </summary>
    public sealed class AreaTravel : MonoBehaviour
    {
        /// <summary>プレイヤーと UI など、ずっと残す物を置くシーン</summary>
        public const string CoreScene = "Core";

        /// <summary>タイトルの画面のシーン。設定の「タイトルに戻る」で、常駐シーンごと入れ替える</summary>
        public const string TitleScene = "Title";

        /// <summary>今いる場所の場の範囲からこれだけ出て、隣の場の範囲に入ったら、隣へ移ったとみなす。境目を行ったり来たりしても、着いた処理を繰り返さない</summary>
        public const float CrossMargin = 0.5f;

        [Tooltip("移動させるもの。プレイヤー")]
        [SerializeField] private Transform traveler;

        [Tooltip("移動のあいだ物理を止める。着いた場所の床や壁と重なって弾かれないように")]
        [SerializeField] private Rigidbody travelerBody;

        [Tooltip("移るあいだ画面を覆う。空なら覆わずに切り替える")]
        [SerializeField] private ScreenVeil veil;

        /// <summary>再生を場所のシーンから始めたとき、着いたことを Start で知らせる</summary>
        private bool _arrivedBeforeCore;

        /// <summary>覆って移るときの行き先。読み込まれた時点で有効なシーンにする</summary>
        private string _arriving;

        private bool _wasKinematic;

        /// <summary>覆いを開ける前に読み終えておく物</summary>
        private readonly List<AsyncOperation> _holding = new List<AsyncOperation>();

        /// <summary>読み込んである場所。場所の Id で引く</summary>
        private readonly Dictionary<string, Area> _present = new Dictionary<string, Area>();

        /// <summary>読み込んでいる途中の場所</summary>
        private readonly Dictionary<string, AsyncOperation> _streaming = new Dictionary<string, AsyncOperation>();

        public Whereabouts Where { get; } = new Whereabouts();

        /// <summary>移動させるもの。プレイヤー</summary>
        public Transform Traveler
        {
            get { return traveler; }
        }

        /// <summary>
        /// 根源の並び。隣の場所を決めるのに使う。根源の持ち主（常駐シーン）が渡す。
        /// 渡されていなければ隣を読み込まず、出口では覆って移る
        /// </summary>
        public Func<IReadOnlyList<string>> Lineup { get; set; }

        /// <summary>いま居る場所。着く前は null</summary>
        public Area Current { get; private set; }

        /// <summary>読み込んである場所。今いる場所と、その隣</summary>
        public IEnumerable<Area> Present
        {
            get { return _present.Values; }
        }

        /// <summary>覆い始めてから開け終わるまで。このあいだプレイヤーは動かない。歩いて境目を越えるときは覆わない</summary>
        public bool IsTraveling { get; private set; }

        /// <summary>プレイヤーが場所を離れる直前。覆って移るときは画面は覆われている。場所のシーンはまだ残っている</summary>
        public event Action<Area> Leaving;

        /// <summary>
        /// 場所のシーンを手放す直前。その場所の物はまだ残っていて、動いている。
        /// その場所に置いた物と敵を控えるのはここ。プレイヤーがどこに居るかに頼らない
        /// </summary>
        public event Action<Area> Unloading;

        /// <summary>
        /// 場所のシーンが読み込まれたあと。プレイヤーはまだ着いていないことがある（隣の場所は先に読み込む）。
        /// その場所に物を置き直すのはここ。プレイヤーがどこに居るかに頼らない
        /// </summary>
        public event Action<Area> Loaded;

        /// <summary>プレイヤーが場所に着き、立ち位置が決まったあと。覆って移るときは画面はまだ覆われている</summary>
        public event Action<Area> Entered;

        private void Awake()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Area area = Area.In(SceneManager.GetSceneAt(i));
                if (area == null) continue;

                area.Release();
                _present[area.Id] = area;
                Settle(area);
                _arrivedBeforeCore = true;
                IsTraveling = true;
                return;
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += ActivateArriving;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= ActivateArriving;
        }

        private IEnumerator Start()
        {
            if (!_arrivedBeforeCore) yield break;

            //常駐シーンの Start が出揃ってから知らせる
            yield return null;

            Loaded?.Invoke(Current);
            Place(Current.PointFor(Arrival.Home));
            Entered?.Invoke(Current);

            yield return Reveal();
            Stream();
        }

        /// <summary>歩いて境目を越えたら、隣の場所へ移る</summary>
        private void Update()
        {
            if (IsTraveling || Current == null) return;

            Vector3 at = traveler.position;
            if (OutsideBy(Current, at) <= CrossMargin) return;

            foreach (Area area in _present.Values)
            {
                if (area == Current || !area.Covers(at)) continue;

                Cross(area);
                return;
            }
        }

        /// <summary>覆わずに隣の場所へ入る。読み込みも手放しもせず、出入りだけを知らせる</summary>
        private void Cross(Area next)
        {
            Leaving?.Invoke(Current);
            Settle(next);
            Entered?.Invoke(next);
            Stream();
        }

        /// <summary>
        /// その位置を場の範囲に含む、読み込んである場所。今いる場所を先に見る。どこにも含まれなければ今いる場所。
        /// 境目では隣の場の範囲に入っているので、今いる場所の範囲の外へ押し戻さない
        /// </summary>
        public Area FieldAt(Vector3 at)
        {
            if (Current == null || Current.Covers(at)) return Current;

            foreach (Area area in _present.Values)
            {
                if (area.Covers(at)) return area;
            }

            return Current;
        }

        /// <summary>覆いを開ける前に読み終えておきたい物。Entered の中で渡すと、読み終えるまで覆ったままにする</summary>
        public void HoldVeilFor(AsyncOperation loading)
        {
            if (loading != null && !loading.isDone) _holding.Add(loading);
        }

        /// <summary>始める。既にどこかに居れば何もしない</summary>
        public void Begin(string savedAreaId)
        {
            if (Current != null || IsTraveling) return;

            Go(TravelRule.Begin(savedAreaId));
        }

        /// <summary>
        /// その出口の向こうの場所が読み込んであり、歩いて境目を越えられるか。
        /// 越えられるなら出口に入っても覆って移らない（Player2）
        /// </summary>
        public bool CanWalk(ExitKind exit, IReadOnlyList<string> rootIds)
        {
            Route route = TravelRule.Through(exit, Where.AreaId, rootIds);
            return !route.IsRefused && _present.ContainsKey(route.AreaId);
        }

        /// <summary>出口を通って隣の場所へ覆って移る。並びの端で行き先が無いときは何もせず false</summary>
        public bool Through(ExitKind exit, IReadOnlyList<string> rootIds)
        {
            Route route = TravelRule.Through(exit, Where.AreaId, rootIds);
            if (route.IsRefused) return false;

            Go(route);
            return true;
        }

        /// <summary>拠点から近道で根源へ移る。行けないときと移っている途中は何もせず false。所持金は呼ぶ側が払う</summary>
        public bool Shortcut(string rootId, IReadOnlyList<string> rootIds)
        {
            Route route = TravelRule.Shortcut(Where.AreaId, rootId, rootIds);
            if (route.IsRefused || IsTraveling) return false;

            Go(route);
            return true;
        }

        /// <summary>根源から近道で拠点へ戻る。根源の外と移っている途中は何もせず false</summary>
        public bool ShortcutHome(IReadOnlyList<string> rootIds)
        {
            Route route = TravelRule.ShortcutHome(Where.AreaId, rootIds);
            if (route.IsRefused || IsTraveling) return false;

            Go(route);
            return true;
        }

        /// <summary>倒れたあと、拠点の決まった地点へ戻る</summary>
        public void Revive()
        {
            Go(TravelRule.Revive);
        }

        /// <summary>行き先へ覆って移る。行き先が今の場所なら、立ち位置だけ移す</summary>
        public void Go(Route route)
        {
            if (route.IsRefused || IsTraveling) return;

            if (Current != null && Current.Id == route.AreaId)
            {
                StartCoroutine(Relocate(route.Arrival));
                return;
            }

            StartCoroutine(Travel(route));
        }

        private IEnumerator Travel(Route route)
        {
            //覆っているあいだに裏で読み込み、覆い終わってから切り替える。隣として読み込み中なら、それを待つ
            string areaId = route.AreaId;
            AsyncOperation loading = null;
            if (!_present.ContainsKey(areaId))
            {
                if (!_streaming.TryGetValue(areaId, out loading))
                {
                    loading = Load(areaId);
                    loading.allowSceneActivation = false;
                    _arriving = areaId;
                }
            }

            IsTraveling = true;
            Freeze();

            if (veil != null) yield return veil.Close();

            Area leaving = Current;
            if (leaving != null) Leaving?.Invoke(leaving);

            if (loading != null)
            {
                loading.allowSceneActivation = true;
                while (!loading.isDone || !_present.ContainsKey(areaId)) yield return null;
            }

            Area arrived = _present[areaId];
            Settle(arrived);
            Place(arrived.PointFor(route.Arrival));
            Unfreeze();
            _arriving = null;

            Entered?.Invoke(arrived);

            //遠くなった場所は覆いの裏で手放す。片付けの重いフレームを明転の途中に挟まない
            List<AsyncOperation> unloading = Prune();
            foreach (AsyncOperation pruned in unloading) yield return pruned;
            yield return null;

            //シーンを手放しても、その場所だけが使っていた絵や形はメモリに残る
            if (unloading.Count > 0) yield return Resources.UnloadUnusedAssets();

            yield return Reveal();

            //隣は明けてから裏で読む。覆いを長引かせない
            Stream();
        }

        private IEnumerator Relocate(Arrival arrival)
        {
            IsTraveling = true;
            Freeze();

            if (veil != null) yield return veil.Close();

            Place(Current.PointFor(arrival));
            Unfreeze();

            yield return Reveal();
        }

        private IEnumerator Reveal()
        {
            while (_holding.Exists(loading => !loading.isDone)) yield return null;
            _holding.Clear();

            if (veil != null) yield return veil.Open();

            IsTraveling = false;
        }

        /// <summary>今いる場所の隣を裏で読み込み、遠くなった場所を手放す</summary>
        private void Stream()
        {
            List<AsyncOperation> unloading = Prune();
            if (unloading.Count > 0) unloading[unloading.Count - 1].completed += _ => Resources.UnloadUnusedAssets();

            foreach (string areaId in Wanted())
            {
                if (!_present.ContainsKey(areaId) && !_streaming.ContainsKey(areaId)) Load(areaId);
            }
        }

        /// <summary>読み込んでおく場所。今いる場所と、その隣</summary>
        private List<string> Wanted()
        {
            List<string> wanted = new List<string>();
            if (Current == null) return wanted;

            wanted.Add(Current.Id);

            IReadOnlyList<string> rootIds = Lineup?.Invoke();
            if (rootIds == null) return wanted;

            foreach (ExitKind exit in new[] { ExitKind.ToPrevious, ExitKind.ToNext })
            {
                Route route = TravelRule.Through(exit, Current.Id, rootIds);
                if (!route.IsRefused) wanted.Add(route.AreaId);
            }

            return wanted;
        }

        /// <summary>今いる場所とその隣でない場所を手放す。手放す操作を返す</summary>
        private List<AsyncOperation> Prune()
        {
            List<string> wanted = Wanted();
            List<Area> far = new List<Area>();
            foreach (Area area in _present.Values)
            {
                if (!wanted.Contains(area.Id)) far.Add(area);
            }

            List<AsyncOperation> unloading = new List<AsyncOperation>();
            foreach (Area area in far)
            {
                Unloading?.Invoke(area);
                _present.Remove(area.Id);

                foreach (GameObject root in area.Scene.GetRootGameObjects()) root.SetActive(false);
                unloading.Add(SceneManager.UnloadSceneAsync(area.Scene));
            }

            return unloading;
        }

        /// <summary>場所のシーンを足して読み込み始める。読み込み終えたら、読み込んである場所に入れて知らせる</summary>
        private AsyncOperation Load(string areaId)
        {
            AsyncOperation loading = SceneManager.LoadSceneAsync(areaId, LoadSceneMode.Additive);
            if (loading == null) throw new InvalidOperationException(areaId + " を読み込めない。Build Settings に入っているか");

            _streaming[areaId] = loading;
            loading.completed += _ => Arrive(areaId);
            return loading;
        }

        private void Arrive(string areaId)
        {
            _streaming.Remove(areaId);

            //読み込みの途中でタイトルへ戻ると、常駐シーンが消えたあとに場所だけが読み終わる。残さず手放す
            if (this == null)
            {
                SceneManager.UnloadSceneAsync(areaId);
                return;
            }

            Area area = Area.In(SceneManager.GetSceneByName(areaId));
            if (area == null) throw new InvalidOperationException(areaId + " のルートに Area が無い");

            _present[areaId] = area;
            Loaded?.Invoke(area);
        }

        /// <summary>
        /// 覆って移るとき、着いたシーンを、そのシーンの Start より前に有効にする。
        /// 前の場所が有効なままだと、Start で生まれた物が前の場所に入り、前の場所と一緒に消える
        /// </summary>
        private void ActivateArriving(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == _arriving) SceneManager.SetActiveScene(scene);
        }

        private void Settle(Area area)
        {
            Current = area;
            Where.Arrive(area.Id, area.Kind);

            //後から生まれた物はこのシーンに入る。光と空気は Atmosphere が混ぜて効かせる
            SceneManager.SetActiveScene(area.Scene);
        }

        /// <summary>場の範囲（横と奥行き）の外へ、どれだけ出ているか</summary>
        private static float OutsideBy(Area area, Vector3 at)
        {
            Vector4 bounds = area.Exposition;
            float dx = Mathf.Max(bounds.y - at.x, 0f, at.x - bounds.x);
            float dz = Mathf.Max(bounds.w - at.z, 0f, at.z - bounds.z);
            return Mathf.Max(dx, dz);
        }

        private void Place(Vector3 position)
        {
            traveler.position = position;
            if (travelerBody != null) travelerBody.position = position;
        }

        private void Freeze()
        {
            if (travelerBody == null) return;

            _wasKinematic = travelerBody.isKinematic;
            if (!_wasKinematic) travelerBody.linearVelocity = Vector3.zero;
            travelerBody.isKinematic = true;
        }

        private void Unfreeze()
        {
            if (travelerBody == null) return;

            travelerBody.isKinematic = _wasKinematic;
            if (!_wasKinematic) travelerBody.linearVelocity = Vector3.zero;
        }

        /// <summary>
        /// エディタで場所のシーンから再生を始めたとき、常駐シーンを足す。
        /// 足し終わるまで場所のシーンを止め、プレイヤーの居ないうちに Start が走るのを防ぐ
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AddCoreToAreaScene()
        {
            if (SceneManager.GetSceneByName(CoreScene).isLoaded) return;

            Area area = Area.In(SceneManager.GetActiveScene());
            if (area == null) return;

            area.Hold();
            SceneManager.LoadScene(CoreScene, LoadSceneMode.Additive);
        }
    }
}
