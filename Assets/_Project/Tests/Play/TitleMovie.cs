using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using MemorialFloor.Domain;
using MemorialFloor.Game;
#if UNITY_EDITOR
using UnityEditor.Media;
#endif

//タイトルの背景の動画を撮る。確かめる値は無く、StreamingAssets の動画を書き換えるので、名指しで回したときだけ走る。
//根源3で、タイトルの絵と同じ構図（主人公の後ろから吹き抜けを見る）に主人公とカメラを置き、敵・一日の巡り・画面の UI・
//カメラの子の落ち葉・被写界深度を止めて撮る。繰り返しの継ぎ目が見えないよう、終わりのあとを少し余分に撮り、始まりへ重ねて溶かす
[Explicit]
public class TitleMovie
{
    private const int FrameRate = 30;

    /// <summary>繰り返す長さ（コマ）。6 秒</summary>
    private const int LoopFrames = 180;

    /// <summary>継ぎ目で重ねて溶かす長さ（コマ）。0.8 秒</summary>
    private const int BlendFrames = 24;

    /// <summary>撮り始める前に回すコマ。滝やしぶきが出そろうまで</summary>
    private const int WarmupFrames = 90;

    /// <summary>動画の情報量（ビット毎秒）。6 秒で 4.5 MB ほど</summary>
    private const int BitRate = 6000000;

    /// <summary>書いたコマを絵でも残す間隔（コマ）。1 秒ごと。手元に動画を読む道具が無いので、これで中身を確かめる</summary>
    private const int SampleEvery = FrameRate;

    /// <summary>撮る画質の段。動画は前もって描くので遊ぶときの重さに関わらず、いちばん高い段で撮る。撮り終えたら Editor の段へ戻す</summary>
    private const string CaptureQuality = "Ultra";

    /// <summary>移動が終わるのを待つ上限（実時間の秒）</summary>
    private const float TravelSeconds = 10f;

    //タイトルの絵を撮ったときの置き方（STATE.md）。主人公の絵は板なので、カメラの向きへ回す
    private static readonly Vector3 HeroAt = new Vector3(3.9f, 0.65f, -1.5f);
    private static readonly Vector3 CameraAt = new Vector3(1.6f, 1.1f, -3.8f);
    private static readonly Vector3 LookAt = new Vector3(14f, 2.8f, 3.0f);
    private const float FieldOfView = 50f;

    private static readonly string Shots = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Temp", "UiShots"));

