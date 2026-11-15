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

    /// <summary>拾う側の体の層</summary>
    private const int PlayerLayer = 8;

    /// <summary>この時刻（Time.time）までは触れても拾わない</summary>
    private float pickableAt;

    /// <summary>プレイヤーが一度、拾う当たりの外へ出るまで拾わない</summary>
    private bool awaitLeave;

    /// <summary>拾う当たり。プレイヤーの体がこの中にあるかを見る</summary>
    private SphereCollider reach;

    /// <summary>
    /// しばらく拾えなくし、プレイヤーが一度離れるまでも拾わない。捨てた品は拾う当たりの届く所に落ちるので、
    /// 時間だけで待たせると、立ち止まっているうちに拾い直して枠がまた埋まる
    /// </summary>
    public void HoldOff(float seconds)
    {
        pickableAt = Time.time + seconds;
        awaitLeave = true;
    }

    private void Start()
    {
        // 外部からspriterendererに適切な画像が入る
    }

    void FixedUpdate()
    {
        transform.Rotate(0, 1, 0);
        if (awaitLeave && !PlayerInReach()) awaitLeave = false;
    }

    //触れている知らせは、止まって眠った物どうしでは来ないことがあるので、当たりの中を問い合わせて見る
    private bool PlayerInReach()
    {
        if (reach == null) reach = GetComponent<SphereCollider>();

        Vector3 scale = transform.lossyScale;
        float radius = reach.radius * Mathf.Max(scale.x, scale.y, scale.z);
        return Physics.CheckSphere(transform.TransformPoint(reach.center), radius, 1 << PlayerLayer, QueryTriggerInteraction.Collide);
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
        if (picked || awaitLeave || Time.time < pickableAt || collision.gameObject.layer != PlayerLayer) return;

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
