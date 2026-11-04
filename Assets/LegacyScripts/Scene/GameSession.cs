using UnityEngine;

/// <summary>
/// 遊びの状態を、起動した直後と同じに戻す。タイトルが開くたびに呼ぶ。
/// 遊びの状態の多くは static に置かれ、シーンを読み直しても残る。戻さないと、設定からタイトルに戻って
/// 別の枠や「はじめる」で始めたときに、前に遊んだ根源・クエスト・時刻などが混ざる。
/// 遊びの状態を static に足したら、ここにも足す
/// </summary>
public static class GameSession
{
    public static void Reset()
    {
        //根源は常駐シーンの RootsManager が作り直す
        RootsManager.Roots.Clear();
        RootsManager.Raid.Clear();

        QuestManager.Quests.Clear();
        QuestManager.ordered_id = "";
        RewardUI.rewardUI_show = 0;
        RewardUI.rewardUI_id = "";

        Sun2.Cycle.SetElapsed(0);
        Sun2.Plan.Clear();
        OF_Spawner.spawnable = false;

        //速さの首飾りの分は、持ち物を入れるときに足し直される
        Player2.speed = 1f;
        Player2.cool = false;
        Player2.horizontalKey = 0f;
        Player2.verticalKey = 0f;

        //上限は表示から 1 度だけ取るので、取り直さない。始めの体力は上限
        if (Player2.Hp.IsReady) Player2.Hp.SetCurrent(Player2.Hp.Max);

        //設定の窓を開いたまま戻ると、時間が止まったまま残る
        Time.timeScale = 1f;
    }
}