    [UnityTest]
    public IEnumerator タイトルの動画を撮る()
    {
        yield return SceneManager.LoadSceneAsync(AreaTravel.CoreScene, LoadSceneMode.Single);
        yield return Arrive();
        Travel().Go(Route.To("Root3", Arrival.FromPrevious));
        yield return Arrive();

        Calm();
        Camera eye = Frame();
        int capture = Array.IndexOf(QualitySettings.names, CaptureQuality);
        Assert.GreaterOrEqual(capture, 0, "画質の段 " + CaptureQuality + " が無い");

        var target = new RenderTexture(TitleBackdrop.MovieWidth, TitleBackdrop.MovieHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
        var frame = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
        eye.targetTexture = target;
        Time.captureFramerate = FrameRate;
        int editorQuality = QualitySettings.GetQualityLevel();
        QualitySettings.SetQualityLevel(capture, true);

        try
        {
            for (int i = 0; i < WarmupFrames; i++) yield return null;

            string path = Path.Combine(Application.dataPath, "StreamingAssets", TitleBackdrop.MovieFile);
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            Directory.CreateDirectory(Shots);
            var head = new List<Color32[]>();

#if UNITY_EDITOR
            //最初の BlendFrames コマは取っておき、終わりのあとのコマに重ねて溶かしてから最後に書く。
            //書く順は f[N..T-1]、溶かした N コマで、繰り返すと f[T-1]→f[T]、f[N-1]→f[N] とつながる
            using (MediaEncoder encoder = Encoder(path))
            {
                for (int i = 0; i < LoopFrames + BlendFrames; i++)
                {
                    yield return new WaitForEndOfFrame();
                    Read(target, frame);

                    if (i < BlendFrames)
                    {
                        head.Add(frame.GetPixels32());
                        continue;
                    }

                    if (i >= LoopFrames)
                    {
                        int j = i - LoopFrames;
                        Blend(frame, head[j], (float)j / BlendFrames);
                    }

                    //0 秒のコマは、撮り終えたら手で Art/Title/TitleBackground.png に写す（動画が流れ始めるまで下に敷く絵）
                    int written = i - BlendFrames;
                    if (written % SampleEvery == 0)
                    {
                        File.WriteAllBytes(Path.Combine(Shots, "title_movie_" + written / SampleEvery + "s.png"), frame.EncodeToPNG());
                    }

                    encoder.AddFrame(frame);
                }
            }
#endif
            Assert.IsTrue(File.Exists(path), "動画を書けなかった: " + path);
        }
        finally
        {
            Time.captureFramerate = 0;
            QualitySettings.SetQualityLevel(editorQuality, true);
            eye.targetTexture = null;
            target.Release();
            UnityEngine.Object.Destroy(frame);
        }
    }

    [UnityTearDown]
    public IEnumerator 持ち回しオブジェクトと静的状態を捨てる()
    {
        Time.timeScale = 1f;
        Time.captureFramerate = 0;

        foreach (GameObject go in UnityEngine.Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None))
        {
            if (go.scene.name == "DontDestroyOnLoad") UnityEngine.Object.Destroy(go);
        }

        ((RootRegistry)StaticField("RootsManager", "Roots").GetValue(null)).Clear();
        ((BaseRaid)StaticField("RootsManager", "Raid").GetValue(null)).Clear();
        ((QuestRegistry)StaticField("QuestManager", "Quests").GetValue(null)).Clear();
        StaticField("QuestManager", "ordered_id").SetValue(null, "");
        StaticField("RewardUI", "rewardUI_show").SetValue(null, 0);
        StaticField("OF_Spawner", "spawnable").SetValue(null, false);
        yield return null;
    }

    //敵を消して湧かないようにし、一日の巡りを止め（明るさが 1 秒ごとに段で変わる）、画面の UI と札・ゲージ・落ち葉を隠し、被写界深度を切る
    private static void Calm()
    {
        foreach (Component spawner in Each("OF_Spawner"))
        {
            ((MonoBehaviour)spawner).StopAllCoroutines();
            ((Behaviour)spawner).enabled = false;
        }
        StaticField("OF_Spawner", "spawnable").SetValue(null, false);

        foreach (GameObject enemy in GameObject.FindGameObjectsWithTag("Enemy")) UnityEngine.Object.Destroy(enemy);
        foreach (Component sun in Each("Sun2")) ((MonoBehaviour)sun).StopAllCoroutines();
        foreach (Canvas canvas in UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)) canvas.enabled = false;

        //カメラの子の落ち葉と雨（TriForge）も止める。撮る位置ではカメラのすぐ前を大きな黒い葉が横切り、主人公を隠す
        foreach (ParticleSystem near in Camera.main.GetComponentsInChildren<ParticleSystem>())
        {
            near.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        //後処理は Post Processing Stack v2（描き方は組み込み）。asmdef から参照していないので名前で引く。
        //profile は写しを返すので、素材の資産は書き換えない
        Type volumeType = Type.GetType("UnityEngine.Rendering.PostProcessing.PostProcessVolume, Unity.Postprocessing.Runtime");
        Assert.IsNotNull(volumeType, "PostProcessVolume の型が見つからない");
        int cut = 0;
        foreach (UnityEngine.Object volume in UnityEngine.Object.FindObjectsByType(volumeType, FindObjectsSortMode.None))
        {
            object profile = volumeType.GetProperty("profile").GetValue(volume);
            if (profile == null) continue;

            foreach (object effect in (IList)profile.GetType().GetField("settings").GetValue(profile))
            {
                if (effect.GetType().Name != "DepthOfField") continue;

                effect.GetType().GetField("active").SetValue(effect, false);
                object enabled = effect.GetType().GetField("enabled").GetValue(effect);
                enabled.GetType().GetMethod("Override", new[] { typeof(bool) }).Invoke(enabled, new object[] { false });
                cut++;
            }
        }
        Assert.Greater(cut, 0, "被写界深度が見つからない。ぼけたまま撮れてしまう");
    }

