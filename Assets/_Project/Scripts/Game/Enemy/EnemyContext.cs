namespace MemorialFloor.Game
{
    /// <summary>
    /// 敵が常駐シーンから使うもの。敵は場所のシーンに居て常駐シーンへ参照を張れないので、
    /// 場所を読み込んだときに SceneStarter が作り、湧き口と敵へ渡す
    /// </summary>
    public sealed class EnemyContext
    {
        public EnemyContext(AreaTravel travel, RootsManager roots, QuestManager quests, PlayerHealth player, MiddleText middleText)
        {
            Travel = travel;
            Roots = roots;
            Quests = quests;
            Player = player;
            MiddleText = middleText;
        }

        public AreaTravel Travel { get; }

        public RootsManager Roots { get; }

        public QuestManager Quests { get; }

        /// <summary>敵が追いかけて当たる相手</summary>
        public PlayerHealth Player { get; }

        public MiddleText MiddleText { get; }
    }
}
