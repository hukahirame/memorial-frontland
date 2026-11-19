using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Profiling;
using Unity.Profiling.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Profiling;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using Stopwatch = System.Diagnostics.Stopwatch;

//場所 1 つを起こす重さと片付ける重さを測り、Temp/AreaCosts.txt に書く。判定はしないので、名指しで回したときだけ走る。
//エディタの数は実機と違う。見るのは、場所どうしの差と、移る途中のどこが重いか
[Explicit]
public class AreaCosts
{
    private const float TravelSeconds = 20f;
    private const int SettleFrames = 60;

    /// <summary>起こし終えてから数えるコマ。Start と最初のコルーチンがここに入る</summary>
    private const int StartFrames = 10;

    /// <summary>手放し終えてから数えるコマ</summary>
    private const int AfterFrames = 5;

    /// <summary>重いとみなすコマ（ミリ秒）。30fps の 1 コマ</summary>
    private const double SpikeMs = 33.0;

    private const int TopMarkers = 6;
    private const double MarkerFloorMs = 0.3;

    private static readonly string Report = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "AreaCosts.txt"));

    /// <summary>拠点に居たまま足して読む場所</summary>
    private static readonly string[] Beside = { "Root1", "Root2", "Root3" };

    /// <summary>暗転の移動で巡る順。奥まで行って拠点へ戻る</summary>
    private static readonly (string Area, Arrival Arrival)[] Tour =
    {
        ("Root1", Arrival.FromPrevious),
        ("Root2", Arrival.FromPrevious),
        ("Root3", Arrival.FromPrevious),
        ("Root2", Arrival.FromNext),
        ("Root1", Arrival.FromNext),
        (TravelRule.BaseAreaId, Arrival.FromNext),
    };

    [UnityTest]
    public IEnumerator 場所を起こす重さと片付ける重さを書き出す()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive(TravelRule.BaseAreaId);

        var text = new StringBuilder();
        text.AppendLine($"計測: {(Application.isEditor ? "Editor" : Application.platform.ToString())} / {SystemInfo.graphicsDeviceType} / {Screen.width}x{Screen.height} / " +
                        $"裏の読み込みの優先度 {Application.backgroundLoadingPriority} / vSync {QualitySettings.vSyncCount} / 画質 {QualitySettings.names[QualitySettings.GetQualityLevel()]}");
        text.AppendLine("メモリはエディタ全体の数で、エディタが開いている資産は手放されない。見るのは差");
        text.AppendLine("いちばん重いコマの後ろは、そのコマで時間を食った処理（ミリ秒。入れ子の処理は親にも含まれる）");
        text.AppendLine();

        //倒れると暗転して拠点へ戻り、測る場面が変わる。半分を切ったら満たす
        PlayerHealth hp = GameObject.FindWithTag("Player").GetComponent<PlayerHealth>();

        using (var meter = new Meter())
        {
            meter.EveryFrame = () =>
            {
                if (hp.Current < hp.Max / 2) hp.SetCurrent(hp.Max);
            };

            yield return LoadBeside(meter, text);
            yield return TravelAround(meter, text);
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Report));
        File.WriteAllText(Report, text.ToString());
    }

    //拠点に居たまま、根源のシーンを足して読み、また手放す。ゲームの移動は通さないので、核や敵は出ない。
    private static IEnumerator LoadBeside(Meter meter, StringBuilder text)
    {
        text.AppendLine("[隣を足す] 拠点に居たまま根源のシーンを足して読み、手放す");

        //場所はどれも同じ原点の周りに作られていて、足すと拠点と重なる。重なった当たりにプレイヤーが弾かれないよう止める
        Rigidbody body = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        bool wasKinematic = body.isKinematic;
        body.isKinematic = true;

        try
        {
            foreach (string id in Beside)
            {
                Memory before = Memory.Now();
                var clock = Stopwatch.StartNew();
                int from = meter.Count;

                AsyncOperationHandle<SceneInstance> reading = Addressables.LoadSceneAsync(id, LoadSceneMode.Additive, false);
                while (!reading.IsDone) { yield return null; meter.Sample(); }
                Assert.AreEqual(AsyncOperationStatus.Succeeded, reading.Status, id + " を読めない");
                double readMs = clock.Elapsed.TotalMilliseconds;
                int read = meter.Count;

                AsyncOperation loading = reading.Result.ActivateAsync();
                while (!loading.isDone) { yield return null; meter.Sample(); }
                double wakeMs = clock.Elapsed.TotalMilliseconds - readMs;
                int woke = meter.Count;

                for (int i = 0; i < StartFrames; i++) { yield return null; meter.Sample(); }
                int started = meter.Count;

                Memory loaded = Memory.Now();
                Scene scene = SceneManager.GetSceneByName(id);
                string census = Census(scene);

                clock.Restart();
                int unloadFrom = meter.Count;
                AsyncOperation unloading = SceneManager.UnloadSceneAsync(scene);
                while (!unloading.isDone) { yield return null; meter.Sample(); }
                double dropMs = clock.Elapsed.TotalMilliseconds;
                int dropped = meter.Count;

                for (int i = 0; i < AfterFrames; i++) { yield return null; meter.Sample(); }
                Memory unloaded = Memory.Now();

                clock.Restart();
                int sweepFrom = meter.Count;
                AsyncOperation sweeping = Resources.UnloadUnusedAssets();
                while (!sweeping.isDone) { yield return null; meter.Sample(); }
                double sweepMs = clock.Elapsed.TotalMilliseconds;
                int swept = meter.Count;
                Memory released = Memory.Now();

                text.AppendLine($"{id}: {census}");
                text.AppendLine($"  読む（裏で。起こす手前まで） {readMs:F0} ms / {meter.Describe(from, read)}");
                text.AppendLine($"  起こす（Awake まで） {wakeMs:F0} ms / {meter.Describe(read, woke)}");
                text.AppendLine($"  起こしたあとの {StartFrames} コマ（Start） / {meter.Describe(woke, started)}");
                text.AppendLine($"  シーンを手放す {dropMs:F0} ms / {meter.Describe(unloadFrom, dropped)}");
                text.AppendLine($"  使われなくなった資産を手放す {sweepMs:F0} ms / {meter.Describe(sweepFrom, swept)}");
                text.AppendLine($"  メモリ（足す前から）: 起こしたあと {loaded.Minus(before)} / シーンを手放したあと {unloaded.Minus(before)} / 資産を手放したあと {released.Minus(before)}");
            }
        }
        finally
        {
            body.isKinematic = wasKinematic;
        }

        text.AppendLine();
    }

    //今の暗転の移動。覆うのと開けるのにかかる時間も含む。巡るたびのメモリで、手放されずに積み上がる量を見る
    private static IEnumerator TravelAround(Meter meter, StringBuilder text)
    {
        text.AppendLine("[暗転の移動] 覆い始めから開け終わるまで。メモリは巡り始める前から");

        AreaTravel travel = Travel();
        Memory start = Memory.Now();

        foreach ((string area, Arrival arrival) in Tour)
        {
            string from = travel.Current.Id;
            int first = meter.Count;
            var clock = Stopwatch.StartNew();

            travel.Go(Route.To(area, arrival));
            float giveUp = Time.realtimeSinceStartup + TravelSeconds;
            while ((travel.IsTraveling || travel.Current.Id != area) && Time.realtimeSinceStartup < giveUp) { yield return null; meter.Sample(); }

            Assert.AreEqual(area, travel.Current.Id, area + " に着かない");
            Assert.IsFalse(travel.IsTraveling, "移動が終わらない");
            double ms = clock.Elapsed.TotalMilliseconds;
            int last = meter.Count;

            for (int i = 0; i < SettleFrames; i++) { yield return null; meter.Sample(); }

            text.AppendLine($"{from} → {area}: {ms:F0} ms / {meter.Describe(first, last)}");
            text.AppendLine($"  着いたあとのメモリ: {Memory.Now().Minus(start)}");
        }

        var sweepClock = Stopwatch.StartNew();
        int sweepFrom = meter.Count;
        AsyncOperation sweeping = Resources.UnloadUnusedAssets();
        while (!sweeping.isDone) { yield return null; meter.Sample(); }
        text.AppendLine($"巡ったあと、使われなくなった資産を手放す {sweepClock.Elapsed.TotalMilliseconds:F0} ms / {meter.Describe(sweepFrom, meter.Count)}");
        text.AppendLine($"  手放したあとのメモリ: {Memory.Now().Minus(start)}");
        text.AppendLine();
    }

    /// <summary>シーンにある物の数。止めてある物も数える。起こす重さは物の数に比例する</summary>
    private static string Census(Scene scene)
    {
        int objects = 0;
        int inactive = 0;
        int behaviours = 0;
        int renderers = 0;
        int colliders = 0;
        int lights = 0;

        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform each in root.GetComponentsInChildren<Transform>(true))
            {
                objects++;
                if (!each.gameObject.activeInHierarchy) inactive++;
            }

            behaviours += root.GetComponentsInChildren<MonoBehaviour>(true).Length;
            renderers += root.GetComponentsInChildren<Renderer>(true).Length;
            colliders += root.GetComponentsInChildren<Collider>(true).Length;
            lights += root.GetComponentsInChildren<Light>(true).Length;
        }

        return $"物 {objects}（止めてある {inactive}）/ スクリプト {behaviours} / 描く物 {renderers} / 当たり {colliders} / 明かり {lights}";
    }

    /// <summary>メモリの量。エディタではエディタ自身の分も入る</summary>
    private readonly struct Memory
    {
        private readonly long _allocated;
        private readonly long _reserved;
        private readonly long _managed;
        private readonly long _graphics;

        private Memory(long allocated, long reserved, long managed, long graphics)
        {
            this._allocated = allocated;
            this._reserved = reserved;
            this._managed = managed;
            this._graphics = graphics;
        }

        public static Memory Now()
        {
            return new Memory(Profiler.GetTotalAllocatedMemoryLong(), Profiler.GetTotalReservedMemoryLong(),
                              Profiler.GetMonoUsedSizeLong(), Profiler.GetAllocatedMemoryForGraphicsDriver());
        }

        public string Minus(Memory baseline)
        {
            return $"確保 {Mb(_allocated - baseline._allocated)} / 予約 {Mb(_reserved - baseline._reserved)} / " +
                   $"C# {Mb(_managed - baseline._managed)} / 描画 {Mb(_graphics - baseline._graphics)}";
        }

        private static string Mb(long bytes)
        {
            return (bytes / 1048576.0).ToString("+0.0;-0.0;0.0") + " MB";
        }
    }

    /// <summary>コマごとの長さと、そのコマで時間を食った処理。値は前のコマのもので、次のコマの初めに読む</summary>
    private sealed class Meter : IDisposable
    {
        //PlayerLoop の段（Update.ScriptRunBehaviourUpdate など）と、読み込み・起こす・片付けの処理
        private static readonly Regex LoopStep = new Regex(@"^(Initialization|EarlyUpdate|FixedUpdate|PreUpdate|Update|PreLateUpdate|PostLateUpdate|TimeUpdate)\.\w+$");

        private static readonly string[] Keywords =
        {
            "Loading.", "Integrate", "Preload", "Unload", "AwakeFromLoad", "Destroy", "GC.Collect", "GarbageCollect",
            "Instantiate", "Physics.Sync", "Shader", "CreateGPUProgram", "Camera.Render", "Coroutines",
        };

        private sealed class Frame
        {
            public double Ms;
            public double Dt;
            public List<(string Name, double Ms)> Top;
        }

        public Action EveryFrame;

        private ProfilerRecorder _mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        private readonly List<(string Name, ProfilerRecorder Recorder)> _markers = new List<(string Name, ProfilerRecorder Recorder)>();
        private readonly List<Frame> _frames = new List<Frame>();

        public Meter()
        {
            //描く側のスレッドと読み込みのスレッドの待ちを混ぜないよう、メインスレッドの分だけ数える
            const ProfilerRecorderOptions MainOnly = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;

                string name = description.Name;
                if (!LoopStep.IsMatch(name) && !Keywords.Any(keyword => name.Contains(keyword))) continue;

                _markers.Add((name, new ProfilerRecorder(handle, 1, MainOnly)));
            }
        }

        public int Count
        {
            get { return _frames.Count; }
        }

        /// <summary>終わったコマを控える。毎コマ、yield return null の直後に呼ぶ</summary>
        public void Sample()
        {
            EveryFrame?.Invoke();

            double dt = Time.unscaledDeltaTime * 1000.0;
            double ms = _mainThread.Valid && _mainThread.LastValue > 0 ? _mainThread.LastValue / 1e6 : dt;

            var found = new List<(string Name, double Ms)>();
            foreach ((string name, ProfilerRecorder recorder) in _markers)
            {
                double spent = recorder.LastValue / 1e6;
                if (spent >= MarkerFloorMs) found.Add((name, spent));
            }

            _frames.Add(new Frame { Ms = ms, Dt = dt, Top = found.OrderByDescending(marker => marker.Ms).Take(TopMarkers).ToList() });
        }

        public string Describe(int from, int to)
        {
            if (to <= from) return "コマ無し";

            List<Frame> span = _frames.GetRange(from, to - from);
            Frame worst = span.OrderByDescending(frame => frame.Ms).First();
            string top = worst.Top.Count == 0 ? "" : " — " + string.Join("、", worst.Top.Select(marker => $"{marker.Name} {marker.Ms:F1}"));

            return $"{span.Count} コマ、合計 {span.Sum(frame => frame.Ms):F0} ms、{SpikeMs:F0} ms 超 {span.Count(frame => frame.Ms > SpikeMs)} コマ、" +
                   $"いちばん重いコマ {worst.Ms:F1} ms（間隔 {worst.Dt:F1}）" + top;
        }

        public void Dispose()
        {
            _mainThread.Dispose();
            foreach ((string _, ProfilerRecorder recorder) in _markers) recorder.Dispose();
        }
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトを捨てる()
    {
        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        yield return null;
    }

    //着いて、着いた場所の Start とコルーチンが一巡するまで待つ
    private static IEnumerator Arrive(string areaId)
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;

        while (Time.realtimeSinceStartup < giveUp)
        {
            if (travel.Current != null && travel.Current.Id == areaId && !travel.IsTraveling) break;
            yield return null;
        }

        Assert.IsNotNull(travel.Current, "場所に着かない");
        Assert.AreEqual(areaId, travel.Current.Id, areaId + " に着かない");
        Assert.IsFalse(travel.IsTraveling, "移動が終わらない");

        for (int i = 0; i < SettleFrames; i++) yield return null;
    }

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
    }
}
