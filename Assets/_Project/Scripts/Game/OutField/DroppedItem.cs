using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 地面に落ちた品。プレイヤーの体が拾う当たりに入ると持ち物へ入れて消える。品の ID は絵の名前で読む。
    /// 持ち物が一杯で拾えなかったものも、空いたあとに触れていれば拾う
    /// </summary>
    public sealed class DroppedItem : MonoBehaviour
    {
        /// <summary>拾う側の体の層</summary>
        private const int PlayerLayer = 8;

        /// <summary>拾った品の枠に出す耐久。落ちた品は耐久を持ち越さない</summary>
        private const int FreshDurability = 0;

        /// <summary>拾われた。消えるのはフレームの終わりなので、それまでの物理の段で触れ直しても二重に拾わない</summary>
        private bool _isPicked;

        /// <summary>この時刻（Time.time）までは触れても拾わない</summary>
        private float _pickableAt;

        /// <summary>プレイヤーが一度、拾う当たりの外へ出るまで拾わない</summary>
        private bool _isAwaitingLeave;

        /// <summary>拾う当たり。プレイヤーの体がこの中にあるかを見る</summary>
        private SphereCollider _reach;

        /// <summary>
        /// しばらく拾えなくし、プレイヤーが一度離れるまでも拾わない。捨てた品は拾う当たりの届く所に落ちるので、
        /// 時間だけで待たせると、立ち止まっているうちに拾い直して枠がまた埋まる
        /// </summary>
        public void HoldOff(float seconds)
        {
            _pickableAt = Time.time + seconds;
            _isAwaitingLeave = true;
        }

        private void Awake()
        {
            _reach = GetComponent<SphereCollider>();
        }

        private void FixedUpdate()
        {
            transform.Rotate(0f, 1f, 0f);
            if (_isAwaitingLeave && !IsPlayerInReach()) _isAwaitingLeave = false;
        }

        //触れている知らせは、止まって眠った物どうしでは来ないことがあるので、当たりの中を問い合わせて見る
        private bool IsPlayerInReach()
        {
            Vector3 scale = transform.lossyScale;
            float radius = _reach.radius * Mathf.Max(scale.x, scale.y, scale.z);
            return Physics.CheckSphere(transform.TransformPoint(_reach.center), radius, 1 << PlayerLayer, QueryTriggerInteraction.Collide);
        }

        private void OnTriggerEnter(Collider other)
        {
            PickUp(other);
        }

        //持ち物が一杯で拾えなかったものも、空いたあとに触れていれば拾う。入り直すまで待たせない
        private void OnTriggerStay(Collider other)
        {
            PickUp(other);
        }

        private void PickUp(Collider other)
        {
            if (_isPicked || _isAwaitingLeave || Time.time < _pickableAt || other.gameObject.layer != PlayerLayer) return;

            PlayerReach player = other.GetComponentInParent<PlayerReach>();
            string itemId = GetComponent<SpriteRenderer>().sprite.name;
            if (player == null || !player.PickUp(itemId, FreshDurability)) return;

            _isPicked = true;
            Debug.Log(itemId + "を入手しました");
            Destroy(gameObject);
        }
    }
}
