using System.Collections;
using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using UnityEngine;
using UnityEngine.Serialization;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 倒れてから起き上がるまで。倒れると宙なら地面まで落ちて止まり、しばらくして起き上がる。
    /// 受けたクエストの根源で倒れていたら失敗にし、拠点へ戻され、所持金を払って少しの体力で立つ
    /// </summary>
    public sealed class PlayerDeath : MonoBehaviour
    {
        /// <summary>起き上がるときに払う所持金</summary>
        private const int DeathPenalty = 100;

        /// <summary>起き上がったときの体力。上限に対する割合（%）</summary>
        private const int RevivePercent = 5;

        /// <summary>倒れてから起き上がるまでの秒数</summary>
        private const float ReviveSeconds = 5f;

        /// <summary>宙で倒れたとき、地面に着くのを待つ上限の秒数。床の無い所で倒れても、いつまでも落ち続けない</summary>
        private const float FallSeconds = 2f;

        /// <summary>左を向いて倒れたとき、絵が浮かないよう下げる量</summary>
        private const float FacingLeftDrop = 0.3f;

        [SerializeField, FormerlySerializedAs("travel")] private AreaTravel _travel;
        [SerializeField, FormerlySerializedAs("purse")] private Purse _purse;
        [SerializeField, FormerlySerializedAs("quests")] private QuestManager _quests;
        [SerializeField] private BigText _bigText;

        private PlayerHealth _health;
        private PlayerMovement _movement;
        private Character _character;
        private Rigidbody _body;

        private void Awake()
        {
            _health = GetComponent<PlayerHealth>();
            _movement = GetComponent<PlayerMovement>();
            _character = GetComponentInChildren<Character>(true);
            _body = GetComponent<Rigidbody>();
        }

        private void OnEnable()
        {
            _health.Died += Fall;
        }

        private void OnDisable()
        {
            _health.Died -= Fall;
        }

        //倒れているあいだは体力が 0 で、歩くのも振るのも止まる（PlayerMovement.CanAct）
        private void Fall()
        {
            _character.SetState(CharacterState.DeathB);
            GetComponentInChildren<Wink>().Stop();
            _bigText.Bigtxt_Anim("体力が0になりました", "失敗");

            StartCoroutine(Revive());
        }

        private IEnumerator Revive()
        {
            float revive = Time.time + ReviveSeconds;

            //宙で倒れたら、体の当たりを残したまま真下へ落とし、着いてから止める。先に止めると、その高さで浮いたままになる
            _body.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
            float giveUp = Time.time + FallSeconds;
            while (!_movement.IsGrounded() && Time.time < giveUp) yield return null;

            GetComponentInChildren<MeshCollider>().enabled = false;
            if (transform.localScale.x == -1) transform.position += Vector3.down * FacingLeftDrop;
            _body.constraints = RigidbodyConstraints.FreezeAll;

            if (Time.time < revive) yield return new WaitForSeconds(revive - Time.time);

            _character.SetState(CharacterState.Idle);
            GetComponentInChildren<Wink>().Resume();
            GetComponentInChildren<MeshCollider>().enabled = true;
            _body.constraints = RigidbodyConstraints.FreezeRotation;

            //受けているクエストが失敗になるのは、その根源で倒れたときだけ。通り道の根源や拠点では失敗にしない
            if (_quests.IsUnderwayIn(_travel.Where.RootId)) _quests.Fail();
            _travel.Revive();
            _health.GuardAfterRevive();

            //起き上がるのは、体力が戻ったとき。ここまでの間はコマをまたがない
            _purse.Coins.Spend(DeathPenalty);
            _health.SetCurrent(_health.Max * RevivePercent / 100);
        }
    }
}
