using System.Collections.Generic;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 場所の日の光。場所のシーンの、向きのある光に付ける。
    /// 強さは常駐シーンの一日の巡りが決め、読み込んである場所の光へ同じ値を配る
    /// </summary>
    [RequireComponent(typeof(Light))]
    public sealed class Daylight : MonoBehaviour
    {
        private static readonly List<Daylight> Shown = new List<Daylight>();

        /// <summary>最後に配った強さ。まだ配っていなければ null で、光は置いたときの強さのまま</summary>
        private static float? _intensity;

        private Light _sun;

        //前の再生で配った強さを持ち越さない。ドメインを読み直さない設定でも戻るように
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ForgetOnPlay()
        {
            Shown.Clear();
            _intensity = null;
        }

        /// <summary>読み込んである場所の日の光を、すべてこの強さにする。あとから読み込んだ場所にも、着いたときに同じ強さを付ける</summary>
        public static void Show(float intensity)
        {
            _intensity = intensity;
            foreach (Daylight daylight in Shown) daylight._sun.intensity = intensity;
        }

        private void Awake()
        {
            _sun = GetComponent<Light>();
        }

        private void OnEnable()
        {
            Shown.Add(this);
            if (_intensity.HasValue) _sun.intensity = _intensity.Value;
        }

        private void OnDisable()
        {
            Shown.Remove(this);
        }
    }
}
