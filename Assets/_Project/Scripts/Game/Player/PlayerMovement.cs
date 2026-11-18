using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.UI;

namespace MemorialFloor.Game
{
    /// <summary>
    /// プレイヤーの移動。入力の向きへ歩き、絵を左右に向け、足音と歩く・止まる姿を切り替える。跳ぶ。
    /// 場の外へ出かけたら押し戻し、抜け出たら決まった地点へ連れ戻す
    /// </summary>
    public sealed class PlayerMovement : MonoBehaviour
    {
        /// <summary>持ち物で足す前の歩く速さ</summary>
        public const float BaseSpeed = 1f;

        /// <summary>絵の向きを変えるのは、横にこの倒し具合を超えたとき</summary>
        private const float TurnThreshold = 0.1f;

        /// <summary>地面を探す光線の始まり（足もとから下へ）と長さ</summary>
        private const float GroundProbeDrop = 0.6f;
        private const float GroundProbeLength = 0.2f;

        /// <summary>跳ぶ強さ。重さに掛ける</summary>
        private const float JumpLift = 4f;

        /// <summary>場から抜け出て連れ戻したときに与える横の速さ</summary>
        private const float ReturnSpeed = 2f;

        /// <summary>場の端から押し戻す速さ。端からの差に掛ける</summary>
        private const float PushBackRate = 0.5f;

        [SerializeField] private Character _character;
        [SerializeField] private Transform _body;
        [SerializeField] private Button _jumpButton;
        [SerializeField] private AudioSource _footstep;
        [SerializeField] private AreaTravel _travel;

        private PlayerInput _input;
        private PlayerHealth _health;
        private Rigidbody _rigidbody;
        private Collider _solidBody;
        private bool _isOnGround;

        /// <summary>歩く速さ。持ち物（速さの首飾り）が決める</summary>
        public float Speed { get; set; } = BaseSpeed;

        /// <summary>向きを変えずに歩く。攻撃を構えてから振り終わるまで</summary>
        public bool IsFacingLocked { get; set; }

        /// <summary>倒れている間と場所を移っている間は、歩くのも振るのも、持ち物を使うのも止める</summary>
        public bool CanAct
        {
            get { return !_health.IsDead && !_travel.IsTraveling; }
        }

        /// <summary>絵が向いている横の側。右なら 1、左なら -1。絵は左右にしか向かないので、柵はこの側に立てる</summary>
        public float Side
        {
            get { return _body.localScale.x < 0f ? -1f : 1f; }
        }

        /// <summary>体の固い当たり。作業台と出口は、これが重なったときだけ数える。倒れているあいだは切られる</summary>
        public Collider Body
        {
            get { return _solidBody; }
        }

        private void Awake()
        {
            _input = GetComponent<PlayerInput>();
            _health = GetComponent<PlayerHealth>();
            _rigidbody = GetComponent<Rigidbody>();
            _solidBody = SolidBody();
        }

        private void OnEnable()
        {
            _input.JumpPressed += Jump;
        }

        private void OnDisable()
        {
            _input.JumpPressed -= Jump;
        }

        private void Start()
        {
            KeyHint.Put((RectTransform)_jumpButton.transform, PlayerInput.JumpKey.ToString());
        }

        /// <summary>いま地面に立っているか。毎コマの判定とは別に、置き直した直後でも正しく答える</summary>
        public bool IsGrounded()
        {
            return Physics.Raycast(transform.position + Vector3.down * GroundProbeDrop, Vector3.down, GroundProbeLength);
        }

        private void Update()
        {
            _isOnGround = IsGrounded();
            _jumpButton.interactable = _isOnGround;

            KeepInField();

            if (CanAct) Walk(_input.Move);
            else StandStill();
        }

        private void Walk(Vector2 move)
        {
            if (!IsFacingLocked)
            {
                if (move.x > TurnThreshold && _body.localScale.x == -1f) _body.localScale = Vector3.one;
                else if (move.x < -TurnThreshold && _body.localScale.x == 1f) _body.localScale = new Vector3(-1f, 1f, 1f);
            }

            Heading heading = MoveRule.Direction(move.x, move.y);
            if (!heading.IsStill)
            {
                if (_isOnGround || _character.GetState() == CharacterState.Idle) _character.SetState(CharacterState.Walk);
                if (!_footstep.loop) _footstep.loop = true;
                if (!_footstep.isPlaying) _footstep.Play();
            }
            else if (_footstep.loop)
            {
                _character.SetState(CharacterState.Idle);
                _footstep.loop = false;
                _footstep.Stop();
            }

            transform.Translate(Speed * new Vector3(heading.X, 0f, heading.Z) * Time.deltaTime);
        }

        //歩けないあいだは足音も止める。Walk の止め方は手を離したときにしか通らない。倒れた姿はそのまま残す
        private void StandStill()
        {
            if (_footstep.loop)
            {
                _footstep.loop = false;
                _footstep.Stop();
            }
            if (!_health.IsDead && _character.GetState() == CharacterState.Walk) _character.SetState(CharacterState.Idle);
        }

        private void Jump()
        {
            if (!_isOnGround || !CanAct) return;

            Vector2 move = _input.Move;
            _rigidbody.AddForce(move.x, _rigidbody.mass * JumpLift, move.y, ForceMode.Impulse);
            _isOnGround = false;
            _jumpButton.interactable = false;
        }

        //緊急脱出用。場の範囲は立っている所の場所のもの。境目では隣の場所の範囲に入るので、押し戻さずに歩いて越えられる。
        //移っているあいだは見ない。止めた体には速さも入れられない
        private void KeepInField()
        {
            if (_travel.IsTraveling) return;

            Area field = _travel.FieldAt(transform.position);
            if (field == null) return;

            Vector3 position = transform.position;
            FieldBounds bounds = Exposition.ToBounds(field.Exposition, field.Ceiling);
            if (bounds.HasEscaped(position.x, position.y, position.z))
            {
                //今いる場所の決まった地点（根源なら入口）へ連れ戻す
                transform.position = _travel.Current.PointFor(Arrival.Home);
                _rigidbody.linearVelocity = Vector3.right * ReturnSpeed;
                return;
            }

            //押し戻すのは横だけ。縦まで 0 にすると、壁際で跳んでも宙で止まり、吹き飛ばしも消える
            Nudge nudge = bounds.PushBack(position.x, position.z);
            if (!nudge.None) _rigidbody.linearVelocity = new Vector3(nudge.X * PushBackRate, _rigidbody.linearVelocity.y, nudge.Z * PushBackRate);
        }

        private Collider SolidBody()
        {
            foreach (MeshCollider found in GetComponentsInChildren<MeshCollider>())
            {
                if (!found.isTrigger) return found;
            }

            return null;
        }
    }
}
