using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DynamicLayer : MonoBehaviour
{
    //静的オブジェには貼らない
    //子の中で順序があれば、静的指定

    [SerializeField] private SpriteRenderer sr;

    [SerializeField] private bool isGroup; //SRのない親に貼る

    private float recent_z;
    private int unique_value;
    private int[] unique_values = new int[30];

    void Start()
    {
        recent_z = transform.position.z;

        if (!isGroup) unique_value = sr.sortingOrder;
        else
        {
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            for(int i = 0; i < renderers.Count(); i++)
            {
                unique_values[i] = renderers[i].sortingOrder;
            }
        }
        StartCoroutine(enumerator());
    }

    IEnumerator enumerator()
    {
        while (true)
        {
            LayerProcess();
            yield return new WaitForSeconds(0.05f); // 20F/s
        }
    }


    private void  LayerProcess() //フレーム数落としてもいい
    {
        if (recent_z != transform.position.z)
        {
            recent_z = transform.position.z;
            int order = Mathf.RoundToInt(recent_z * -100);

            if (!isGroup) sr.sortingOrder = order + unique_value;
            else
            {
                var renderers = GetComponentsInChildren<SpriteRenderer>();
                for (int i = 0; i < renderers.Count(); i++)
                {
                    renderers[i].sortingOrder = order + unique_values[i];
                }
            }
        }
    }
}
