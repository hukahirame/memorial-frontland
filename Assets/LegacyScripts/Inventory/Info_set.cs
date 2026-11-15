using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Info_set : MonoBehaviour
{
    /// <summary>捨てた品を拾わない秒。過ぎても、プレイヤーが一度離れるまでは拾わない（Dropitem）</summary>
    private const float DiscardHoldSeconds = 3f;

    /// <summary>捨てた品を、向いている側の斜め上へ放る勢い</summary>
    private const float DiscardToss = 1.5f;

    /// <summary>落とし物の prefab。敵や作業台が落とすものと同じ。Resources にある</summary>
    private const string DropPrefab = "Dropitem";

    /// <summary>使うボタンと捨てるボタンを並べるときの、1 つの幅と間。窓の幅は 500</summary>
    private const float PairWidth = 220f;
    private const float PairGap = 20f;

    [SerializeField] private Image mainImage;
    [SerializeField] private Text nametxt;
    [SerializeField] private Text txt;
    [SerializeField] private Button button;
    [SerializeField] private Sprite buttonsprite;
    [SerializeField] private PlacedObjects placed;

    [Header("手に持つ絵。原簿の ID と対で持つ")]
    [SerializeField] private string weapon1Id = "Ironsword";
    [SerializeField] private Sprite weapon1;
    [SerializeField] private string weapon2Id = "Legendsword";
    [SerializeField] private Sprite weapon2;

    private ItemDefinition current;

    /// <summary>捨てるボタン。使うボタンを写して作る。窓が閉じたまま Delete_Info が呼ばれることがあるので、要るときに作る</summary>
    private Button discard;

    /// <summary>使うボタンの、シーンで置いた位置と大きさ。並べるときに縮めるので、元を覚えておく</summary>
    private Vector2 buttonPosition;
    private Vector2 buttonSize;

    private AreaTravel travel;

    void Start()
    {
        button.gameObject.SetActive(false);
        DiscardButton().gameObject.SetActive(false);
    }

    private Button DiscardButton()
    {
        if (discard != null) return discard;

        var rect = (RectTransform)button.transform;
        buttonPosition = rect.anchoredPosition;
        buttonSize = rect.sizeDelta;

        discard = Instantiate(button, button.transform.parent);
        discard.name = "Discard";
        //写すとシーンで繋いだ「使う」の呼び先も付いてくる。Inspector で繋いだ呼び先は RemoveAllListeners では外れない
        discard.onClick = new Button.ButtonClickedEvent();
        discard.onClick.AddListener(Throw);
        discard.GetComponentInChildren<Text>(true).text = "捨てる";
        discard.gameObject.SetActive(false);

        return discard;
    }

    //使うボタンがあれば左右に並べ、無ければ捨てるボタンを使うボタンの位置に出す
    private void Lay(bool usable)
    {
        var use = (RectTransform)button.transform;
        var throwAway = (RectTransform)DiscardButton().transform;
        float offset = (PairWidth + PairGap) / 2f;

        use.anchoredPosition = buttonPosition + Vector2.left * offset;
        use.sizeDelta = new Vector2(PairWidth, buttonSize.y);

        throwAway.anchoredPosition = usable ? buttonPosition + Vector2.right * offset : buttonPosition;
        throwAway.sizeDelta = usable ? new Vector2(PairWidth, buttonSize.y) : buttonSize;
    }

    public int Show_Info2(string s)　// アイテム情報フレームへの代入
    {
        current = GameManager.Items.Find(s);
        if (current == null) return 0;

        nametxt.text = current.Name;
        txt.text = current.Description;
        mainImage.sprite = Resources.Load<Sprite>(s);

        // 非表示のボタンからも文字を取る。既定の GetComponentInChildren は
        // 活性なものしか返さず、設置できないアイテムを選ぶと null になっていた
        button.GetComponentInChildren<Text>(true).text = Label(current.Use);
        button.gameObject.SetActive(current.Installable);
        DiscardButton().gameObject.SetActive(true);
        Lay(current.Installable);

        return 1;
    }

    private static string Label(ItemUse use)
    {
        switch (use)
        {
            case ItemUse.Eat: return "食べる";
            case ItemUse.Equip: return "装備";
            default: return "設置";
        }
    }

    public void Delete_Info()
    {
        current = null;
        mainImage.sprite = buttonsprite;
        nametxt.text = "";
        txt.text = "";
        button.gameObject.SetActive(false);
        DiscardButton().gameObject.SetActive(false);
    }

    /// <summary>
    /// 選んでいる品を 1 つ捨てる。品は体から向いている側へ放られ、しばらくたって一度離れるまで拾えない。
    /// 倒れている間と移っている間は捨てない。装備している武器は、最後の 1 つなら捨てない
    /// </summary>
    public void Throw()
    {
        if (current == null) return;

        Player2 player = GameObject.FindWithTag("Player").GetComponent<Player2>();
        if (!player.CanAct) return;

        PlayerInventory inventory = GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>();
        string itemId = current.ItemId;
        int held = inventory.Inventory.CountOf(itemId);
        if (held <= 0) return;

        if (itemId == Weapon.Equipped.ItemId && held <= 1)
        {
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("装備中の武器は捨てられません");
            return;
        }

        //落とし物は場所のシーンに入れ、場所と一緒に消える。場所に居なければ捨てない
        if (travel == null) travel = FindAnyObjectByType<AreaTravel>();
        if (travel == null || travel.Current == null) return;

        GameObject drop = Instantiate(Resources.Load<GameObject>(DropPrefab), player.transform.position, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(drop, travel.Current.Scene);
        drop.GetComponent<SpriteRenderer>().sprite = Resources.Load<Sprite>(itemId);
        drop.GetComponent<Dropitem>().HoldOff(DiscardHoldSeconds);

        //体の中に出すので、品の硬い当たりは体と当たらないようにする。当たると物理が押し出し、放った向きと別の方へ飛ぶ。
        //拾う当たり（トリガー）まで外すと拾えなくなるので外さない
        Collider[] body = player.GetComponentsInChildren<Collider>();
        foreach (Collider solid in drop.GetComponentsInChildren<Collider>())
        {
            if (solid.isTrigger) continue;
            foreach (Collider part in body) Physics.IgnoreCollision(solid, part);
        }

        drop.GetComponent<Rigidbody>().AddForce(new Vector3(player.Side, 1f, 0f) * DiscardToss, ForceMode.Impulse);

        inventory.UnloadInventory(itemId);
    }

    public void Install()
    {
        if (current == null) return;

        //倒れている間と場所を移っている間は使えない。倒れていると食べても体力は戻らず、移る途中に置いた物は前の場所と一緒に消える
        if (!GameObject.FindWithTag("Player").GetComponent<Player2>().CanAct) return;

        switch (current.Use)
        {
            case ItemUse.Eat: Eat(); break;
            case ItemUse.Equip: Equip(); break;
            default: Place(); break;
        }
    }

    private void Eat()
    {
        Player2.Hp.Heal(current.Heal);
        Player2.RefreshHpView();
        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
    }

    private void Equip()
    {
        if (!Wield(current.ItemId)) return;

        TempAudio.TempAudioPlay("Fantasy_Game_Action_Backpack_Open");
    }

    /// <summary>武器を持つ。手の絵を替え、持ち物の「装備中」の印も付け直す。保存から戻すときも使う。装備できない品なら何もせず false</summary>
    public bool Wield(string itemId)
    {
        if (!Weapon.Equipped.Equip(GameManager.Items.Find(itemId))) return false;

        Sprite inhand = InHand(itemId);
        if (inhand != null) GameObject.Find("WeaponChild").GetComponent<SpriteRenderer>().sprite = inhand;

        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().MarkEquipped();
        return true;
    }

    /// <summary>手に持つ絵。原簿には置けないため Inspector で ID と対にしてある</summary>
    private Sprite InHand(string itemId)
    {
        if (itemId == weapon1Id) return weapon1;
        if (itemId == weapon2Id) return weapon2;

        return null;
    }

    private void Place()
    {
        Player2 player = GameObject.FindWithTag("Player").GetComponent<Player2>();
        Vector3 installpos = player.transform.position + Vector3.down * 0.4f;
        float yaw = 0f;

        //柵は足元ではなく、向いている側の格子の線に立てる。地面が無い所や、動かない物に重なる所には立てない
        GameObject prefab = PlacedObjects.Prefab(current.ItemId);
        if (prefab != null && prefab.TryGetComponent(out Fence fence)
            && !FenceSite.TryFind(fence, player.Body, player.Side, out installpos, out yaw))
        {
            GameObject.Find("MiddleText").GetComponent<MiddleText>().Midtxt_Anim("ここには置けません");
            return;
        }

        if (!placed.Place(current.ItemId, installpos, yaw)) return;
        GameObject.FindWithTag("PlayerInventory").GetComponent<PlayerInventory>().UnloadInventory(current.ItemId);
    }
}
