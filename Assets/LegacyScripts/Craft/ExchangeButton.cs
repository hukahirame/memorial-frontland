using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExchangeButton : MonoBehaviour
{
    [SerializeField] private RectTransform pi;
    [SerializeField] private RectTransform rm;
    [SerializeField] private RectTransform center;

    [SerializeField] private bool is_pi = true;

    /// <summary>持ち物と地図が入れ替わりきるまでの秒数</summary>
    public const float SlideSeconds = 0.3f;

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
            CloseRootDetail();
            StartCoroutine (Anim_Exchange2());
            TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
        }
    }

    //根源の詳細は地図の上に開く。開いたまま持ち物へ移ると、詳細だけが持ち物の上に残るので、地図と一緒に閉じる
    private static void CloseRootDetail()
    {
        GameObject detail = GameObject.FindWithTag("QuestManager");
        if (detail != null) detail.GetComponentInChildren<CloseManager>(true).CloseClick();
    }

    IEnumerator Anim_Exchange1()
    {
        moving = this;
        yield return Slide(rm, pi, Vector3.right);
        pi.position = new Vector3(0, 5000, 0);
        moving = null;
    }
    IEnumerator Anim_Exchange2()
    {
        moving = this;
        yield return Slide(pi, rm, Vector3.left);
        rm.position = new Vector3(-1000, 5000, 0);
        moving = null;
    }

    //入ってくる方を画面 1 つ分外から真ん中へ、出ていく方を真ん中から画面の外へ送る。
    //描画の速さに関わらず同じ秒数で終え、終わり際をゆっくりにする。時間止めの間も進める
    private IEnumerator Slide(RectTransform coming, RectTransform going, Vector3 direction)
    {
        Vector3 distance = direction * Screen.width;
        Vector3 comingFrom = center.position - distance;
        Vector3 goingFrom = going.position;

        for (float elapsed = 0f; elapsed < SlideSeconds; elapsed += Time.unscaledDeltaTime)
        {
            float eased = 1f - Mathf.Pow(1f - elapsed / SlideSeconds, 3f);
            coming.position = comingFrom + distance * eased;
            going.position = goingFrom + distance * eased;
            yield return null;
        }

        coming.position = center.position;
    }
}
