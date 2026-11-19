namespace MemorialFloor.Domain
{
    /// <summary>場外へ落ちたかどうか</summary>
    public static class FallRule
    {
        /// <summary>この高さより下へ落ちたら戻れない</summary>
        public const float OutOfFieldY = -10f;

        public static bool IsOutOfField(float y)
        {
            return y < OutOfFieldY;
        }
    }
}
