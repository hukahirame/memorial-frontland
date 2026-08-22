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

    private void Update()
    {
       if (Input.GetKeyUp(KeyCode.Space)) Save();
    }

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
