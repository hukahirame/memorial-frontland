using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using MemorialFloor.Game;
using UnityEngine;

public class SaveSystem : MonoBehaviour
{
    /// <summary>まとめて書く間隔（実時間の秒）。体力や時刻は毎秒変わるので、変わるたびには書かない</summary>
    private const float AutosaveSeconds = 10f;

    /// <summary>読み書きするか。保存のファイルは 1 つなので、テストでは切る。切らないと前のテストの状態が次のテストに持ち越される</summary>
    public static bool Enabled = true;

    //テストは切ったまま終わる。テストの中で戻すと、再生を止めるときの書き込み（OnApplicationQuit）がテストの状態を保存してしまう。
    //そのため再生を始めるたびにここで戻す。ドメインを読み直さない設定でも戻るように。
    //枠と名前も戻す。前の再生でタイトルから選んだ枠が残ると、タイトルを通らずに始めたときに別の枠を読み書きする
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void EnableOnPlay()
    {
        Enabled = true;
        Slot = 1;
        PendingName = "";
    }

    [SerializeField] private AreaTravel travel;
    [SerializeField] private PlacedObjects placed;

    public SaveData savedata;

    /// <summary>読み込んだ保存を、最初に場所へ着いたときに戻す</summary>
    private bool restorePending;

    /// <summary>保存してあった場所。保存が無ければ null</summary>
    public string SavedAreaId
    {
        get { return restorePending ? savedata.entered_scene : null; }
    }

    /// <summary>保存の枠の数</summary>
    public const int SlotCount = 5;

    /// <summary>読み書きする枠。1 から数える。タイトルで選ぶ。タイトルを通らずに始めると 1</summary>
    public static int Slot = 1;

    /// <summary>はじめから始める枠に付ける名前。タイトルで決め、保存が無いまま始めたときに保存へ移す</summary>
    public static string PendingName = "";

    /// <summary>いまの枠の保存のファイル</summary>
    private static string FilePath
    {
        get { return SlotPath(Slot); }
    }

    /// <summary>枠の保存のファイル。1 は枠を分ける前と同じ名前にし、前の保存がそのまま 1 に入る</summary>
    private static string SlotPath(int slot)
    {
        string number = slot == 1 ? "" : slot.ToString();
        return Application.persistentDataPath + "/" + ".savedata" + number + ".json";
    }

    /// <summary>いまの枠に、続きから始められる保存があるか。読み書きを切っているあいだは無いとみなす</summary>
    public static bool HasSave
    {
        get { return HasSaveIn(Slot); }
    }

    public static bool HasSaveIn(int slot)
    {
        FinishWriting();
        return Enabled && File.Exists(SlotPath(slot));
    }

    /// <summary>枠の保存を最後に書いた時刻。保存が無ければ null</summary>
    public static DateTime? SavedAt(int slot)
    {
        return HasSaveIn(slot) ? File.GetLastWriteTime(SlotPath(slot)) : (DateTime?)null;
    }

    /// <summary>枠の保存の中身を、戻さずに読む。タイトルで名前や場所を出すのに使う。保存が無いか読めなければ null</summary>
    public static SaveData Peek(int slot)
    {
        if (!HasSaveIn(slot)) return null;

        try
        {
            return JsonUtility.FromJson<SaveData>(File.ReadAllText(SlotPath(slot)));
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// 枠の保存の名前を書き換える。ほかは変えない。書いた時刻も元に戻す。時刻が変わると、タイトルの「つづきから」が
    /// 名前を変えただけの枠を選び、枠に出す日時も遊んだ時刻とずれる。保存が無いか読めなければ何もしない
    /// </summary>
    public static void Rename(int slot, string name)
    {
        SaveData data = Peek(slot);
        if (data == null) return;

        string path = SlotPath(slot);
        DateTime written = File.GetLastWriteTime(path);

        data.saveName = name;
        string writing = path + ".tmp";
        File.WriteAllText(writing, JsonUtility.ToJson(data));
        File.Copy(writing, path, true);
        File.Delete(writing);

        File.SetLastWriteTime(path, written);
    }

    /// <summary>いまの枠の保存を消す。次に始めると、はじめからになる。取り返せないので、呼ぶ側で確かめてから呼ぶ</summary>
    public static void Erase()
    {
        Erase(Slot);
    }

    public static void Erase(int slot)
    {
        FinishWriting();
        string path = SlotPath(slot);
        if (File.Exists(path)) File.Delete(path);
        if (File.Exists(path + ".tmp")) File.Delete(path + ".tmp");
    }

    void Awake()
    {
        savedata = new SaveData();

        if (HasSave) Load();

        //はじめから始めた枠には、タイトルで決めた名前を付ける。続きからなら読んだ保存の名前のまま
        if (!restorePending) savedata.saveName = PendingName;
    }

    void Start()
    {
        if (Enabled) StartCoroutine(Autosave());
    }

    private void OnDestroy()
    {
        travel.Entered -= Restore;
    }

    //裏に回したアプリは、そのまま落とされることがある。書き終えるまで待ってから手放す
    private void OnApplicationPause(bool paused)
    {
        if (!paused) return;

        TrySave();
        FinishWriting();
    }

    private void OnApplicationQuit()
    {
        TrySave();
        FinishWriting();
    }

    private IEnumerator Autosave()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(AutosaveSeconds);
            TrySave();
        }
    }

