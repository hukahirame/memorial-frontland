using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Title : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI txt;

    private void Start()
    {
        StartCoroutine(colorset());
    }

    IEnumerator colorset()
    {
        while (true)
        {
           /// txt.color = (255,0,0);
            yield return new WaitForSeconds(0.1f);
        }
    }

    public void Load()
    {
        SceneManager.LoadScene(AreaTravel.CoreScene);
    }
}
