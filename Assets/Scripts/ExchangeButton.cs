using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExchangeButton : MonoBehaviour
{
    [SerializeField] private RectTransform pi;
    [SerializeField] private RectTransform rm;
    [SerializeField] private RectTransform center;

    [SerializeField] private bool is_pi = true;

    public void Exchange_UI()
    {
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
        rm.position = center.position + Vector3.left * 1200;

        for (int i = 0; i < 60; i++)
        {
            rm.position += Vector3.right * 20;
            pi.position += Vector3.right * 20;

            yield return 1 / 120f;
        }
        pi.position = new Vector3(0, 5000, 0);
    }
    IEnumerator Anim_Exchange2()
    {
        pi.position = center.position + Vector3.right * 1200;

        for (int i = 0; i < 60; i++)
        {
            pi.position += Vector3.left * 20;
            rm.position += Vector3.left * 20;

            yield return 1 / 120f;
        }
        rm.position = new Vector3(-1000, 5000, 0);
    }
}
