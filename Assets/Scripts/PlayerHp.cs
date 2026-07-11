using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt;
    [SerializeField] private Slider slider;

    private float time = 0;

    void Update()
    {
        time += Time.deltaTime;

        if(time > 5)
        {
            time = 0;
            slider.value += 1;
        }
    }

    public void ValueChange()
    {
        txt.text = slider.value.ToString() +" / " + slider.maxValue.ToString();
    }
}