    //主人公とカメラを、タイトルの絵を撮ったときの置き方にする。カメラはプレイヤーの子なので、外してから置き、追いかけるのもやめる
    private static Camera Frame()
    {
        Camera eye = Camera.main;
        Assert.IsNotNull(eye, "カメラが無い");

        MainCamera follow = eye.GetComponent<MainCamera>();
        if (follow != null) follow.enabled = false;

        eye.transform.SetParent(null, true);
        eye.transform.position = CameraAt;
        eye.transform.LookAt(LookAt);
        eye.fieldOfView = FieldOfView;

        GameObject hero = GameObject.FindWithTag("Player");
        Rigidbody body = hero.GetComponent<Rigidbody>();
        if (body != null) body.isKinematic = true;

        hero.transform.position = HeroAt;
        hero.transform.rotation = Quaternion.Euler(0f, eye.transform.eulerAngles.y, 0f);

        //立ったまま、剣を立てて構える（前のタイトルの絵の構え）。プレイヤーの Update が歩きの姿勢に書き換えないよう、先に止める
        Component player = hero.GetComponent(GameType("Player2"));
        ((Behaviour)player).enabled = false;
        object character = player.GetType().GetField("character").GetValue(player);
        MethodInfo setState = character.GetType().GetMethod("SetState");
        setState.Invoke(character, new[] { Enum.Parse(setState.GetParameters()[0].ParameterType, "Idle") });

        var weapon = (Transform)player.GetType().GetField("weapon", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(player);
        weapon.gameObject.AddComponent<HoldSword>();
        return eye;
    }

    //剣を、攻撃のスティックを倒していないときの向き（手に対して 0 度。剣が立つ）に保つ。主人公のアニメは、倒れる動きが剣の向きを持つため、
    //ほかの姿勢でも毎コマ既定の向き（斜め下）を書き戻す。アニメのあとに回る LateUpdate で書き直す
    private sealed class HoldSword : MonoBehaviour
    {
        private void LateUpdate()
        {
            transform.localEulerAngles = Vector3.zero;
        }
    }

    private static void Read(RenderTexture source, Texture2D into)
    {
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = source;
        into.ReadPixels(new Rect(0, 0, source.width, source.height), 0, 0);
        into.Apply(false);
        RenderTexture.active = previous;
    }

    //終わりのあとのコマを、始まりのコマへ t の割合で寄せる
    private static void Blend(Texture2D end, Color32[] start, float t)
    {
        Color32[] pixels = end.GetPixels32();
        for (int p = 0; p < pixels.Length; p++) pixels[p] = Color32.Lerp(pixels[p], start[p], t);
        end.SetPixels32(pixels);
        end.Apply(false);
    }

#if UNITY_EDITOR
    private static MediaEncoder Encoder(string path)
    {
        var h264 = new H264EncoderAttributes
        {
            gopSize = FrameRate,
            numConsecutiveBFrames = 0
        };
        var video = new VideoTrackEncoderAttributes(h264)
        {
            frameRate = new MediaRational(FrameRate),
            width = TitleBackdrop.MovieWidth,
            height = TitleBackdrop.MovieHeight,
            targetBitRate = BitRate
        };
        return new MediaEncoder(path, video);
    }
#endif

    private static AreaTravel Travel()
    {
        AreaTravel travel = UnityEngine.Object.FindAnyObjectByType<AreaTravel>();
        Assert.IsNotNull(travel, "常駐シーンに AreaTravel が無い");
        return travel;
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
        for (int i = 0; i < 60; i++) yield return null;
    }

    private static IEnumerable<Component> Each(string typeName)
    {
        return UnityEngine.Object.FindObjectsByType(GameType(typeName), FindObjectsSortMode.None).Cast<Component>();
    }

    //Assembly-CSharp は asmdef から参照できない（参照は一方向）ため名前で引く
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
        FieldInfo field = GameType(typeName).GetField(fieldName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
        Assert.IsNotNull(field, typeName + "." + fieldName + " が見つからない");
        return field;
    }
}
