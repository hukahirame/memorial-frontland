using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainCamera : MonoBehaviour
{
    private Transform playerpos;
    void Start()
    {
        playerpos = GameObject.FindWithTag("Player").transform;
    }

    void FixedUpdate()
    {
        if (playerpos != null)
        {
            Vector3 pos = transform.position;
            if (playerpos.position.x - pos.x > 2)
            {
                transform.Translate(0.04f, 0f, 0f);
            }
            else if (playerpos.position.x - pos.x < -2)
            {
                transform.Translate(-0.04f, 0f, 0f);
            }
            /*else if (playerpos.position.x - pos.x > 0.8)
            {
                transform.Translate(0.03f, 0f, 0f);
            }
            else if (playerpos.position.x - pos.x < -0.8)
            {
                transform.Translate(-0.03f, 0f, 0f);
            }
            if (playerpos.y - pos.y < -0.15)
            {
                transform.Translate(0f, -0.04f, 0f);
            }
            else if (playerpos.y - pos.y > 0.15)
            {
                transform.Translate(0f, 0.04f, 0f);
            }*/
        }
    }
}
