using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapButton : MonoBehaviour
{
    public void MapClick()
    {
        GameObject.Find("RootsManager").GetComponent<RootsManager>().RootUIShow();
    }
}
