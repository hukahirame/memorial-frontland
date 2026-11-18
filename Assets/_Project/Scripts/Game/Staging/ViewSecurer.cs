using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 視界を確保する。プレイヤーとカメラのあいだに入った木（名前に tree を含む絵）と、透ける素材の箱（名前に Cube を含む物）を薄くし、
    /// 出たら戻す
    /// </summary>
    public sealed class ViewSecurer : MonoBehaviour
    {
        [Tooltip("薄くしたときの不透明度")]
        [SerializeField, FormerlySerializedAs("alpha")] private float _alpha;

        private void OnTriggerEnter(Collider other)
        {
            SetAlpha(other, _alpha);
        }

        private void OnTriggerExit(Collider other)
        {
            SetAlpha(other, 1f);
        }

        private static void SetAlpha(Collider other, float alpha)
        {
            if (other.name.IndexOf("tree") != -1)
            {
                SpriteRenderer sprite = other.GetComponent<SpriteRenderer>();
                Color color = sprite.color;
                color.a = alpha;
                sprite.color = color;
            }

            if (other.name.IndexOf("Cube") != -1 && IsFading(other))
            {
                Material material = other.GetComponent<Renderer>().material;
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }

        /// <summary>
        /// 透ける素材か。不透明の素材は色の透明度を変えても見た目が変わらないので触らない。
        /// 触ると素材が物ごとに複製され、まとめて描けなくなる（static batching が崩れる）
        /// </summary>
        private static bool IsFading(Collider other)
        {
            Renderer shown = other.GetComponent<Renderer>();
            return shown != null && shown.sharedMaterial != null && shown.sharedMaterial.renderQueue >= (int)RenderQueue.AlphaTest;
        }
    }
}
