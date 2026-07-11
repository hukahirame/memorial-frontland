using UnityEngine;
using UnityEngine.UI;

public class DamageSet : MonoBehaviour
{
    [SerializeField] private Text txt;

    private float timer = 0;
    private int d = -1;
    private Vector3 origin;

    void Start()
    {
        //位置調整
    }

    void Update()
    {
        if (d == -1) return; //Text()が行われるまで待機

        timer += Time.deltaTime;
        transform.position = origin + new Vector3(0, timer / 10, 0);

        if (timer > d / 100 + 1) Destroy(gameObject);
    }

    public void Text(int damage)
    {
        origin = transform.position;
        d = damage;
        txt.text = d.ToString();
        //色変更
        //エフェクト
    }
}
