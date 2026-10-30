using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using System.Collections;
using MemorialFloor.Game;
using UnityEngine;
using TMPro;

public class PlayerDeath : MonoBehaviour
{
    private const int DeathPenalty = 100;
    private const int RevivePercent = 5;

    [SerializeField] private AreaTravel travel;

    private Player2 p;

    public void DeathMainProcess()
    {
        p = GetComponent<Player2>();

        Player2.cool = true;
        p.character.SetState(CharacterState.DeathB);
        transform.GetComponentInChildren<Wink>().DeathEyesClose(-1000);
        transform.GetComponentInChildren<MeshCollider>().enabled = false;
        if (transform.localScale.x == -1) transform.position += Vector3.down * 0.3f;
        Player2.playerrb.constraints = RigidbodyConstraints.FreezeAll;
        GameObject.Find("BigText").GetComponent<BigText>().Bigtxt_Anim("体力が0になりました", "失敗");

        StartCoroutine(ReviveMainProcess());
    }

    IEnumerator ReviveMainProcess()
    {

        yield return new WaitForSeconds(5f);

        Player2.cool = false;
        p.character.SetState(CharacterState.Idle);
        transform.GetComponentInChildren<Wink>().DeathEyesClose(0);
        transform.GetComponentInChildren<MeshCollider>().enabled = true;
        Player2.playerrb.constraints = RigidbodyConstraints.FreezeRotation;

        RewardUI.rewardUI_show = 1;
        travel.Revive();

        GameManager.Coins.Spend(DeathPenalty);
        GameObject.Find("CoinText").GetComponent<TextMeshProUGUI>().text =
            GameManager.Coins.Amount.ToString();
        Player2.Hp.SetCurrent(Player2.Hp.Max * RevivePercent / 100);
        Player2.RefreshHpView();
    }

}
