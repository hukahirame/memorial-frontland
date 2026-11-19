namespace MemorialFloor.Domain
{
    /// <summary>
    /// 攻撃の時間割。振りかぶり、当たる、戻りの順に進む。
    /// 当たる時間は、振りのアニメの打点（振り始めから約 0.32 秒）を挟む
    /// </summary>
    public static class AttackTiming
    {
        /// <summary>振り始めから当たり始めるまで</summary>
        public const float WindupSeconds = 0.2f;

        /// <summary>当たる長さ</summary>
        public const float ActiveSeconds = 0.25f;

        /// <summary>ケサの全体。これが過ぎるまで次は出ない</summary>
        public const float SwingSeconds = 0.6f;

        /// <summary>突きの全体。これが過ぎるまで次は出ない</summary>
        public const float ThrustSeconds = 0.7f;

        /// <summary>ステップの全体</summary>
        public const float StepSeconds = 0.8f;

        /// <summary>終わる前のこの長さに入った入力は捨てずに預かり、終わってすぐ出す</summary>
        public const float BufferSeconds = 0.25f;

        /// <summary>duration の攻撃が elapsed 経ったところで来た入力を、次の攻撃として預かるか</summary>
        public static bool IsBuffered(float elapsed, float duration)
        {
            return elapsed >= duration - BufferSeconds;
        }
    }
}
