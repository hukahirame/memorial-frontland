using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneFinisher : MonoBehaviour
{
    [SerializeField] string type; //役割

    public void SceneFinish()
    {
        var saveobj = GameObject.FindGameObjectsWithTag("SaveObject");
        SaveData sd = GameObject.Find("GameManager").GetComponent<SaveSystem>().savedata;
        for (int i = 0; i < saveobj.Length; i++)
        {
            sd.saveObjects[i] = saveobj[i].name;
            sd.saveObjectsPos[i] = saveobj[i].transform.position;

            sd.saveObjectsScene[i] = GameManager.entered_scene;
        }
    }

}
