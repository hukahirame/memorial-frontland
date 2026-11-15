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
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using Stopwatch = System.Diagnostics.Stopwatch;

//ときどき重くなるコマを探す。根源で、湧き・倒す・保存・零時の知らせなどを 1 つずつ起こしてから、しばらく何もせずに流し、
//長くかかったコマと、そのコマで時間を食った処理を Temp/Hitches.txt に書く。判定はしないので、名指しで回したときだけ走る
[Explicit]
public class Hitches
{
    private const float WarmSeconds = 5f;

    /// <summary>起こしたことの重さを探す長さ（実時間の秒）。倒した敵は 0.25 秒弾けてから落とし物を出す</summary>
    private const float WindowSeconds = 1f;

    /// <summary>窓のあと、次を起こすまでの間（実時間の秒）</summary>
    private const float GapSeconds = 1f;

    /// <summary>何も起こさずに流す長さ（実時間の秒）。湧きの連鎖は 15〜30 秒ごと</summary>
    private const float IdleSeconds = 60f;

    private const int SaveRepeats = 10;

    /// <summary>重いとみなす下限（ミリ秒）。温めたときの中央値の 2.5 倍がこれより長ければ、そちらを使う</summary>
    private const double SpikeFloorMs = 20.0;

    private const double MarkerFloorMs = 0.3;
    private const int TopMarkers = 6;
    private const int MaxSpikeLines = 120;

    private const float TravelSeconds = 10f;
    private const int SettleFrames = 120;

    /// <summary>保存と同じ書き方を、保存と同じ場所で試すときのファイル。保存の枠とは名前を分ける</summary>
    private const string ProbeFile = ".hitches-probe.json";

