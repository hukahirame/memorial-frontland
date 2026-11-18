using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>移動のスティックに、歩いている向きの矢印と、代わりに押すキーの案内を出す</summary>
    public sealed class MoveStickArrows : MonoBehaviour
    {
        /// <summary>矢印を出すのは、この倒し具合を超えたとき</summary>
        private const float Threshold = 0.5f;

        [SerializeField] private PlayerInput _input;
        [SerializeField, FormerlySerializedAs("up")] private GameObject _up;
        [SerializeField, FormerlySerializedAs("down")] private GameObject _down;
        [SerializeField, FormerlySerializedAs("left")] private GameObject _left;
        [SerializeField, FormerlySerializedAs("right")] private GameObject _right;

        private void Start()
        {
            KeyHint.Put((RectTransform)transform, "WASD");
        }

        private void Update()
        {
            Vector2 move = _input.Move;
            _up.SetActive(move.y > Threshold);
            _down.SetActive(move.y < -Threshold);
            _right.SetActive(move.x > Threshold);
            _left.SetActive(move.x < -Threshold);
        }
    }
}
