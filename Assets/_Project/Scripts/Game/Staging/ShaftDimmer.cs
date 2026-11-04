using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 日差しの筋を、日の光の強さに合わせて薄くする。筋は後ろを明るくするだけの板で、光の当たり方に関係なく光る。
    /// 一日の巡りは日の光の強さしか動かさないので、合わせないと夜も昼と同じ濃さで光る。
    /// 素材は筋どうしで共有するので、濃さは板ごとに上書きする
    /// </summary>
    public sealed class ShaftDimmer : MonoBehaviour
    {
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");

        [Tooltip("筋の濃さを決める日の光")]
        [SerializeField] private Light sun;

        [Tooltip("筋が素材どおりの濃さになる日の光の強さ。これより強くても濃くしない")]
        [SerializeField] private float fullAt = 1f;

        private Renderer[] _beams;
        private MaterialPropertyBlock _block;
        private float _strength = -1f;

        private void Awake()
        {
            _beams = GetComponentsInChildren<Renderer>();
            _block = new MaterialPropertyBlock();
        }

        private void LateUpdate()
        {
            float strength = Mathf.Clamp01(sun.intensity / fullAt);
            if (strength == _strength) return;

            _strength = strength;
            foreach (Renderer beam in _beams)
            {
                Color tint = beam.sharedMaterial.GetColor(TintColor);
                beam.GetPropertyBlock(_block);
                _block.SetColor(TintColor, new Color(tint.r * strength, tint.g * strength, tint.b * strength, tint.a));
                beam.SetPropertyBlock(_block);
            }
        }
    }
}
