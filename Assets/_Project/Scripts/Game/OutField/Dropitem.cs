using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Game;
using UnityEngine;

public class Dropitem : MonoBehaviour
{
    private IItemReceiver pi;
    private int durability = 0;

    /// <summary>拾われた。消えるのはフレームの終わりなので、それまでの物理の段で触れ直しても二重に拾わない</summary>
    private bool picked;

    private void Start()
    {
        // 外部からspriterendererに適切な画像が入る
    }

    void FixedUpdate()
    {
        transform.Rotate(0, 1, 0);
    }

    private void OnTriggerEnter(Collider collision)
    {
        PickUp(collision);
    }

    //持ち物が一杯で拾えなかったものも、空いたあとに触れていれば拾う。入り直すまで待たせない
    private void OnTriggerStay(Collider collision)
    {
        PickUp(collision);
    }

    private void PickUp(Collider collision)
    {
        if (picked || collision.gameObject.layer != 8) return;

        if (pi == null) pi = GameObject.FindWithTag("PlayerInventory").GetComponent<IItemReceiver>();
        if (pi.LoadInventory(GetComponent<SpriteRenderer>().sprite.name, durability) != 1) return;

        picked = true;
        Destroy(gameObject);
    }

    void OnDestroy()
    {
        Debug.Log(GetComponent<SpriteRenderer>().sprite.name + "を入手しました");
    }
}
