using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SortingDebuger : MonoBehaviour
{
    void Start()
    {
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        foreach(var sprite in sprites)
        {
            if(sprite.sortingOrder != 0)
            {
                Debug.Log("Åyî≠å©Åz" + sprite.gameObject.name + "ÅF" + sprite.sortingOrder);
                sprite.sortingOrder = 0;
            }
        }
    }

}
