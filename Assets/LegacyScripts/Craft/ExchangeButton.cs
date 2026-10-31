using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExchangeButton : MonoBehaviour
{
    [SerializeField] private RectTransform pi;
    [SerializeField] private RectTransform rm;
    [SerializeField] private RectTransform center;

    [SerializeField] private bool is_pi = true;

    /// <summary>入れ替えている最中のボタン。2 つのボタンで持ち合う。重ねて動かすと、持ち物も地図も画面の外へ送られる</summary>
    private static ExchangeButton moving;

    public void Exchange_UI()
    {
        if (moving != null && moving.isActiveAndEnabled) return;

        if (is_pi)
        {
            StartCoroutine(Anim_Exchange1());
            rm.GetComponent<RootsManager>().RootUIShow();
            TempAudio.TempAudioPlay("Fantasy_Game_Action_Book_Page_Turn_2");
        }
        else
        {
            StartCoroutine (Anim_Exchange2());
            TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
        }
    }

    IEnumerator Anim_Exchange1()
    {
        moving = this;
        rm.position = center.position + Vector3.left * 1200;

        for (int i = 0; i < 60; i++)
        {
            rm.position += Vector3.right * 20;
            pi.position += Vector3.right * 20;

            yield return 1 / 120f;
        }
        pi.position = new Vector3(0, 5000, 0);
        moving = null;
    }
    IEnumerator Anim_Exchange2()
    {
        moving = this;
        pi.position = center.position + Vector3.right * 1200;

        for (int i = 0; i < 60; i++)
        {
            pi.position += Vector3.left * 20;
            rm.position += Vector3.left * 20;

            yield return 1 / 120f;
        }
        rm.position = new Vector3(-1000, 5000, 0);
        moving = null;
    }
}
