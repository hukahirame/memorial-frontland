using System;
using System.Collections;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// プレイヤーの攻撃。押して構え、離した向きへ振る。向きと体の向きで、突き・斬り・踏み込み・回避を選ぶ。
    /// 振っているあいだに離せば、終わってから続けて出す（先行入力）。当たりを出すときを知らせる
    /// </summary>
    public sealed class PlayerAttack : MonoBehaviour
    {
        /// <summary>剣の振りの音</summary>
        private const string SwingSound = "Fantasy_Game_Attack_Skill_Knife_Throw_B";

        /// <summary>回避攻撃の音の高さ。剣の振りと同じ音を高くして、軽く鳴らす</summary>
        private const float RollPitch = 1.3f;

        /// <summary>攻撃の縦の向き 1 で剣を回す角度</summary>
        private const float WeaponTilt = 90f;

        /// <summary>踏み込みで横へ押す強さ。重さに掛ける</summary>
        private const float StepPush = 3f;

        [SerializeField] private Character _character;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _body;
        [SerializeField] private Transform _weapon;

        private PlayerInput _input;
        private PlayerHealth _health;
        private PlayerMovement _movement;
        private Rigidbody _rigidbody;
        private SideJab _sideJab;

        /// <summary>構えてから振り終わるまで。このあいだは体の向きを変えずに歩く</summary>
        private bool _isAttacking;

        /// <summary>振っている。続けて出す先行入力も含めて、出し終わるまで</summary>
        private bool _isSwinging;

        /// <summary>出ている攻撃が始まった時刻と全体の長さ。先行入力を受けるかの判定に使う</summary>
        private float _attackStartedAt;
        private float _attackDuration;

        /// <summary>先行入力。出ている攻撃が終わったら、この向きで次を出す</summary>
        private bool _hasBufferedAttack;
        private Vector2 _bufferedAim;

        /// <summary>出ている攻撃。出ていなければ null</summary>
        private AttackMove? _swinging;

        /// <summary>剣の当たりを出す。渡すのは出しておく秒数</summary>
        public event Action<float> Struck;

        /// <summary>回避攻撃の当たりを出す。渡すのは秒数と、体のまわりの半径</summary>
        public event Action<float, float> SpinStruck;

        /// <summary>いまの行動。振っているあいだはその攻撃、構えているあいだは構え歩き。どちらでもなければ null</summary>
        public AttackMove? Doing
        {
            get
            {
                if (_swinging.HasValue) return _swinging;

                return IsBracing ? AttackMove.StanceWalk : (AttackMove?)null;
            }
        }

        /// <summary>構えているあいだ、いまの向きで離すと出る攻撃。構えていなければ null</summary>
        public AttackMove? Aimed
        {
            get
            {
                if (!IsBracing) return null;

                Vector2 aim = _input.Aim;
                return AttackRule.MoveOf(aim.x, aim.y, _body.localScale.x);
            }
        }

        /// <summary>攻撃を押したまま、まだ離していない</summary>
        private bool IsBracing
        {
            get { return _movement.CanAct && _isAttacking && !_isSwinging; }
        }

        private bool IsAttacking
        {
            get { return _isAttacking; }
            set
            {
                _isAttacking = value;
                _movement.IsFacingLocked = value;
            }
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _health = GetComponent<PlayerHealth>();
            _movement = GetComponent<PlayerMovement>();
            _rigidbody = GetComponent<Rigidbody>();
            _sideJab = GetComponentInChildren<SideJab>();
        }

        private void OnEnable()
        {
            _input.AttackPressed += Press;
            _input.AttackReleased += Release;
        }

        private void OnDisable()
        {
            _input.AttackPressed -= Press;
            _input.AttackReleased -= Release;
        }

        private void Update()
        {
            if (_weapon != null) _weapon.localEulerAngles = new Vector3(0f, 0f, _input.Aim.y * WeaponTilt);
        }

        /// <summary>構える。攻撃のキーか攻撃のスティックを押したとき</summary>
        public void Press()
        {
            IsAttacking = true;
        }

        /// <summary>aim の向きへ振る。aim は PlayerInput.AimOf で決めた攻撃の向き。振っている最中なら、終わってから出す</summary>
        public void Release(Vector2 aim)
        {
            StartCoroutine(AttackToward(aim));
        }

        private IEnumerator AttackToward(Vector2 aim)
        {
            if (_isSwinging)
            {
                if (AttackTiming.IsBuffered(Time.time - _attackStartedAt, _attackDuration))
                {
                    _hasBufferedAttack = true;
                    _bufferedAim = aim;
                }
                yield break;
            }
            _isSwinging = true;

            while (true)
            {
                yield return Swing(aim);

                if (!_hasBufferedAttack) break;
                _hasBufferedAttack = false;
                aim = _bufferedAim;
            }

            Finish();
        }

        private void Finish()
        {
            IsAttacking = false;
            _isSwinging = false;
            _swinging = null;
            _animator.transform.localEulerAngles = Vector3.zero;
        }

        private IEnumerator Swing(Vector2 aim)
        {
            if (!_movement.CanAct) yield break;

            IsAttacking = true;
            _attackStartedAt = Time.time;
            float facing = _sideJab.transform.localScale.x;

            AttackKind kind = AttackRule.Choose(aim.x, aim.y, facing);
            _swinging = AttackRule.MoveOf(aim.x, aim.y, facing);

            if (kind == AttackKind.Step)
            {
                _attackDuration = AttackTiming.StepSeconds;
                _rigidbody.AddForce(new Vector3(aim.x * StepPush, 1f, aim.y * StepPush) * _rigidbody.mass, ForceMode.Impulse);
                yield return new WaitForSeconds(AttackTiming.StepSeconds);
                yield break;
            }

            if (kind == AttackKind.Roll)
            {
                _attackDuration = RollRule.Seconds;
                yield return Roll(aim.y, facing);
                yield break;
            }

            _attackDuration = kind == AttackKind.Thrust ? AttackTiming.ThrustSeconds : AttackTiming.SwingSeconds;
            if (kind == AttackKind.Thrust)
            {
                int lean = AttackRule.ThrustDirection(aim.y);
                if (lean != 0) _sideJab.Lean(lean);
                _character.Jab();
            }
            else
            {
                _character.Slash();
            }
            TempAudio.TempAudioPlay(SwingSound);

            //当たるのは振りの打点のあたりだけ。振りかぶっているあいだに倒れたり移り始めたりしたら出さない
            yield return new WaitForSeconds(AttackTiming.WindupSeconds);
            if (!_movement.CanAct) yield break;
            Struck?.Invoke(AttackTiming.ActiveSeconds);
            yield return new WaitForSeconds(_attackDuration - AttackTiming.WindupSeconds);
        }

        //手前か奥へ抜ける。回り始めから少しのあいだ当たらず、回っているあいだは体のまわりに当てる。見た目は RollStaging が受け持つ
        private IEnumerator Roll(float vertical, float facing)
        {
            _health.Dodge();
            _character.Slash();
            TempAudio.TempAudioPlay(SwingSound, RollPitch);

            float depth = vertical > 0f ? 1f : -1f;
            //抜ける側へ、体の前から回る
            float turn = -360f * depth * Mathf.Sign(facing);
            RollStaging staging = new RollStaging(_animator.transform, transform, turn, FeetHeight());
            float elapsed = 0f;
            float travelled = 0f;
            bool isStriking = false;

            while (elapsed < RollRule.Seconds && _movement.CanAct)
            {
                elapsed = Mathf.Min(elapsed + Time.deltaTime, RollRule.Seconds);
                float progress = elapsed / RollRule.Seconds;

                float reach = RollRule.Travelled(progress) * RollRule.Distance;
                transform.Translate(0f, 0f, (reach - travelled) * depth);
                travelled = reach;

                staging.Pose(progress);

                if (!isStriking && elapsed >= RollRule.StrikeFrom)
                {
                    isStriking = true;
                    SpinStruck?.Invoke(RollRule.StrikeSeconds, RollRule.Radius);
                }

                yield return null;
            }

            staging.Restore();
        }

        /// <summary>足もとの高さ。体の固い当たりの下端</summary>
        private float FeetHeight()
        {
            Collider body = _movement.Body;
            return body != null ? body.bounds.min.y : transform.position.y;
        }
    }
}
