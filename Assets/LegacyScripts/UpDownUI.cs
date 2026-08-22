using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UpDownUI : MonoBehaviour
{
    [SerializeField] RectTransform rect;
    private int mode = 1; 
    private void Start()
    {
        StartCoroutine(UpDown());
    }

    IEnumerator UpDown()
    {
        while (true)
        {
            for (int i = 0; i < 18; i++)
            {
                if ((i >= 14) || (i <= 3))
                {
                    if(i % 2 == 0) rect.position += Vector3.up * mode;
                }
                else rect.position += Vector3.up * mode;

                yield return new WaitForSeconds(0.07f);
            }
            mode *= -1;
        }
    }
}
