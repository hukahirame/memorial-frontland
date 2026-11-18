using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>画面の部品を、1 画素ずつ上下にゆっくり揺らし続ける。往きと帰りの端では半分の速さにする</summary>
    public sealed class UpDownUI : MonoBehaviour
    {
        /// <summary>片道のコマ数</summary>
        private const int StepsPerWay = 18;

        /// <summary>1 コマの秒数</summary>
        private const float StepSeconds = 0.07f;

        [SerializeField, FormerlySerializedAs("rect")] private RectTransform _rect;

        /// <summary>1 コマに動く向きと量。上が 1、下が -1</summary>
        private int _direction = 1;

        private void Start()
        {
            StartCoroutine(Sway());
        }

        private IEnumerator Sway()
        {
            while (true)
            {
                for (int i = 0; i < StepsPerWay; i++)
                {
                    //端の 4 コマは 1 コマおきに動かす
                    bool isEnd = i >= StepsPerWay - 4 || i <= 3;
                    if (!isEnd || i % 2 == 0) _rect.position += Vector3.up * _direction;

                    yield return new WaitForSeconds(StepSeconds);
                }

                _direction *= -1;
            }
        }
    }
}
