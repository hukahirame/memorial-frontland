using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Seeker : MonoBehaviour
{
    private void Start()
    {
        transform.position += Vector3.up;
    }

    private void SetPosition()
    {
        RaycastHit hit;
        transform.position += Vector3.down * 0.05f;

        // 除外したいレイヤーを設定 (例: "IgnoreRaycast") int layerMask = 1 << LayerMask.NameToLayer("IgnoreRaycast"); 
        // 除外するためには、レイヤーマスクを反転する layerMask = ~layerMask;
        Physics.Raycast(transform.position, Vector3.down, out hit, 10f/*, layerMask*/);

        if (hit.point != null)  transform.parent.GetComponent<OF_Spawner>().Spawn2(hit.point + Vector3.up * 0.6f);

        else  Debug.Log("スポーン接地失敗");
    }

    public void SeekPosition(float range, Vector4 expos)
    {
        Vector3 org = transform.position;

        float pos_x = Random.Range(Mathf.Max(org.x - range, expos.y), Mathf.Min(org.x + range, expos.x));
        float pos_z = Random.Range(Mathf.Max(org.z - range, expos.w), Mathf.Min(org.z + range, expos.z));

        transform.position = new Vector3(pos_x, 5, pos_z);
        SetPosition();
    }

    /* private void Start()
     {
         transform.position += Vector3.up;
         StartCoroutine(SetPosition(0));
     }

     IEnumerator SetPosition(int mode)
     {
         for (int i = 0; i < 200; i++)
         {
             transform.position += Vector3.down * 0.05f;
             if (Physics.Raycast(transform.position, Vector3.down, 0.1f))
             {
                 if (mode == 1)
                 {
                     transform.parent.GetComponent<OF_Spawner>().Spawn2(transform.position + Vector3.up * 0.6f);
                     if (seek != null)
                     {
                         StopCoroutine(seek);
                         seek = null;
                         yield break; // コルーチンを終了
                     }
                     else { Debug.Log("呼び出し元-seek-が見つかりません"); }
                 }
             }
             yield return null; // 一応
         }
         Debug.Log("スポーン接地失敗");
         transform.parent.GetComponent<OF_Spawner>().Spawn(); // 例外処理

         if (mode == 0)
         {
             StartCoroutine(SetPosition(0));
         }
     }

     public IEnumerator SeekPosition(float range, Vector4 expos)
     {
         Vector3 org = transform.position;
         int maxAttempts = 100; // 最大試行回数を設定
         int attempts = 0;

         do
         {
             pos_x = Random.Range(org.x - range, org.x + range);
             pos_z = Random.Range(org.z - range, org.z + range);
             attempts++;

             if (attempts >= maxAttempts)
             {
                 Debug.Log("ex範囲内スポーン失敗");
                 transform.parent.GetComponent<OF_Spawner>().Spawn();
                 break;
             }
         } while ((pos_x >= expos.x) || (pos_x <= expos.y) || (pos_z >= expos.z) || (pos_z <= expos.w)); // 修正

         transform.position = new Vector3(pos_x, 3, pos_z);
         StartCoroutine(SetPosition(1));
         yield return null;
     }
    */
}

