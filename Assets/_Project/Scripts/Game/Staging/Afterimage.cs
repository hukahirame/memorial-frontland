using System.Collections.Generic;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>残像 1 枚。写した時点の姿を色を付けて残し、薄くしながら消す</summary>
    public sealed class Afterimage : MonoBehaviour
    {
        private readonly List<SpriteRenderer> _parts = new List<SpriteRenderer>();
        private readonly List<float> _alphas = new List<float>();
        private Color _tint;
        private float _born;
        private float _seconds;

        /// <summary>body より下のいま見えている絵を、親子ごと同じ形で写して残す。root はプレイヤーの一番上</summary>
        public static void Leave(Transform body, Transform root, Color tint, float seconds)
        {
            var ghost = new GameObject("Afterimage");
            Transform top = ghost.transform;
            top.SetPositionAndRotation(root.position, root.rotation);
            top.localScale = root.lossyScale;

            var afterimage = ghost.AddComponent<Afterimage>();
            afterimage._tint = tint;
            afterimage._born = Time.time;
            afterimage._seconds = seconds;

            var copies = new Dictionary<Transform, Transform> { { root, top } };
            foreach (SpriteRenderer source in body.GetComponentsInChildren<SpriteRenderer>())
            {
                if (!source.enabled || source.sprite == null || source.color.a <= 0f) continue;

                SpriteRenderer part = Copy(source.transform, copies).gameObject.AddComponent<SpriteRenderer>();
                part.sprite = source.sprite;
                part.flipX = source.flipX;
                part.flipY = source.flipY;
                part.drawMode = source.drawMode;
                if (source.drawMode != SpriteDrawMode.Simple) part.size = source.size;
                part.sharedMaterial = source.sharedMaterial;
                part.sortingLayerID = source.sortingLayerID;
                part.sortingOrder = source.sortingOrder;
                part.maskInteraction = source.maskInteraction;

                afterimage._parts.Add(part);
                afterimage._alphas.Add(source.color.a);
            }

            afterimage.Fade(1f);
        }

        private void Update()
        {
            float left = 1f - (Time.time - _born) / _seconds;
            if (left <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Fade(left);
        }

        private void Fade(float left)
        {
            for (int i = 0; i < _parts.Count; i++)
            {
                _parts[i].color = new Color(_tint.r, _tint.g, _tint.b, _tint.a * _alphas[i] * left);
            }
        }

        //original と同じ親子と位置・向き・大きさを、写しの側にたどって作る。縮み伸びや左右の反転もそのまま写る
        private static Transform Copy(Transform original, Dictionary<Transform, Transform> copies)
        {
            if (copies.TryGetValue(original, out Transform copy)) return copy;

            copy = new GameObject(original.name).transform;
            copy.gameObject.layer = original.gameObject.layer;
            copy.SetParent(Copy(original.parent, copies), false);
            copy.localPosition = original.localPosition;
            copy.localRotation = original.localRotation;
            copy.localScale = original.localScale;
            copies[original] = copy;
            return copy;
        }
    }
}
