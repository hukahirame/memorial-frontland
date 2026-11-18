using System;
using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 一日の巡り。常駐シーンに 1 つだけ置く。場所をいくつ読み込んでいても、時計はここで 1 つだけ進める。
    /// 場所の日の光の強さは Daylight へ配り、零時と夜明けを知らせる
    /// </summary>
    public sealed class Sun : MonoBehaviour
    {
        private const float MaxIntensity = 1.8f;
        private const float MinIntensity = 0.3f;

        [Tooltip("1 日のゲーム内時間（秒）")]
        [SerializeField, FormerlySerializedAs("aroundtime")] private float _dayLength = 120;

        public DayCycle Cycle { get; } = new DayCycle();

        /// <summary>零時を過ぎた</summary>
        public event Action Midnight;

        /// <summary>夜明けの時刻を跨いだ。その刻みに 1 回だけ</summary>
        public event Action Morning;

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

                Daylight.Show(DayClock.LightIntensity(Cycle.HourAt(_dayLength), MinIntensity, MaxIntensity));

                yield return new WaitForSeconds(1f);
                Cycle.Advance();

                if (Cycle.Entered(DayClock.MorningHour, _dayLength)) Morning?.Invoke();
            }
        }
    }
}