    /// <summary>
    /// 書いてよいときだけ書く。戻し終える前に書くと、保存を始めの状態で上書きする。
    /// 移っている最中と倒れているあいだは、場所と体力が定まらない。設定からタイトルに戻るときも呼ぶ
    /// </summary>
    public void TrySave()
    {
        if (!Enabled || restorePending) return;
        if (travel.Current == null || travel.IsTraveling) return;
        if (!Player2.Hp.IsReady || Player2.Hp.IsDead) return;

        Save();
    }

    public void Save()
    {
        savedata.SyncStatic(travel.Where, placed);
        Write(FilePath, JsonUtility.ToJson(savedata));
    }

    /// <summary>
    /// 裏で書いている途中の保存。ファイルに書くのは数ミリ秒かかり、遊んでいるあいだに挟むとそのコマが遅れて引っかかる。
    /// 書くのは前の書き込みが終わってからなので、古い中身があとから新しい中身を上書きすることはない
    /// </summary>
    private static Task pendingWrites = Task.CompletedTask;

    private static readonly object WritingGate = new object();

    //中身はここで固めて渡す。WebGL は裏で動かせないので、その場で書く
    private static void Write(string path, string json)
    {
        if (Application.platform == RuntimePlatform.WebGLPlayer)
        {
            WriteNow(path, json);
            return;
        }

        lock (WritingGate)
        {
            pendingWrites = pendingWrites.ContinueWith(_ => WriteNow(path, json), TaskScheduler.Default);
        }
    }

    //書いている途中で落ちても前の保存を壊さないよう、別のファイルに書いてから差し替える
    private static void WriteNow(string path, string json)
    {
        try
        {
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, json);
            File.Copy(tmp, path, true);
            File.Delete(tmp);
        }
        catch (Exception e)
        {
            Debug.LogWarning("保存を書けなかった：" + e.Message);
        }
    }

    /// <summary>裏で書いている保存を書き終えるまで待つ。保存のファイルを読む・消す前と、手放す直前に呼ぶ</summary>
    private static void FinishWriting()
    {
        Task last;
        lock (WritingGate) last = pendingWrites;
        last.Wait();
    }

    /// <summary>保存してあった根源とクエストを戻す。最初の場所に着く前に 1 度だけ呼ぶ。戻さなければ false</summary>
    public bool RestoreWorld()
    {
        return restorePending && savedata.LoadWorld();
    }

    /// <summary>起動時に1度だけ。場所の移動は始める側が SavedAreaId を見て行う。読めない保存は捨てる</summary>
    private void Load()
    {
        SaveData loaded = null;
        try
        {
            loaded = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
        }
        catch (Exception e)
        {
            Debug.LogWarning("保存を読めなかったので、はじめから始める：" + e.Message);
        }

        if (loaded == null) return;

        savedata = loaded;
        savedata.LoadPlacements(placed.Ledger);

        restorePending = true;
        travel.Entered += Restore;
    }

    private void Restore(Area area)
    {
        travel.Entered -= Restore;
        restorePending = false;

        savedata.LoadStatic(area.Id);
    }

}
