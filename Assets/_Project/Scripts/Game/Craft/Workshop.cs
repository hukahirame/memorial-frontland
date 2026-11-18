using MemorialFloor.Domain;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 作業台で作る・買う。できた品は作業台の上に落とす。
    /// 倒れている間と場所を移っている間、作業台の無い場所では作らない
    /// </summary>
    public sealed class Workshop : MonoBehaviour
    {
        private const string BenchTag = "CraftBench";

        /// <summary>できた品を落とす、作業台からの高さ</summary>
        private const float DropHeight = 0.5f;

        [SerializeField] private PlayerInventory _inventory;
        [SerializeField] private Purse _purse;
        [SerializeField] private PlayerMovement _movement;
        [SerializeField] private MiddleText _middleText;

        [Tooltip("落とし物の prefab")]
        [SerializeField] private GameObject _dropPrefab;

        /// <summary>材料を使って 1 つ作る。材料が足りなければ作らない</summary>
        public void Make(Recipe recipe)
        {
            //移る途中に作ると、品は前の場所と一緒に消えて材料だけ減る
            if (!_movement.CanAct) return;

            if (!recipe.CanCraftWith(_inventory.Inventory))
            {
                Debug.Log("材料が足りません!!");
                return;
            }

            //画面は作業台を離れても閉じない。材料を減らしてから探すと材料だけ消える
            GameObject bench = Bench();
            if (bench == null) return;

            foreach (Ingredient ingredient in recipe.Ingredients)
            {
                for (int i = 0; i < ingredient.Amount; i++) _inventory.UnloadInventory(ingredient.ItemId);
            }

            Drop(recipe.ProductId, bench);
        }

        /// <summary>所持金で 1 つ買う。値段の無い品は買えない</summary>
        public void Buy(ItemDefinition item)
        {
            if (item.Price <= 0) return;
            if (!_movement.CanAct) return;

            //払ってから探すと所持金だけ減る
            GameObject bench = Bench();
            if (bench == null) return;

            if (!_purse.Coins.Spend(item.Price))
            {
                _middleText.Midtxt_Anim("所持金が足りません");
                return;
            }

            Drop(item.ItemId, bench);
        }

        //作業台は場所のシーンにあり、常駐シーンから参照を張れないので、押したときに引く
        private static GameObject Bench()
        {
            return GameObject.FindWithTag(BenchTag);
        }

        private void Drop(string itemId, GameObject bench)
        {
            GameObject drop = Instantiate(_dropPrefab, bench.transform.position + Vector3.up * DropHeight, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(drop, bench.scene);
            drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
        }
    }
}
