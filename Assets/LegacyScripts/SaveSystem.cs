using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

public class SaveSystem : MonoBehaviour
{
    private string filePath;
    public SaveData savedata;

    void Awake()
    {
        filePath = Application.persistentDataPath + "/" + ".savedata.json";
        savedata = new SaveData();

        if (File.Exists(filePath)) Load();
    }

    //Domain 分割の間はセーブを止めている。Save() は残してあるので呼べば動く
    //再開するときは Space を使わないこと。Player2 のジャンプと同じキーで、
    //ジャンプのたびに保存されていた（Player2.cs の Jump も GetKeyUp(Space)）

    public void Save()
    {
        savedata.SyncStatic();

        string json = JsonUtility.ToJson(savedata);
        using (StreamWriter streamWriter = new StreamWriter(filePath))
        {
            streamWriter.Write(json);
        }
    }

    public void Load()
    {
        if (File.Exists(filePath))
        {
            using (StreamReader streamReader = new StreamReader(filePath))
            {
                string data = streamReader.ReadToEnd();
                savedata = JsonUtility.FromJson<SaveData>(data);
            }

            //既に目的のシーンにいる場合は読み直さない。読み直すと Awake が再入して無限ループになる
            if (SceneManager.GetActiveScene().name != savedata.entered_scene)
                SceneManager.LoadScene(savedata.entered_scene);
            StartCoroutine(Load2());
        }
    }

    IEnumerator Load2()
    {
        yield return new WaitUntil(() => SceneManager.GetActiveScene().isLoaded); //シーンロードまで待機

        savedata.LoadStatic();

        for (int i = 0; i < savedata.saveObjects.Count; i++) //savedata側で実行できないLoad処理の残り
        {
            var obj = Instantiate((GameObject)Resources.Load(savedata.saveObjects[i]), savedata.saveObjectsPos[i], Quaternion.identity);
            obj.name = obj.name.Replace("(Clone)", "");
        }
    }

}
