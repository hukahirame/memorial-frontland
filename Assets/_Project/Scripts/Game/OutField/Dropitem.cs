using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Game;
using UnityEngine;

public class Dropitem : MonoBehaviour
{
    private IItemReceiver pi;
    private int durability = 0;

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
        if (collision.gameObject.layer == 8)
        {
            if (pi == null) pi = GameObject.FindWithTag("PlayerInventory").GetComponent<IItemReceiver>();

            if (pi.LoadInventory(GetComponent<SpriteRenderer>().sprite.name, durability) == 1) Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        Debug.Log(GetComponent<SpriteRenderer>().sprite.name + "を入手しました");
    }
}
