using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WeaponBox : MonoBehaviour // Boxが重ねられる。実体はない。
{
    void Start()
    {
        
    }


    public void Weapon_App(string s)
    {
        Transform t = GameObject.FindWithTag("Player").transform;
        GameObject obj = (GameObject)Instantiate(Resources.Load(s), t.position, Quaternion.identity,t);

    }

    public void Weapon_Des()
    {
        //自身のボタンをクリアする
    }
}
