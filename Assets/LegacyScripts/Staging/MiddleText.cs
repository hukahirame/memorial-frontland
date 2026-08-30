using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiddleText : MonoBehaviour
{
    public float waittime;
    public int flame;
    private Coroutine c;
    private Vector3 origin;

    [SerializeField] private Image image;
    [SerializeField] private TextMeshProUGUI txt;

    void Start()
    {
        origin = transform.position;
        transform.localScale = new Vector3(0, 1, 1);
        Midtxt_Anim("管理地ーはじまりの地ー");
    }

    public void Midtxt_Anim(string s)
    {
        transform.localScale = Vector3.one;
        txt.text = s;

        transform.position = origin;
        if (c != null) StopCoroutine(c);
        c = StartCoroutine(Midtxt_Anim2());
    }

    IEnumerator Midtxt_Anim2()
    {
        transform.position -= new Vector3(60, 0, 0);
        image.color = new Color(255, 255, 255, 0);
        txt.color = new Color(255, 255, 255, 0);

        for(int i = 0; i < flame; i++)
        {
            transform.position += Vector3.right;
            image.color += new Color(0, 0, 0, 1f / flame);
            txt.color += new Color(0, 0, 0, 1f / flame);
            yield return new WaitForSeconds(1f / flame);
        }

        yield return new WaitForSeconds(waittime);

        for (int i = 0; i < flame; i++)
        {
            transform.position += Vector3.left;
            image.color -= new Color(0, 0, 0, 1f / flame);
            txt.color -= new Color(0, 0, 0, 1f / flame);
            yield return new WaitForSeconds(1 / flame);
        }

        transform.localScale = new Vector3(0, 1, 1);
    }
}
