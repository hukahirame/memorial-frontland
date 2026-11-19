using System;

namespace MemorialFloor.Domain
{
    /// <summary>
    /// 回避攻撃。体を縦の軸で 1 回転させながら手前か奥へ抜ける。回り始めから少しのあいだ当たらず（<see cref="Invulnerability.DodgeSeconds"/>）、
    /// 回っているあいだは、剣を振ったときより短い、体のまわりの範囲に当てる
    /// </summary>
    public static class RollRule
    {
        /// <summary>全体の秒数。これが過ぎるまで次は出ない</summary>
        public const float Seconds = 0.7f;

        /// <summary>抜ける距離</summary>
        public const float Distance = 1.5f;

        /// <summary>回り始めから当たり始めるまで</summary>
        public const float StrikeFrom = 0.16f;

        /// <summary>当たる長さ</summary>
        public const float StrikeSeconds = 0.39f;

        /// <summary>当たる範囲の、体からの半径。剣の振りが前へ届く距離（ケサで約 0.7、突きで約 0.85）より短い</summary>
        public const float Radius = 0.5f;

        /// <summary>進み具合 0〜1 のときに、抜ける距離のどれだけを進んだか。終わり際ほどゆっくりにする</summary>
        public static float Travelled(float progress)
        {
            float rest = 1f - Math.Max(0f, Math.Min(1f, progress));
            return 1f - rest * rest;
        }
    }
}
