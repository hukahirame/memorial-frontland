using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempAudio : MonoBehaviour
{
    private static AudioSource tempaudio;

    [SerializeField] private static List<AudioClip> clips = new List<AudioClip>();

    private void Start()
    {
        tempaudio = GetComponent<AudioSource>();
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Attack_Skill_Knife_Throw_B"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Attack_Weapon_Impact"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Action_Book_Page_Turn_2"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Action_Backpack_Open"));
    }


    public static void TempAudioPlay(string audioname)
    {
        AudioClip clip = null;

        foreach (var c in clips)
        {
            if (c.name == audioname)
            {
                clip = c; break;
            }
        }

        if (clip != null)
        {
            tempaudio.clip = clip;
            tempaudio.Play();
        }
    }
}
