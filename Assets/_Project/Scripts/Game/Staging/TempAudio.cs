using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TempAudio : MonoBehaviour
{
    private static AudioSource tempaudio;

    /// <summary>音程を変えて鳴らす側。音程は鳴っている音すべてに掛かるため分ける</summary>
    private static AudioSource pitched;

    [SerializeField] private static List<AudioClip> clips = new List<AudioClip>();

    private void Start()
    {
        tempaudio = GetComponent<AudioSource>();

        pitched = gameObject.AddComponent<AudioSource>();
        pitched.playOnAwake = false;
        pitched.outputAudioMixerGroup = tempaudio.outputAudioMixerGroup;
        pitched.volume = tempaudio.volume;
        pitched.spatialBlend = tempaudio.spatialBlend;

        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Attack_Skill_Knife_Throw_B"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Attack_Weapon_Impact"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Action_Book_Page_Turn_2"));
        clips.Add((AudioClip)Resources.Load("Fantasy_Game_Action_Backpack_Open"));
    }

    /// <summary>重ねて鳴らす。先に鳴っている音は切らない</summary>
    public static void TempAudioPlay(string audioname)
    {
        AudioClip clip = Find(audioname);
        if (clip != null) tempaudio.PlayOneShot(clip);
    }

    /// <summary>音程を変えて重ねて鳴らす。1 より小さいと低く重くなる</summary>
    public static void TempAudioPlay(string audioname, float pitch)
    {
        AudioClip clip = Find(audioname);
        if (clip == null) return;

        pitched.pitch = pitch;
        pitched.PlayOneShot(clip);
    }

    private static AudioClip Find(string audioname)
    {
        foreach (var c in clips)
        {
            if (c.name == audioname) return c;
        }

        return null;
    }
}
