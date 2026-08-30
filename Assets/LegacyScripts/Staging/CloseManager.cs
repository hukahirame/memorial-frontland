using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CloseManager : MonoBehaviour
{
    public RectTransform rt;
    public RectTransform center;
    [SerializeField] private int deleteLength;

    public void CloseClick()
    {
        rt.position = new Vector3(0, 5000, 0);
        for(int i = 0; i < transform.parent.childCount; ++i)
        {
            if (transform.parent.GetChild(i).name.Length <= deleteLength) Destroy(transform.parent.GetChild(i).gameObject);
        }
    }

    public void OpenClick()
    {
        rt.position = center.position;
        
    }
}