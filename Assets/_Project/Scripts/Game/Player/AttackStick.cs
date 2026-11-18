using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>攻撃のスティック。押して構え、離した瞬間の向きへ振る。向きの矢印と攻撃の案内も出す</summary>
    public sealed class AttackStick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        /// <summary>矢印を出すのは、この倒し具合を超えたとき</summary>
        private const float Threshold = 0.5f;

        [SerializeField, FormerlySerializedAs("joystick")] private FixedJoystick _stick;
        [SerializeField] private PlayerAttack _attack;
        [SerializeField, FormerlySerializedAs("up")] private GameObject _up;
        [SerializeField, FormerlySerializedAs("down")] private GameObject _down;
        [SerializeField, FormerlySerializedAs("left")] private GameObject _left;
        [SerializeField, FormerlySerializedAs("right")] private GameObject _right;

        private AttackGuide _guide;

        /// <summary>
        /// 押しているあいだの、最後のスティックの倒し具合。同じ物の部品は並び順に呼ばれ、
        /// スティックがこの部品より先にあると、離したときには先に 0 に戻っている
        /// </summary>
        private Vector2 _held;

        private void Start()
        {
            //攻撃のキーは右下、向きのキーは左上に分けて置く。← と → を隣に並べると、1 本の長い矢印に見える。
            //下の辺に 2 つ並べると、スティックの幅に収まらず重なる
            KeyHint.Put((RectTransform)transform, PlayerInput.AttackKeyName);
            KeyHint.Put((RectTransform)transform, "←↑↓→", Vector2.up);
            _guide = AttackGuide.Put((RectTransform)transform, PlayerInput.AttackKeyName);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _held = _stick.Direction;
            _attack.Press();
        }

        public void OnDrag(PointerEventData eventData)
        {
            _held = _stick.Direction;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            Vector2 stick = _stick.Direction != Vector2.zero ? _stick.Direction : _held;
            _held = Vector2.zero;
            _attack.Release(PlayerInput.AimOf(stick));
        }

        private void Update()
        {
            //矢印キーで向けているあいだも出す
            Vector2 aim = PlayerInput.AimOf(_stick.Direction);
            _up.SetActive(aim.y > Threshold);
            _down.SetActive(aim.y < -Threshold);
            _right.SetActive(aim.x > Threshold);
            _left.SetActive(aim.x < -Threshold);

            _guide.Show(_attack.Doing, _attack.Aimed);
        }
    }
}
