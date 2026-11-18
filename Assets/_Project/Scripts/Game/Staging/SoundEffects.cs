using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 効果音を名前で重ねて鳴らす。先に鳴っている音は切らない。
    /// 音程を変えて鳴らすときは別の AudioSource を使う（音程は、その AudioSource で鳴っている音すべてに掛かるため）
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public sealed class SoundEffects : MonoBehaviour
    {
        /// <summary>鳴らせる音。Resources から名前で読む</summary>
        private static readonly string[] ClipNames =
        {
            "Fantasy_Game_Attack_Skill_Knife_Throw_B",
            "Fantasy_Game_Attack_Weapon_Impact",
            "Fantasy_Game_Action_Book_Page_Turn_2",
            "Fantasy_Game_Action_Backpack_Open",
        };

        private AudioClip[] _clips;
        private AudioSource _plain;
        private AudioSource _pitched;

        private void Awake()
        {
            _plain = GetComponent<AudioSource>();

            _pitched = gameObject.AddComponent<AudioSource>();
            _pitched.playOnAwake = false;
            _pitched.outputAudioMixerGroup = _plain.outputAudioMixerGroup;
            _pitched.volume = _plain.volume;
            _pitched.spatialBlend = _plain.spatialBlend;

            _clips = new AudioClip[ClipNames.Length];
            for (int i = 0; i < ClipNames.Length; i++) _clips[i] = Resources.Load<AudioClip>(ClipNames[i]);
        }

        /// <summary>重ねて鳴らす。無い名前なら鳴らさない</summary>
        public void Play(string clipName)
        {
            AudioClip clip = Find(clipName);
            if (clip != null) _plain.PlayOneShot(clip);
        }

        /// <summary>音程を変えて重ねて鳴らす。1 より小さいと低く重くなる</summary>
        public void Play(string clipName, float pitch)
        {
            AudioClip clip = Find(clipName);
            if (clip == null) return;

            _pitched.pitch = pitch;
            _pitched.PlayOneShot(clip);
        }

        private AudioClip Find(string clipName)
        {
            foreach (AudioClip clip in _clips)
            {
                if (clip != null && clip.name == clipName) return clip;
            }

            return null;
        }
    }
}
