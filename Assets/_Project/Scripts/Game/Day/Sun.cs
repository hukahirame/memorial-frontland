using System;
using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 一日の巡り。常駐シーンに 1 つだけ置く。場所をいくつ読み込んでいても、時計はここで 1 つだけ進める。
    /// 日の光の強さを決めて知らせ（場所の日の光は Daylight が受ける）、零時と夜明けを知らせる
    /// </summary>
    public sealed class Sun : MonoBehaviour
    {
        private const float MaxIntensity = 1.8f;
        private const float MinIntensity = 0.3f;

        [Tooltip("1 日のゲーム内時間（秒）")]
        [SerializeField, FormerlySerializedAs("aroundtime")] private float _dayLength = 120;

        public DayCycle Cycle { get; } = new DayCycle();

        /// <summary>いまの日の光の強さ。時刻が 1 つ進むたびに変わる</summary>
        public float LightIntensity { get; private set; }

        /// <summary>零時を過ぎた</summary>
        public event Action Midnight;

        /// <summary>夜明けの時刻を跨いだ。その刻みに 1 回だけ</summary>
        public event Action Morning;

        /// <summary>日の光の強さが変わった</summary>
        public event Action<float> LightChanged;

        //場所の日の光は、読み込んだときにすぐ今の強さを受け取る。巡りが回り始める前でも 0 を渡さない
        private void Awake()
        {
            LightIntensity = Intensity();
        }

        private void Start()
        {
            StartCoroutine(Go());
        }

        private IEnumerator Go()
        {
            while (true)
            {
                if (Cycle.IsDayOver(_dayLength))
                {
                    Cycle.Reset();
                    Midnight?.Invoke();
                }

                LightIntensity = Intensity();
                LightChanged?.Invoke(LightIntensity);

                yield return new WaitForSeconds(1f);
                Cycle.Advance();

                if (Cycle.Entered(DayClock.MorningHour, _dayLength)) Morning?.Invoke();
            }
        }

        private float Intensity()
        {
            return DayClock.LightIntensity(Cycle.HourAt(_dayLength), MinIntensity, MaxIntensity);
        }
    }
}
