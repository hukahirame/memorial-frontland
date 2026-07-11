using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SideJab : MonoBehaviour
{

    public IEnumerator SideJabPlay(int direction)
    {
        for (int i = 0; i < 15; i++)
        {
            transform.parent.Rotate(0, -direction * transform.localScale.x, 0);
            yield return new WaitForSeconds(0.03f);
        }
    }

}
