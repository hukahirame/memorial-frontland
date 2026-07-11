using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseInventory : MonoBehaviour
{
    public RectTransform rt;
    public RectTransform center;
    
    public void CloseClick()
    {
        rt.position = new Vector3(0, 5000, 0);
    }

    public void OpenClick()
    {
        rt.position = center.position;
        TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
    }
}
