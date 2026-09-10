using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using MemorialFloor.Domain;

public class Craft_set : MonoBehaviour
{
    public Image mainImage;
    public Text nametxt;
    public Text txt;
    public Button button;

        public void Put_Info(string s) // アイテム情報フレームへの代入
        {
        ItemDefinition item = GameManager.Items.Find(s);
        if (item == null) return;

        nametxt.text = item.Name;
        mainImage.sprite = Resources.Load<Sprite>(item.ItemId);
        txt.text = item.Description;

        }
}
