using MemorialFloor.Game;
using UnityEngine;
using System.IO;

public class SaveSystem : MonoBehaviour
{
    [SerializeField] private AreaTravel travel;
    [SerializeField] private PlacedObjects placed;

    private string filePath;
    public SaveData savedata;

    /// <summary>読み込んだ保存を、最初に場所へ着いたときに戻す</summary>
    private bool restorePending;

    /// <summary>保存してあった場所。保存が無ければ null</summary>
    public string SavedAreaId
    {
        get { return restorePending ? savedata.entered_scene : null; }
    }

    void Awake()
    {
        filePath = Application.persistentDataPath + "/" + ".savedata.json";
        savedata = new SaveData();

        if (File.Exists(filePath)) Load();
    }

    private void OnDestroy()
    {
        travel.Entered -= Restore;
    }

    //Domain 分割の間はセーブを止めている。Save() は残してあるので呼べば動く
    //再開するときは Space を使わないこと。Player2 のジャンプと同じキーで、
    //ジャンプのたびに保存されていた（Player2.cs の Jump も GetKeyUp(Space)）

    public void Save()
    {
        savedata.SyncStatic(travel.Where, placed);

        string json = JsonUtility.ToJson(savedata);
        using (StreamWriter streamWriter = new StreamWriter(filePath))
        {
            streamWriter.Write(json);
        }
    }

    /// <summary>起動時に1度だけ。場所の移動は始める側が SavedAreaId を見て行う</summary>
    private void Load()
    {
        using (StreamReader streamReader = new StreamReader(filePath))
        {
            savedata = JsonUtility.FromJson<SaveData>(streamReader.ReadToEnd());
        }

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
