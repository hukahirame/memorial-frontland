using UnityEngine;
using UnityEngine.UI;

public class CraftButton : MonoBehaviour
{
    public Craft craft;

    public void CraftPrepare1()
    {
        craft.Craftprepare2(gameObject.name);
    }
}
