using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerHp : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt;
    [SerializeField] private Slider slider;

    [Header("自然回復")]
    [SerializeField] private float regenSeconds = 5;
    [SerializeField] private int regenAmount = 1;

    private float time = 0;

    void Update()
    {
        time += Time.deltaTime;

        if (time < regenSeconds) return;

        time = 0;

        //Slider を直に足すと、次の RefreshHpView で Health の値に上書きされて消える。
        //死んでいる間に回復しないのは Health.Heal 側の規則
        Player2.Hp.Heal(regenAmount);
        Player2.RefreshHpView();
    }

    public void ValueChange()
    {
        txt.text = slider.value.ToString() +" / " + slider.maxValue.ToString();
    }
}
