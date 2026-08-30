using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class JoystickEffect_ATK : MonoBehaviour
{
    [SerializeField] FixedJoystick joystick;

    [SerializeField] private GameObject up;
    [SerializeField] private GameObject down;
    [SerializeField] private GameObject left;
    [SerializeField] private GameObject right;

    void Update()
    {
        if (joystick.Vertical > 0.5) up.SetActive(true); else up.SetActive(false);
        if (joystick.Vertical < -0.5) down.SetActive(true); else down.SetActive(false);
        if (joystick.Horizontal > 0.5) right.SetActive(true); else right.SetActive(false);
        if (joystick.Horizontal < -0.5) left.SetActive(true); else left.SetActive(false);
    }
}
