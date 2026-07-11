using System.Collections;
using UnityEngine;

public class Wink : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Sprite open;
    [SerializeField] private Sprite close;

    [SerializeField] private SpriteRenderer deatheyes;

    private float time;
    private bool is_close = false;
    public static bool play_eyeclose = false;

    void Update()
    {
        time += Time.deltaTime;

        if(play_eyeclose) 
        {
            play_eyeclose = false;
            sr.sprite = close;
            is_close = true;
            time = -0.3f;
        }

        if ((time > 4f) && (!is_close))
        {
            sr.sprite = close;
            is_close = true;
            time = 0;
        }
        else if((time > 0.2f) && (is_close))
        {
            sr.sprite = open;
            is_close = false;
            time = 0;
        }
    }

    public void DeathEyesClose(float timeset)
    {
        sr.sprite = open;
        time = timeset;
    }
}
