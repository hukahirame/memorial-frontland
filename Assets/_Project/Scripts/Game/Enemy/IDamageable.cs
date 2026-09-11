namespace MemorialFloor.Game
{
    /// <summary>攻撃が当たる側。当てる側は相手の種類を知らない</summary>
    public interface IDamageable
    {
        /// <summary>いまの体力。割合で弱める側が読む</summary>
        int CurrentHp { get; }

        /// <summary>損害を受ける。死亡の判定もこの中で済ませる</summary>
        void TakeDamage(int amount);
    }
}