    private static readonly string Report = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "Hitches.txt"));

    [UnityTest]
    public IEnumerator 根源で動かして重いコマを書き出す()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();
        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();

        Component player = GameObject.FindWithTag("Player").GetComponent(GameType("Player2"));
        //隣の根源も読み込んであるので、今いる根源の湧き口を探す
        Component spawner = UnityEngine.Object.FindObjectsByType(GameType("OF_Spawner"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.gameObject.scene == SceneManager.GetActiveScene());
        Component roots = GameObject.FindWithTag("RootsManager").GetComponent(GameType("RootsManager"));
        Component save = GameObject.Find("GameManager").GetComponent(GameType("SaveSystem"));
        Assert.IsNotNull(spawner, "根源に湧き口が無い");

        //倒れると暗転して拠点へ戻り、計る場面が変わる。半分を切ったら満たす
        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);

        var watch = new Watch();
        watch.EveryFrame = () =>
        {
            if (hp.IsReady && hp.Current < hp.Max / 2) hp.SetCurrent(hp.Max);
        };

        try
        {
            yield return watch.Run(WarmSeconds, false);
            watch.SetThreshold();

            yield return watch.Probe("保存（自動保存と同じ処理）", () => SaveLikeAutosave(save));
            yield return watch.Probe("敵が 1 体湧く", () => Spawn(spawner, 1));
            yield return watch.Probe("敵が 5 体いっせいに湧く", () => Spawn(spawner, 5));
            yield return watch.Probe("全員に「！」を出す", Alert);
            yield return watch.Probe("敵を 1 体倒す", Kill);
            yield return watch.Probe("零時の処理と知らせ", () => { Call(roots, "Dayover"); return ""; });
            yield return watch.Probe("攻撃を振る", () => Swing(player));
            yield return watch.Probe("回避して残像を出す", () => Roll(player));
            yield return watch.Probe("中くらいの文字を出す", () => { UnityEngine.Object.FindAnyObjectByType<MiddleText>().Midtxt_Anim("計測のための知らせです"); return ""; });
            yield return watch.Probe("核の候補を叩く（頭上の帯を描き直す）", HitCandidate);

            Fence fence = null;
            yield return watch.Probe("柵を置く", () => { fence = PlaceFence(player.transform); return ""; });
            yield return watch.Probe("柵を削る（頭上の帯を描き直す）", () => { fence.Ram(5, fence.transform.position + Vector3.forward); return ""; });
            for (int i = 0; i < SaveRepeats; i++)
            {
                yield return watch.Probe("保存 " + (i + 2) + " 回目", () => SaveLikeAutosave(save));
            }

            watch.Mark("ここから何も起こさずに流す");
            yield return watch.Run(IdleSeconds, false);

            bool written = FinishProbeWrites();
            Directory.CreateDirectory(Path.GetDirectoryName(Report));
            File.WriteAllText(Report, "裏で書いた保存のファイル: " + (written ? "あり" : "なし") + "\n" + watch.Describe());
        }
        finally
        {
            watch.Dispose();
        }
    }

    //描く物を数える。拠点と根源1 で、何をいくつ描き、そのうちいくつが影を落とし、まとめて描かれているかを Temp/Renderers.txt に書く
    [UnityTest]
    public IEnumerator 拠点と根源で描く物を数える()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();

        var text = new StringBuilder();
        CountRenderers(text, "拠点");

        Travel().Go(Route.To("Root1", Arrival.FromPrevious));
        yield return Arrive();
        CountRenderers(text, "根源1");
        CountLights(text);

        //画質の段ごとに、描く回数と描く時間を測る。段はエディタで遊ぶときの設定にも残るので、測り終えたら戻す
        int original = QualitySettings.GetQualityLevel();
        try
        {
            text.AppendLine($"[画質の段ごと] 画面 {Screen.width}x{Screen.height}。段 / batches / SetPass / draw calls / 影を落とす回数 / Camera.Render（ms）/ GPU（ms）。どれも中央値");
            for (int level = QualitySettings.names.Length - 1; level >= 0; level--)
            {
                QualitySettings.SetQualityLevel(level, true);
                for (int i = 0; i < 30; i++) yield return null;

                var batches = new List<long>();
                var setPass = new List<long>();
                var drawCalls = new List<long>();
                var casters = new List<long>();
                var render = new List<double>();
                var gpu = new List<double>();
                var timings = new FrameTiming[1];
                using (ProfilerRecorder batchesRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Batches Count"))
                using (ProfilerRecorder setPassRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "SetPass Calls Count"))
                using (ProfilerRecorder drawCallsRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Draw Calls Count"))
                using (ProfilerRecorder castersRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Shadow Casters Count"))
                using (ProfilerRecorder renderRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Render, "Camera.Render", 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.CollectOnlyOnCurrentThread))
                {
                    yield return null;
                    for (int i = 0; i < 90; i++)
                    {
                        FrameTimingManager.CaptureFrameTimings();
                        yield return null;
                        if (FrameTimingManager.GetLatestTimings(1, timings) > 0 && timings[0].gpuFrameTime > 0) gpu.Add(timings[0].gpuFrameTime);
                        batches.Add(batchesRecorder.LastValue);
                        setPass.Add(setPassRecorder.LastValue);
                        drawCalls.Add(drawCallsRecorder.LastValue);
                        casters.Add(castersRecorder.LastValue);
                        render.Add(renderRecorder.LastValue / 1e6);
                    }
                }

                string gpuMs = gpu.Count > 0 ? gpu.OrderBy(v => v).ElementAt(gpu.Count / 2).ToString("F2") : "取れない";
                text.AppendLine($"  {QualitySettings.names[level]} / {Median(batches)} / {Median(setPass)} / {Median(drawCalls)} / {Median(casters)} / {render.OrderBy(v => v).ElementAt(render.Count / 2):F2} / {gpuMs}" +
                                $"（影の段 {QualitySettings.shadowCascades}、影の距離 {QualitySettings.shadowDistance}、影の細かさ {QualitySettings.shadowResolution}、" +
                                $"1 画素ずつ照らす光 {QualitySettings.pixelLightCount}、影 {QualitySettings.shadows}、MSAA {QualitySettings.antiAliasing}）");
            }
        }
        finally
        {
            QualitySettings.SetQualityLevel(original, true);
        }

        File.WriteAllText(Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "Renderers.txt")), text.ToString());
    }

    private static long Median(List<long> values)
    {
        return values.OrderBy(v => v).ElementAt(values.Count / 2);
    }

    private static void CountLights(StringBuilder text)
    {
        text.AppendLine("[明かり] 名前 / シーン / 種類 / 影 / 照らし方 / 届く距離 / 強さ");
        foreach (Light light in UnityEngine.Object.FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.enabled && l.gameObject.activeInHierarchy))
        {
            text.AppendLine($"  {light.name} / {light.gameObject.scene.name} / {light.type} / {light.shadows} / {light.renderMode} / {light.range:F1} / {light.intensity:F2}");
        }

        Camera eye = Camera.main;
        if (eye != null)
        {
            text.AppendLine($"[カメラ] {eye.name} / 描き方 {eye.actualRenderingPath} / 深さの絵 {eye.depthTextureMode} / HDR {eye.allowHDR} / MSAA {eye.allowMSAA} / 部品 " +
                            string.Join("、", eye.GetComponents<Component>().Select(c => c.GetType().Name)));
        }

        text.AppendLine();
    }

    private static void CountRenderers(StringBuilder text, string label)
    {
        List<Renderer> drawn = UnityEngine.Object.FindObjectsByType<Renderer>(FindObjectsSortMode.None)
            .Where(renderer => renderer.enabled && renderer.gameObject.activeInHierarchy)
            .ToList();

        text.AppendLine($"[{label}] 描く物 {drawn.Count}（いま見えている {drawn.Count(r => r.isVisible)}、影を落とす {drawn.Count(Casts)}、" +
                        $"まとめて描かれている {drawn.Count(r => r.isPartOfStaticBatch)}、static の印 {drawn.Count(r => r.gameObject.isStatic)}）");
        foreach (IGrouping<string, Renderer> scene in drawn.GroupBy(r => r.gameObject.scene.name))
        {
            text.AppendLine($"  シーン {scene.Key}: {scene.Count()}（影を落とす {scene.Count(Casts)}）");
        }

        text.AppendLine("  種類ごと（数 / 見えている / 影を落とす / static / 種類 / 形 / 素材 / 素材の GPU インスタンシング）");
        foreach (IGrouping<string, Renderer> kind in drawn.GroupBy(Kind).OrderByDescending(g => g.Count()).Take(45))
        {
            text.AppendLine($"  {kind.Count()} / {kind.Count(r => r.isVisible)} / {kind.Count(Casts)} / {kind.Count(r => r.gameObject.isStatic)} / {kind.Key}");
        }

        text.AppendLine("  いちばん上の親ごと（数 / 影を落とす / 名前）");
        foreach (IGrouping<string, Renderer> top in drawn.GroupBy(r => r.gameObject.scene.name + "/" + r.transform.root.name).OrderByDescending(g => g.Count()).Take(20))
        {
            text.AppendLine($"  {top.Count()} / {top.Count(Casts)} / {top.Key}");
        }

        text.AppendLine();
    }

    private static bool Casts(Renderer renderer)
    {
        return renderer.shadowCastingMode != UnityEngine.Rendering.ShadowCastingMode.Off;
    }

    private static string Kind(Renderer renderer)
    {
        string shape = renderer.GetType().Name;
        if (renderer is SpriteRenderer sprite) shape = sprite.sprite != null ? "絵 " + sprite.sprite.name : "絵なし";
        else if (renderer is MeshRenderer)
        {
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            shape = filter != null && filter.sharedMesh != null ? filter.sharedMesh.name + "（" + filter.sharedMesh.vertexCount + " 頂点）" : "形なし";
        }

        Material[] materials = renderer.sharedMaterials;
        string names = string.Join("+", materials.Select(m => m != null ? m.name : "なし"));
        string instancing = string.Join("+", materials.Select(m => m != null && m.enableInstancing ? "あり" : "なし"));
        return $"{renderer.GetType().Name} / {shape} / {names} / {instancing}";
    }

    //SaveSystem.Save と同じ手順。保存の枠に書くと遊んでいる保存を上書きするので、書く先だけ変える。
    //渡すのは、ファイルに書く仕事を書き込みの列に積むまで。書くのは裏なので、ここでは待たない
    private static string SaveLikeAutosave(Component save)
    {
        object data = Field(save, "savedata").GetValue(save);
        AreaTravel travel = (AreaTravel)Field(save, "travel").GetValue(save);
        object placed = Field(save, "placed").GetValue(save);
        MethodInfo sync = data.GetType().GetMethod("SyncStatic");
        MethodInfo write = GameType("SaveSystem").GetMethod("Write", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(write, "SaveSystem.Write が見つからない");

        var clock = Stopwatch.StartNew();
        sync.Invoke(data, new object[] { travel.Where, placed });
        double copy = clock.Elapsed.TotalMilliseconds;

        clock.Restart();
        string json = JsonUtility.ToJson(data);
        double serialize = clock.Elapsed.TotalMilliseconds;

        clock.Restart();
        write.Invoke(null, new object[] { Path.Combine(Application.persistentDataPath, ProbeFile), json });
        double hand = clock.Elapsed.TotalMilliseconds;

        return $"写す {copy:F2} ms、JSON {serialize:F2} ms（{json.Length} 字）、渡す {hand:F2} ms";
    }

    /// <summary>裏で書いている保存を書き終えるまで待ち、書けたかを返す。書いたファイルは消す</summary>
    private static bool FinishProbeWrites()
    {
        MethodInfo finish = GameType("SaveSystem").GetMethod("FinishWriting", BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(finish, "SaveSystem.FinishWriting が見つからない");
        finish.Invoke(null, null);

        string probe = Path.Combine(Application.persistentDataPath, ProbeFile);
        bool written = File.Exists(probe);
        if (written) File.Delete(probe);
        if (File.Exists(probe + ".tmp")) File.Delete(probe + ".tmp");
        return written;
    }

    private static string Spawn(Component spawner, int count)
    {
        float range = (float)Field(spawner, "spawnrange").GetValue(spawner);
        for (int i = 0; i < count; i++) Call(spawner, "Spawn", range);

        return "敵 " + GameObject.FindGameObjectsWithTag("Enemy").Length + " 体";
    }

    private static string Alert()
    {
        int count = 0;
        foreach (Component slime in Slimes())
        {
            AlertMark.Show(slime.transform.Find("Canvas"));
            count++;
        }

        return count + " 体";
    }

    private static string Kill()
    {
        PropertyInfo hp = GameType("Slime").GetProperty("CurrentHp");
        Component target = Slimes().FirstOrDefault(slime => (int)hp.GetValue(slime) > 0);
        if (target == null) return "倒す敵がいない";

        Call(target, "TakeHit", 9999, Vector3.zero);
        return "";
    }

    private static string Swing(Component player)
    {
        Call(player, "AttackDown");
        Call(player, "AttackUp");
        ((MonoBehaviour)player).StartCoroutine((IEnumerator)Call(player, "Attack"));
        return "";
    }

    private static string Roll(Component player)
    {
        ((MonoBehaviour)player).StartCoroutine((IEnumerator)Call(player, "Roll", 1f, 1f));
        return "";
    }

    private static string HitCandidate()
    {
        Component candidate = UnityEngine.Object.FindObjectsByType(GameType("SpawnerCandidate"), FindObjectsSortMode.None)
            .Cast<Component>()
            .FirstOrDefault(found => found.gameObject.activeInHierarchy);
        if (candidate == null) return "核の候補がない";

        Call(candidate, "Candidate", 1);
        return "";
    }

    private static Fence PlaceFence(Transform player)
    {
        GameObject prefab = Resources.Load<GameObject>("Fence_obj");
        return UnityEngine.Object.Instantiate(prefab, player.position + Vector3.right * 3f, Quaternion.identity).GetComponent<Fence>();
    }

    private static IEnumerable<Component> Slimes()
    {
        return UnityEngine.Object.FindObjectsByType(GameType("Slime"), FindObjectsSortMode.None).Cast<Component>();
    }

    /// <summary>コマごとの長さと、重いコマで時間を食った処理を集める。値は前のコマのもので、次のコマの初めに読む</summary>
    private sealed class Watch : IDisposable
    {
        //PlayerLoop の段（Update.ScriptRunBehaviourUpdate など）。段に分ければ、重いコマがどこで時間を食ったかが分かる
        private static readonly Regex LoopStep = new Regex(@"^(Initialization|EarlyUpdate|FixedUpdate|PreUpdate|Update|PreLateUpdate|PostLateUpdate|TimeUpdate)\.\w+$");

        private static readonly string[] Keywords =
        {
            "GC.Collect", "Instantiate", "Font", "Shader", "CreateGPUProgram", "Loading.", "Canvas.", "Layout", "TextMesh", "TMP",
            "Physics.Simulate", "Physics.Processing", "Gfx.Wait", "WaitForTargetFPS", "Log", "Camera.Render", "Cook", "Culling",
            "Shadows", "Drawing", "Render.OpaqueGeometry", "Render.TransparentGeometry", "ImageEffects", "PostProcess",
        };

        /// <summary>描くものの数。編集中と開発版でだけ数えられる</summary>
        private static readonly string[] RenderCounters =
        {
            "Batches Count", "SetPass Calls Count", "Draw Calls Count", "Shadow Casters Count", "Vertices Count",
        };

        private sealed class Sample
        {
            public float At;
            public double Ms;
            public double Dt;
            public double PlayerMs;
            public long Alloc;
            public List<(string Name, double Ms)> Top;
        }

        private sealed class ProbeResult
        {
            public string Label;
            public string Note;
            public double CallMs;
            public int From;
            public int To;
        }

        public Action EveryFrame;

        private ProfilerRecorder mainThread = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread");
        private ProfilerRecorder allocated = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
        private ProfilerRecorder playerLoop;
        private readonly List<(string Name, ProfilerRecorder Recorder)> markers = new List<(string Name, ProfilerRecorder Recorder)>();
        private readonly List<(string Name, ProfilerRecorder Recorder, List<long> Values)> counters = new List<(string Name, ProfilerRecorder Recorder, List<long> Values)>();

        /// <summary>処理ごとの、全コマを通した合計（ミリ秒）。ふだんのコマが何に時間を使っているかを見る</summary>
        private double[] totals;
        private readonly List<Sample> samples = new List<Sample>();
        private readonly List<ProbeResult> probes = new List<ProbeResult>();
        private readonly List<(float At, string Text)> logs = new List<(float At, string Text)>();
        private readonly List<(int Index, string Text)> marks = new List<(int Index, string Text)>();
        private readonly float startedAt = Time.realtimeSinceStartup;
        private readonly int collectionsAtStart = GC.CollectionCount(0);
        private double threshold = double.MaxValue;

        public Watch()
        {
            //描く側のスレッドの待ちを混ぜないよう、メインスレッドの分だけ数える
            const ProfilerRecorderOptions MainOnly = ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.CollectOnlyOnCurrentThread;

            var handles = new List<ProfilerRecorderHandle>();
            ProfilerRecorderHandle.GetAvailable(handles);
            foreach (ProfilerRecorderHandle handle in handles)
            {
                ProfilerRecorderDescription description = ProfilerRecorderHandle.GetDescription(handle);
                string name = description.Name;

                if (RenderCounters.Contains(name) && description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds)
                {
                    counters.Add((name, new ProfilerRecorder(handle, 1, ProfilerRecorderOptions.Default | ProfilerRecorderOptions.StartImmediately), new List<long>()));
                    continue;
                }
                if (description.UnitType != ProfilerMarkerDataUnit.TimeNanoseconds) continue;

                if (name == "PlayerLoop")
                {
                    playerLoop = new ProfilerRecorder(handle, 1, MainOnly);
                    continue;
                }
                if (!LoopStep.IsMatch(name) && !Keywords.Any(keyword => name.Contains(keyword))) continue;

                markers.Add((name, new ProfilerRecorder(handle, 1, MainOnly)));
            }

            totals = new double[markers.Count];
            Application.logMessageReceived += OnLog;
        }

        public IEnumerator Run(float seconds, bool detail)
        {
            float until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until)
            {
                yield return null;
                Record(detail);
            }
        }

        /// <summary>act を今のコマで起こし、そのあと窓のあいだのコマを細かく記録する</summary>
        public IEnumerator Probe(string label, Func<string> act)
        {
            var result = new ProbeResult { Label = label };
            var clock = Stopwatch.StartNew();
            try
            {
                result.Note = act();
            }
            catch (Exception e)
            {
                result.Note = "失敗: " + (e.InnerException ?? e).Message;
            }
            result.CallMs = clock.Elapsed.TotalMilliseconds;

            result.From = samples.Count;
            yield return Run(WindowSeconds, true);
            result.To = samples.Count;
            probes.Add(result);

            yield return Run(GapSeconds, false);
        }

        public void SetThreshold()
        {
            threshold = Math.Max(SpikeFloorMs, Percentile(s => s.Ms, 0.5) * 2.5);
        }

        public void Mark(string text)
        {
            marks.Add((samples.Count, text));
        }

        private void Record(bool detail)
        {
            EveryFrame?.Invoke();

            double dt = Time.unscaledDeltaTime * 1000.0;
            double ms = mainThread.Valid && mainThread.LastValue > 0 ? mainThread.LastValue / 1e6 : dt;
            var sample = new Sample
            {
                At = Time.realtimeSinceStartup - startedAt,
                Ms = ms,
                Dt = dt,
                PlayerMs = playerLoop.Valid ? playerLoop.LastValue / 1e6 : 0.0,
                Alloc = allocated.Valid ? allocated.LastValue : 0L,
            };
            if (detail || ms > threshold || dt > threshold) sample.Top = Breakdown();
            samples.Add(sample);

            for (int i = 0; i < markers.Count; i++) totals[i] += markers[i].Recorder.LastValue / 1e6;
            foreach ((string _, ProfilerRecorder recorder, List<long> values) in counters) values.Add(recorder.LastValue);
        }

        private List<(string Name, double Ms)> Breakdown()
        {
            var found = new List<(string Name, double Ms)>();
            foreach ((string name, ProfilerRecorder recorder) in markers)
            {
                double ms = recorder.LastValue / 1e6;
                if (ms >= MarkerFloorMs) found.Add((name, ms));
            }

            return found.OrderByDescending(marker => marker.Ms).Take(TopMarkers).ToList();
        }

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            string text = condition.Replace("\n", " ");
            if (text.Length > 40) text = text.Substring(0, 40);
            logs.Add((Time.realtimeSinceStartup - startedAt, text));
        }

        public string Describe()
        {
            var text = new StringBuilder();
            float seconds = samples.Count > 0 ? samples[samples.Count - 1].At : 0f;
            text.AppendLine($"計測: {(Application.isEditor ? "Editor" : Application.platform.ToString())} / {Screen.width}x{Screen.height} / vSync {QualitySettings.vSyncCount} / targetFrameRate {Application.targetFrameRate} / {seconds:F0} 秒 / {samples.Count} コマ / 記録した処理 {markers.Count} 種 / Main Thread {(mainThread.Valid ? "あり" : "なし")} / PlayerLoop {(playerLoop.Valid ? "あり" : "なし")}");
            text.AppendLine($"メインスレッド: 中央値 {Percentile(s => s.Ms, 0.5):F1} ms / 95% {Percentile(s => s.Ms, 0.95):F1} / 99% {Percentile(s => s.Ms, 0.99):F1} / 最大 {Percentile(s => s.Ms, 1.0):F1}");
            text.AppendLine($"うち PlayerLoop: 中央値 {Percentile(s => s.PlayerMs, 0.5):F1} ms / 95% {Percentile(s => s.PlayerMs, 0.95):F1} / 99% {Percentile(s => s.PlayerMs, 0.99):F1} / 最大 {Percentile(s => s.PlayerMs, 1.0):F1}");
            text.AppendLine($"PlayerLoop の外（Editor と待ち）: 中央値 {Percentile(s => s.Ms - s.PlayerMs, 0.5):F1} ms / 95% {Percentile(s => s.Ms - s.PlayerMs, 0.95):F1} / 最大 {Percentile(s => s.Ms - s.PlayerMs, 1.0):F1}");
            text.AppendLine($"コマの間隔: 中央値 {Percentile(s => s.Dt, 0.5):F1} ms / 95% {Percentile(s => s.Dt, 0.95):F1} / 99% {Percentile(s => s.Dt, 0.99):F1} / 最大 {Percentile(s => s.Dt, 1.0):F1}");
            text.AppendLine($"重いとみなす線: {threshold:F1} ms。メインスレッドが超えたコマ {samples.Count(s => s.Ms > threshold)}、33 ms 超 {samples.Count(s => s.Ms > 33)}、50 ms 超 {samples.Count(s => s.Ms > 50)}、100 ms 超 {samples.Count(s => s.Ms > 100)}");
            long allocated = samples.Sum(s => s.Alloc);
            text.AppendLine($"GC: 確保は 1 コマ平均 {allocated / Math.Max(1, samples.Count) / 1024.0:F1} KB、合計 {allocated / 1048576.0:F1} MB。回収 {GC.CollectionCount(0) - collectionsAtStart} 回");
            foreach ((string name, ProfilerRecorder _, List<long> values) in counters)
            {
                if (values.Count == 0) continue;
                List<long> sorted = values.OrderBy(v => v).ToList();
                text.AppendLine($"{name}: 中央値 {sorted[sorted.Count / 2]} / 最大 {sorted[sorted.Count - 1]}");
            }
            text.AppendLine();

            text.AppendLine("[ふだんのコマ] 1 コマあたりの平均（ミリ秒）。入れ子の処理は親にも含まれる");
            int frames = Math.Max(1, samples.Count);
            foreach (int i in Enumerable.Range(0, markers.Count).OrderByDescending(k => totals[k]).Take(16))
            {
                text.AppendLine($"{markers[i].Name} {totals[i] / frames:F2}");
            }
            text.AppendLine();

            text.AppendLine("[起こしたこと] 呼び出しの長さ / 窓の中でいちばん重いコマ / そのコマで時間を食った処理");
            foreach (ProbeResult probe in probes)
            {
                string note = string.IsNullOrEmpty(probe.Note) ? "" : " / " + probe.Note;
                text.AppendLine($"{probe.Label}: 呼び出し {probe.CallMs:F1} ms / {Line(Worst(probe.From, probe.To))}{note}");
            }
            text.AppendLine();

            //起こしたことの後始末が窓の外へはみ出すので、何も起こさずに流したあいだの重いコマだけを並べる
            int idleFrom = marks.Count > 0 ? marks[marks.Count - 1].Index : samples.Count;
            if (idleFrom < samples.Count) text.AppendLine($"[何も起こさずに流したあいだの重いコマ] +{samples[idleFrom].At:F1}s から");
            int lines = 0;
            for (int i = idleFrom; i < samples.Count && lines < MaxSpikeLines; i++)
            {
                Sample sample = samples[i];
                if (sample.Ms <= threshold && sample.Dt <= threshold) continue;

                text.AppendLine($"+{sample.At:F1}s {Line(sample)}{LogsNear(sample)}");
                lines++;
            }

            return text.ToString();
        }

        private Sample Worst(int from, int to)
        {
            Sample worst = null;
            for (int i = from; i < to && i < samples.Count; i++)
            {
                if (worst == null || samples[i].Ms > worst.Ms) worst = samples[i];
            }

            return worst;
        }

        private static string Line(Sample sample)
        {
            if (sample == null) return "記録なし";

            string top = sample.Top == null ? "" : string.Join("、", sample.Top.Select(marker => $"{marker.Name} {marker.Ms:F1}"));
            return $"{sample.Ms:F1} ms（PlayerLoop {sample.PlayerMs:F1}、間隔 {sample.Dt:F1}、確保 {sample.Alloc / 1024.0:F0} KB）" + (top.Length > 0 ? " — " + top : "");
        }

        private string LogsNear(Sample sample)
        {
            float from = sample.At - (float)(sample.Dt / 1000.0) - 0.01f;
            string[] near = logs.Where(log => log.At >= from && log.At <= sample.At).Select(log => log.Text).ToArray();
            return near.Length == 0 ? "" : " / ログ: " + string.Join("｜", near);
        }

        private double Percentile(Func<Sample, double> value, double q)
        {
            if (samples.Count == 0) return 0.0;

            List<double> sorted = samples.Select(value).OrderBy(v => v).ToList();
            return sorted[(int)Math.Round(q * (sorted.Count - 1))];
        }

        public void Dispose()
        {
            Application.logMessageReceived -= OnLog;
            mainThread.Dispose();
            allocated.Dispose();
            if (playerLoop.Valid) playerLoop.Dispose();
            foreach ((string _, ProfilerRecorder recorder) in markers) recorder.Dispose();
            foreach ((string _, ProfilerRecorder recorder, List<long> _) in counters) recorder.Dispose();
        }
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        ((RootRegistry)StaticField("RootsManager", "Roots").GetValue(null)).Clear();
        ((BaseRaid)StaticField("RootsManager", "Raid").GetValue(null)).Clear();
        ((QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null)).Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("RewardUI", "rewardUI_id").SetValue(null, "");
        StaticField("Player2", "cool").SetValue(null, false);

        Health hp = (Health)StaticField("Player2", "Hp").GetValue(null);
        if (hp.IsReady) hp.SetCurrent(hp.Max);

        FinishProbeWrites();

        yield return null;
    }

    //着いて、着いた場所の Start とコルーチンが一巡するまで待つ
    private static IEnumerator Arrive()
    {
        AreaTravel travel = Travel();
        float giveUp = Time.realtimeSinceStartup + TravelSeconds;

        while (Time.realtimeSinceStartup < giveUp)
        {
            if (travel.Current != null && !travel.IsTraveling) break;
            yield return null;
        }

        Assert.IsNotNull(travel.Current, "場所に着かない");
        Assert.IsFalse(travel.IsTraveling, "移動が終わらない");

        for (int i = 0; i < SettleFrames; i++) yield return null;
    }

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");

        return travel;
    }

    private static object Call(Component target, string methodName, params object[] args)
    {
        MethodInfo method = target.GetType().GetMethod(methodName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.IsNotNull(method, target.GetType().Name + "." + methodName + " が見つからない");

        return method.Invoke(target, args);
    }

    private static FieldInfo Field(Component target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
        Assert.IsNotNull(field, target.GetType().Name + "." + fieldName + " が見つからない");

        return field;
    }

    //Assembly-CSharp は asmdef から参照できないため名前で引く
    private static Type GameType(string typeName)
    {
        Type type = AppDomain.CurrentDomain.GetAssemblies()
            .Select(a => a.GetType(typeName))
            .FirstOrDefault(t => t != null);
        Assert.IsNotNull(type, typeName + " 型が見つからない");

        return type;
    }

    private static FieldInfo StaticField(string typeName, string fieldName)
    {
        FieldInfo field = GameType(typeName).GetField(fieldName,
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, typeName + "." + fieldName + " が見つからない");

        return field;
    }
}
