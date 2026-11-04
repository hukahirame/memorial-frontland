using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BigText : MonoBehaviour
{
    public float waittime;
    public int flame;
    [SerializeField] private Image image1;
    [SerializeField] private Image image2;
    [SerializeField] private TextMeshProUGUI txt1; //中央
    [SerializeField] private TextMeshProUGUI txt2; //リボン側

    private Coroutine shown;

    //テキスト・画像は最初から透過指定

    public void Bigtxt_Anim(string s1,string s2)
    {
        //開いている持ち物や地図より手前に出す。Canvas の子は後に並ぶものほど手前に描かれる
        transform.SetAsLastSibling();
        transform.localScale = new Vector3(1, 1, 1);
        txt1.text = s1;
        txt2.text = s2;

        //続けて出すと、前のものの終わりで後のものまで消える。前のものは止めて、透明から出し直す
        if (shown != null) StopCoroutine(shown);
        foreach (Graphic part in new Graphic[] { image1, image2, txt1, txt2 })
        {
            Color color = part.color;
            color.a = 0f;
            part.color = color;
        }
        shown = StartCoroutine(Bigtxt_Anim2());
    }

    IEnumerator Bigtxt_Anim2()
    {
        for (int i = 0; i < flame; i++)
        {
            image1.color += new Color(0, 0, 0, 1f / flame);
            image2.color += new Color(0, 0, 0, 1f / flame);
            txt1.color += new Color(0, 0, 0, 1f / flame);
            txt2.color += new Color(0, 0, 0, 1f / flame);
            yield return new WaitForSeconds(1f / flame);
        }

        yield return new WaitForSeconds(waittime);

        for (int i = 0; i < flame; i++)
        {
            image1.color -= new Color(0, 0, 0, 1f / flame);
            image2.color -= new Color(0, 0, 0, 1f / flame);
            txt1.color -= new Color(0, 0, 0, 1f / flame);
            txt2.color -= new Color(0, 0, 0, 1f / flame);
            yield return new WaitForSeconds(1f / flame);
        }
        transform.localScale = new Vector3(0, 1, 1);
    }
}