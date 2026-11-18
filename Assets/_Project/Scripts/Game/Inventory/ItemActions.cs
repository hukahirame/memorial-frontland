using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>持ち物の品を使う（食べる、持つ、置く）と、捨てる。倒れている間と場所を移っている間はどれもしない</summary>
    public sealed class ItemActions : MonoBehaviour
    {
        /// <summary>捨てた品を拾わない秒。過ぎても、プレイヤーが一度離れるまでは拾わない（DroppedItem）</summary>
        private const float DiscardHoldSeconds = 3f;

        /// <summary>捨てた品を、向いている側の斜め上へ放る勢い</summary>
        private const float DiscardToss = 1.5f;

        /// <summary>落とし物の prefab。敵や作業台が落とすものと同じ。Resources にある</summary>
        private const string DropPrefab = "Dropitem";

        /// <summary>置く物を足元に置くとき、体の中心から下げる量</summary>
        private const float PlaceDrop = 0.4f;

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private PlayerHealth _health;
        [SerializeField] private PlayerEquipment _equipment;
        [SerializeField] private PlacedObjects _placed;
        [SerializeField] private AreaTravel _travel;
        [SerializeField] private MiddleText _middleText;
        [SerializeField] private SoundEffects _sounds;

        /// <summary>
        /// 品を 1 つ使う。倒れていると食べても体力は戻らず、移る途中に置いた物は前の場所と一緒に消えるので、そのあいだは使わない
        /// </summary>
        public void Use(ItemDefinition item)
        {
            if (!_movement.CanAct) return;

            switch (item.Use)
            {
                case ItemUse.Eat: Eat(item); break;
                case ItemUse.Equip: Equip(item); break;
                default: Place(item); break;
            }
        }

        /// <summary>
        /// 品を 1 つ捨てる。品は体から向いている側へ放られ、しばらくたって一度離れるまで拾えない。
        /// 装備している武器は、最後の 1 つなら捨てない
        /// </summary>
        public void Throw(ItemDefinition item)
        {
            if (!_movement.CanAct) return;

            string itemId = item.ItemId;
            int held = _inventory.Inventory.CountOf(itemId);
            if (held <= 0) return;

            if (itemId == _equipment.Equipped.ItemId && held <= 1)
            {
                _middleText.Show("装備中の武器は捨てられません");
                return;
            }

            //落とし物は場所のシーンに入れ、場所と一緒に消える。場所に居なければ捨てない
            if (_travel.Current == null) return;

            Vector3 from = _movement.transform.position;
            GameObject drop = Instantiate(Resources.Load<GameObject>(DropPrefab), from, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(drop, _travel.Current.Scene);
            drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
            drop.GetComponent<DroppedItem>().HoldOff(DiscardHoldSeconds);

            //体の中に出すので、品の硬い当たりは体と当たらないようにする。当たると物理が押し出し、放った向きと別の方へ飛ぶ。
            //拾う当たり（トリガー）まで外すと拾えなくなるので外さない
            Collider[] body = _movement.GetComponentsInChildren<Collider>();
            foreach (Collider solid in drop.GetComponentsInChildren<Collider>())
            {
                if (solid.isTrigger) continue;
                foreach (Collider part in body) Physics.IgnoreCollision(solid, part);
            }

            drop.GetComponent<Rigidbody>().AddForce(new Vector3(_movement.Side, 1f, 0f) * DiscardToss, ForceMode.Impulse);

            _inventory.UnloadInventory(itemId);
        }

        private void Eat(ItemDefinition item)
        {
            _health.Heal(item.Heal);
            _inventory.UnloadInventory(item.ItemId);
        }

        private void Equip(ItemDefinition item)
        {
            _equipment.Equip(item);
            _sounds.Play("Fantasy_Game_Action_Backpack_Open");
        }

        private void Place(ItemDefinition item)
        {
            Vector3 position = _movement.transform.position + Vector3.down * PlaceDrop;
            float yaw = 0f;

            //柵は足元ではなく、向いている側の格子の線に立てる。地面が無い所や、動かない物に重なる所には立てない
            GameObject prefab = _placed.Prefab(item.ItemId);
            if (prefab != null && prefab.TryGetComponent(out Fence fence)
                && !FenceSite.TryFind(fence, _movement.Body, _movement.Side, out position, out yaw))
            {
                _middleText.Show("ここには置けません");
                return;
            }

            if (!_placed.Place(item.ItemId, position, yaw)) return;
            _inventory.UnloadInventory(item.ItemId);
        }
    }
}
