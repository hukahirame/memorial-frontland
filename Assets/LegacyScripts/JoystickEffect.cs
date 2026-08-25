using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickEffect : MonoBehaviour
{
    [SerializeField] private GameObject up;
    [SerializeField] private GameObject down;
    [SerializeField] private GameObject left;
    [SerializeField] private GameObject right;

    void Update()
    {
        if (Player2.verticalKey > 0.5) up.SetActive(true); else up.SetActive(false);
        if (Player2.verticalKey < -0.5) down.SetActive(true); else down.SetActive(false);
        if (Player2.horizontalKey > 0.5) right.SetActive(true); else right.SetActive(false);
        if (Player2.horizontalKey < -0.5) left.SetActive(true); else left.SetActive(false);
    }
}
