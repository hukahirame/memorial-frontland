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
    private int index = -1;

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
            var obj = Instantiate(enemy, spawnpos, Quaternion.identity);
            if (RootsManager.parameta[index][0] >= 30)
            {
                var s = obj.transform.Find("Canvas").Find("Slider").GetComponent<Slider>();
                s.value -= s.value * 0.2f;
            }

            Invoke("Spawn", Random.Range(15, 30));
        }
    }
}
