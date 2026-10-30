using System;
using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所の移動の入口。場所のシーンの読み込みと「今いる場所」の書き換えはここだけで行う。
    /// 常駐シーンに1つだけ置き、場所のシーンを Additive で差し替える。
    /// </summary>
    public sealed class AreaTravel : MonoBehaviour
    {
        /// <summary>プレイヤーと UI など、ずっと残す物を置くシーン</summary>
        public const string CoreScene = "Core";

        [Tooltip("移動させるもの。プレイヤー")]
        [SerializeField] private Transform traveler;

        [Tooltip("移動のあいだ物理を止める。着いた場所の床や壁と重なって弾かれないように")]
        [SerializeField] private Rigidbody travelerBody;

        [Tooltip("移るあいだ画面を覆う。空なら覆わずに切り替える")]
        [SerializeField] private ScreenVeil veil;

        /// <summary>再生を場所のシーンから始めたとき、着いたことを Start で知らせる</summary>
        private bool _arrivedBeforeCore;

        /// <summary>読み込み中の行き先。読み込まれた時点で有効なシーンにする</summary>
        private string _arriving;

        private bool _wasKinematic;

        public Whereabouts Where { get; } = new Whereabouts();

        /// <summary>いま居る場所。着く前は null</summary>
        public Area Current { get; private set; }

        /// <summary>覆い始めてから開け終わるまで。このあいだプレイヤーは動かない</summary>
        public bool IsTraveling { get; private set; }

        /// <summary>場所を離れる直前。画面は覆われ、場所のシーンはまだ残っている</summary>
        public event Action<Area> Leaving;

        /// <summary>場所に着き、立ち位置が決まったあと。画面はまだ覆われている</summary>
        public event Action<Area> Entered;

        private void Awake()
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Area area = Area.In(SceneManager.GetSceneAt(i));
                if (area == null) continue;

                area.Release();
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

            Place(Current.PointFor(Arrival.Home));
            Entered?.Invoke(Current);

            yield return Reveal();
        }

        /// <summary>始める。既にどこかに居れば何もしない</summary>
        public void Begin(string savedAreaId)
        {
            if (Current != null || IsTraveling) return;

            Go(TravelRule.Begin(savedAreaId));
        }

        /// <summary>出口を通る。行けないときは何もせず false</summary>
        public bool Through(ExitKind exit, Quest ordered)
        {
            Route route = TravelRule.Through(exit, ordered);
            if (route.IsRefused) return false;

            Go(route);
            return true;
        }

        /// <summary>倒れたあと、拠点の決まった地点へ戻る</summary>
        public void Revive()
        {
            Go(TravelRule.Revive);
        }

        /// <summary>行き先へ移る。行き先が今の場所なら、読み直さずに立ち位置だけ移す</summary>
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
            //覆っているあいだに裏で読み込み、覆い終わってから切り替える
            AsyncOperation loading = SceneManager.LoadSceneAsync(route.AreaId, LoadSceneMode.Additive);
            if (loading == null)
            {
                throw new InvalidOperationException(route.AreaId + " を読み込めない。Build Settings に入っているか");
            }

            loading.allowSceneActivation = false;
            _arriving = route.AreaId;
            IsTraveling = true;
            Freeze();

            if (veil != null) yield return veil.Close();

            Area leaving = Current;
            if (leaving != null) Leaving?.Invoke(leaving);

            loading.allowSceneActivation = true;
            while (!loading.isDone) yield return null;

            AsyncOperation unloading = null;
            if (leaving != null)
            {
                Scene previous = leaving.Scene;
                foreach (GameObject root in previous.GetRootGameObjects()) root.SetActive(false);
                unloading = SceneManager.UnloadSceneAsync(previous);
            }

            Area arrived = Area.In(SceneManager.GetSceneByName(route.AreaId));
            if (arrived == null) throw new InvalidOperationException(route.AreaId + " のルートに Area が無い");

            Settle(arrived);
            Place(arrived.PointFor(route.Arrival));
            Unfreeze();
            _arriving = null;

            Entered?.Invoke(arrived);

            //前の場所を片付け終わるまで覆っておく。片付けの重いフレームを明転の途中に挟まない
            if (unloading != null) yield return unloading;
            yield return null;

            yield return Reveal();
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
            if (veil != null) yield return veil.Open();

            IsTraveling = false;
        }

        /// <summary>
        /// 着いたシーンを、そのシーンの Start より前に有効にする。
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

            //光や霧は有効なシーンのものが効く。後から生まれた物もこのシーンに入る
            SceneManager.SetActiveScene(area.Scene);
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
