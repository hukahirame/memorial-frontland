using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Craft_set : MonoBehaviour
{
    public Image mainImage;
    public Text nametxt;
    public Text txt;
    public Button button;

        public void Put_Info(string s) // アイテム情報フレームへの代入
        {
            int index = -1;
        for (int i = 0; index == -1; i++)
        {
            if (GameManager.items[i][0] == s) index = i;
        }
        nametxt.text = GameManager.items[index][1];
        mainImage.sprite = Resources.Load<Sprite>(GameManager.items[index][0]);
        txt.text = GameManager.items[index][10];

        }
}
