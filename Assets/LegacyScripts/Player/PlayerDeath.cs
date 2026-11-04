using Assets.HeroEditor.Common.Scripts.CharacterScripts;
using System.Collections;
using MemorialFloor.Game;
using UnityEngine;

public class PlayerDeath : MonoBehaviour
{
    private const int DeathPenalty = 100;
    private const int RevivePercent = 5;

    /// <summary>倒れてから起き上がるまでの秒数</summary>
    private const float ReviveSeconds = 5f;

    /// <summary>宙で倒れたとき、地面に着くのを待つ上限の秒数。床の無い所で倒れても、いつまでも落ち続けない</summary>
    private const float FallSeconds = 2f;

    [SerializeField] private AreaTravel travel;

    private Player2 p;

    public void DeathMainProcess()
    {
        p = GetComponent<Player2>();

        Player2.cool = true;
        p.character.SetState(CharacterState.DeathB);
        transform.GetComponentInChildren<Wink>().DeathEyesClose(-1000);
        GameObject.Find("BigText").GetComponent<BigText>().Bigtxt_Anim("体力が0になりました", "失敗");

        StartCoroutine(ReviveMainProcess());
    }

    IEnumerator ReviveMainProcess()
    {
        float revive = Time.time + ReviveSeconds;

        //宙で倒れたら、体の当たりを残したまま真下へ落とし、着いてから止める。先に止めると、その高さで浮いたままになる
        Player2.playerrb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionX | RigidbodyConstraints.FreezePositionZ;
        float giveUp = Time.time + FallSeconds;
        while (!p.Grounded() && Time.time < giveUp) yield return null;

        transform.GetComponentInChildren<MeshCollider>().enabled = false;
        if (transform.localScale.x == -1) transform.position += Vector3.down * 0.3f;
        Player2.playerrb.constraints = RigidbodyConstraints.FreezeAll;

        if (Time.time < revive) yield return new WaitForSeconds(revive - Time.time);

        Player2.cool = false;
        p.character.SetState(CharacterState.Idle);
        transform.GetComponentInChildren<Wink>().DeathEyesClose(0);
        transform.GetComponentInChildren<MeshCollider>().enabled = true;
        Player2.playerrb.constraints = RigidbodyConstraints.FreezeRotation;

        //受けているクエストが失敗になるのは、その根源で倒れたときだけ。通り道の根源や拠点では失敗にしない
        if (QuestManager.IsUnderwayIn(travel.Where.RootId)) RewardUI.rewardUI_show = 1;
        travel.Revive();
        p.GuardAfterRevive();

        GameManager.Coins.Spend(DeathPenalty);
        Player2.Hp.SetCurrent(Player2.Hp.Max * RevivePercent / 100);
        Player2.RefreshHpView();
    }

}
