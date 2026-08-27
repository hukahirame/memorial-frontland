using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MS_Spawner : MonoBehaviour
{
    public float spawnrange; // 最大範囲
    public GameObject seeker;
    public GameObject enemy;

    private Vector3 spawnpos;

    public static bool spawnable = false;
    public Vector4 expos; //XL,XS,ZL,ZS

    public void Spawn()
    {
        do
        {
            spawnpos = new Vector3(Random.Range(-spawnrange - 2, spawnrange + 2), 1, Random.Range(-spawnrange, spawnrange)) + transform.position;
        }
        while ((spawnpos.x < expos.x) && (spawnpos.x > expos.y) && (spawnpos.z < expos.z) && (spawnpos.z > expos.w));
        seeker.transform.position = spawnpos;
        seeker.SetActive(true);
        Invoke("Spawn2", 0.2f);
    }

    private void Spawn2()
    {
        if (spawnable == false)
        {
            spawnable = true;
            Spawn();
        }
        else
        {
            seeker.SetActive(false);
            //MainSite に対応する根源は無いため、攻略度による弱体化は行わない。
            //旧コードは index が -1 のまま parameta[-1] を読んでおり、到達すれば必ず例外だった
            Instantiate(enemy, spawnpos, Quaternion.identity);

            Invoke("Spawn", Random.Range(15, 30));
        }
    }
}
