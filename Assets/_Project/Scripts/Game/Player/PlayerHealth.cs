using System;
using System.Collections;
using MemorialFloor.Domain;
using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// プレイヤーの体力。打たれると減って押し返され、しばらく当たらずに点滅する。少しずつ回復する。
    /// 体力が変わったことと、倒れたことを知らせる
    /// </summary>
    public sealed class PlayerHealth : MonoBehaviour
    {
        /// <summary>打たれたときに押し返す強さ</summary>
        private const float KnockbackForce = 100f;

        /// <summary>点滅で赤くしている秒数と、元の色に戻している秒数</summary>
        private const float FlashRedSeconds = 0.3f;
        private const float FlashClearSeconds = 0.2f;

        [SerializeField] private int _max = 200;

        [Header("自然回復")]
        [SerializeField] private float _regenSeconds = 1f;
        [SerializeField] private int _regenAmount = 2;

        [SerializeField] private AreaTravel _travel;

        private readonly Health _health = new Health();
        private readonly Invulnerability _invulnerability = new Invulnerability();
        private Rigidbody _body;
        private float _sinceRegen;

        /// <summary>いまの体力か上限が変わった</summary>
        public event Action Changed;

        /// <summary>打たれて体力が 0 になった</summary>
        public event Action Died;

        public int Current
        {
            get { return _health.Current; }
        }

        public int Max
        {
            get { return _health.Max; }
        }

        public bool IsDead
        {
            get { return _health.IsDead; }
        }

        private void Awake()
        {
            _body = GetComponent<Rigidbody>();
            _health.SetMax(_max);
            _health.SetCurrent(_max);
        }

        private void Update()
        {
            _sinceRegen += Time.deltaTime;
            if (_sinceRegen < _regenSeconds) return;

            _sinceRegen = 0f;
            Heal(_regenAmount);
        }

        /// <summary>回復する。上限で頭打ちで、倒れているあいだは戻らない</summary>
        public void Heal(int amount)
        {
            int before = _health.Current;
            _health.Heal(amount);
            NotifyIfChanged(before);
        }

        /// <summary>体力をこの値にする。読み込みと、倒れたあとの起き上がりに使う</summary>
        public void SetCurrent(int value)
        {
            int before = _health.Current;
            _health.SetCurrent(value);
            NotifyIfChanged(before);
        }

        /// <summary>
        /// 打たれる。direction は押し返す向き。倒れているあいだと、場所を移っているあいだ（倒れて拠点へ戻される途中も）と、
        /// 当たらないあいだは何もしない
        /// </summary>
        public void Hurt(int damage, Vector3 direction)
        {
            if (_health.IsDead || _travel.IsTraveling || _invulnerability.IsActive(Time.time)) return;

            _invulnerability.Begin(Time.time);
            int before = _health.Current;
            _health.Take(damage);
            NotifyIfChanged(before);
            _body.AddForce(direction.normalized * KnockbackForce, ForceMode.Impulse);
            StartCoroutine(Flash());

            if (_health.IsDead) Died?.Invoke();
        }

        /// <summary>回避を始めた。回り始めから少しのあいだ当たらない</summary>
        public void Dodge()
        {
            _invulnerability.Dodge(Time.time);
        }

        /// <summary>
        /// 倒れて戻される。動けるようになってからしばらく当たらず、打たれたあとと同じく点滅する。
        /// 戻った地点のまわりに攻め込んだ敵が居ても、離れる間を残す。移り始めてから呼ぶ
        /// </summary>
        public void GuardAfterRevive()
        {
            StartCoroutine(GuardUntilRevived());
        }

        private IEnumerator GuardUntilRevived()
        {
            //移り終わったフレームに当たらないよう、数え始めるまでのあいだも当たらないようにしておく
            _invulnerability.Hold();
            while (_travel.IsTraveling) yield return null;

            _invulnerability.Revived(Time.time);
            yield return Flash();
        }

        private IEnumerator Flash()
        {
            GetComponentInChildren<Wink>().Flinch();
            SpriteRenderer[] renderers = GetComponentsInChildren<SpriteRenderer>();

            //髪や目は色を付けてあるので、白ではなく元の色へ戻す
            Color[] colors = new Color[renderers.Length];
            for (int i = 0; i < renderers.Length; i++) colors[i] = renderers[i].color;

            while (_invulnerability.IsFlashing(Time.time))
            {
                foreach (SpriteRenderer part in renderers) part.color = new Color(1f, 0.2f, 0.2f, part.color.a);
                yield return new WaitForSeconds(FlashRedSeconds);

                for (int i = 0; i < renderers.Length; i++) renderers[i].color = colors[i];
                yield return new WaitForSeconds(FlashClearSeconds);
            }
        }

        private void NotifyIfChanged(int before)
        {
            if (_health.Current != before) Changed?.Invoke();
        }
    }
}
