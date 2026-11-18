using System.Collections;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 手前か奥へ突くとき、体を突く側へひねる。左右の向きは、この物の大きさの x が持つ（左向きなら -1）
    /// </summary>
    public sealed class SideJab : MonoBehaviour
    {
        private const int Steps = 15;
        private const float StepSeconds = 0.03f;

        /// <summary>direction は突く側。奥なら 1、手前なら -1</summary>
        public void Lean(int direction)
        {
            StartCoroutine(Leaning(direction));
        }

        private IEnumerator Leaning(int direction)
        {
            for (int i = 0; i < Steps; i++)
            {
                transform.parent.Rotate(0f, -direction * transform.localScale.x, 0f);
                yield return new WaitForSeconds(StepSeconds);
            }
        }
    }
}
