using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MemorialFloor.Game
{
    /// <summary>
    /// プレイヤーへの入力。歩く向きは WASD か移動のスティック、攻撃の向きは攻撃のスティックか矢印キーから読む。
    /// 攻撃のキーを押した・離したことと、跳ぶこと（Space か画面のジャンプボタン）を知らせる
    /// </summary>
    [DefaultExecutionOrder(-10)]
    public sealed class PlayerInput : MonoBehaviour
    {
        /// <summary>画面の案内に出す、攻撃のキーの名前。AttackKeys を変えたら合わせる</summary>
        public const string AttackKeyName = "Enter";

        /// <summary>ジャンプボタンの代わりに押すキー。離したときに跳ぶ</summary>
        public const KeyCode JumpKey = KeyCode.Space;

        /// <summary>攻撃のスティックの代わりに押すキー。2 つで 1 つのキーとして扱う</summary>
        private static readonly KeyCode[] AttackKeys = { KeyCode.Return, KeyCode.KeypadEnter };

        [SerializeField] private FixedJoystick _moveStick;
        [SerializeField] private FixedJoystick _attackStick;

        /// <summary>攻撃のキーを押して構えているか。2 つの Enter を続けて押しても、構えるのも振るのも 1 度にする</summary>
        private bool _isAttackKeyHeld;

        /// <summary>攻撃のキーを押した</summary>
        public event Action AttackPressed;

        /// <summary>攻撃のキーを離した。渡すのは離したときの攻撃の向き</summary>
        public event Action<Vector2> AttackReleased;

        /// <summary>跳ぶ。Space を離したときと、画面のジャンプボタンを押したとき</summary>
        public event Action JumpPressed;

        /// <summary>歩く向き。WASD を押していればその向き、押していなければ移動のスティックの倒し具合</summary>
        public Vector2 Move
        {
            get
            {
                //矢印キーは攻撃の向きに使う。GetAxisRaw は矢印も拾うので、WASD を直に読む
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.D))
                {
                    return new Vector2(KeyAxis(KeyCode.D, KeyCode.A), KeyAxis(KeyCode.W, KeyCode.S));
                }

                return new Vector2(_moveStick.Horizontal, _moveStick.Vertical);
            }
        }

        /// <summary>いまの攻撃の向き。長さは 1 まで</summary>
        public Vector2 Aim
        {
            get { return AimOf(_attackStick.Direction); }
        }

        /// <summary>攻撃の向き。stick を倒していればその向き、倒していなければ矢印キーの向き。長さは 1 まで</summary>
        public static Vector2 AimOf(Vector2 stick)
        {
            if (stick != Vector2.zero) return stick;

            return new Vector2(KeyAxis(KeyCode.RightArrow, KeyCode.LeftArrow), KeyAxis(KeyCode.UpArrow, KeyCode.DownArrow)).normalized;
        }

        /// <summary>画面のジャンプボタンから呼ぶ</summary>
        public void PressJump()
        {
            JumpPressed?.Invoke();
        }

        private void Start()
        {
            //遊ぶあいだ、Enter（攻撃）と Space（ジャンプ）と矢印・WASD は遊びの操作に使う。画面のボタンの決定や選びの移動にも取られると、
            //マウスで押したボタンが選ばれたまま残り、攻撃や跳ぶたびにそのボタンもまた押される。キーで押すボタンは KeyShortcut が受け持つ
            if (EventSystem.current != null) EventSystem.current.sendNavigationEvents = false;
        }

        private void Update()
        {
            if (Input.GetKeyUp(JumpKey)) PressJump();

            //押してすぐ離した 1 コマでも振れるよう、押したかと押しているかを分けて見る。2 つの Enter は、どちらも離したときに振る
            if (!_isAttackKeyHeld && IsAnyAttackKey(Input.GetKeyDown))
            {
                _isAttackKeyHeld = true;
                AttackPressed?.Invoke();
            }
            if (_isAttackKeyHeld && !IsAnyAttackKey(Input.GetKey))
            {
                _isAttackKeyHeld = false;
                AttackReleased?.Invoke(Aim);
            }
        }

        private static float KeyAxis(KeyCode positive, KeyCode negative)
        {
            return (Input.GetKey(positive) ? 1f : 0f) - (Input.GetKey(negative) ? 1f : 0f);
        }

        /// <summary>攻撃のキーのどれかが state に当てはまるか。state は Input.GetKey など</summary>
        private static bool IsAnyAttackKey(Func<KeyCode, bool> state)
        {
            foreach (KeyCode key in AttackKeys)
            {
                if (state(key)) return true;
            }

            return false;
        }
    }
}
