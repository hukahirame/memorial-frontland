using System;

namespace MemorialFloor.Domain
{
    /// <summary>攻撃の種類</summary>
    public enum AttackKind
    {
        /// <summary>突き。体の向きと同じ側へ強く倒したとき</summary>
        Thrust,
        /// <summary>ケサ。どちらへも倒れていないとき</summary>
        Slash,
        /// <summary>ステップ。体の向きと逆へ倒したとき</summary>
        Step,
        /// <summary>回避攻撃。上下だけ倒したとき</summary>
        Roll
    }

    /// <summary>攻撃の案内に並べる行動。突きは上下の振りが付くかで分ける</summary>
    public enum AttackMove
    {
        /// <summary>ケサ</summary>
        Slash,
        /// <summary>突き。上下の振りが付かない</summary>
        Thrust,
        /// <summary>斜め突き。上下にも倒した突きで、上下の振りが付く</summary>
        DiagonalThrust,
        /// <summary>ステップ</summary>
        Step,
        /// <summary>回避攻撃</summary>
        Roll,
        /// <summary>構え歩き。攻撃を構えたまま、体の向きを変えずに歩く。離すまで攻撃は出ない</summary>
        StanceWalk
    }

    /// <summary>攻撃スティックの傾きから何が出るかを決める</summary>
    public static class AttackRule
    {
        /// <summary>倒したとみなす傾き</summary>
        public const float Threshold = 0.5f;

        /// <summary>facing は体の向き。正なら右、負なら左を向いている</summary>
        public static AttackKind Choose(float horizontal, float vertical, float facing)
        {
            if (Math.Abs(horizontal) > Threshold && horizontal * facing > 0f) return AttackKind.Thrust;
            if (Math.Abs(horizontal) < Threshold && Math.Abs(vertical) < Threshold) return AttackKind.Slash;
            if (Math.Abs(horizontal) < Threshold && Math.Abs(vertical) > Threshold) return AttackKind.Roll;

            return AttackKind.Step;
        }

        /// <summary>突きの上下。どちらにも倒れていなければ 0 で、上下の振りは出ない</summary>
        public static int ThrustDirection(float vertical)
        {
            if (vertical > Threshold) return 1;
            if (vertical < -Threshold) return -1;

            return 0;
        }

        /// <summary>倒し方から出る行動。facing は Choose と同じ</summary>
        public static AttackMove MoveOf(float horizontal, float vertical, float facing)
        {
            switch (Choose(horizontal, vertical, facing))
            {
                case AttackKind.Thrust:
                    return ThrustDirection(vertical) != 0 ? AttackMove.DiagonalThrust : AttackMove.Thrust;
                case AttackKind.Slash:
                    return AttackMove.Slash;
                case AttackKind.Roll:
                    return AttackMove.Roll;
                default:
                    return AttackMove.Step;
            }
        }
    }

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

    /// <summary>
    /// 回避攻撃。体を縦の軸で 1 回転させながら手前か奥へ抜ける。回り始めから少しのあいだ当たらず（<see cref="Invulnerability.DodgeSeconds"/>）、
    /// 回っているあいだは、剣を振ったときより短い、体のまわりの範囲に当てる
    /// </summary>
    public static class RollRule
    {
        /// <summary>全体の秒数。これが過ぎるまで次は出ない</summary>
        public const float Seconds = 0.6f;

        /// <summary>抜ける距離</summary>
        public const float Distance = 1.5f;

        /// <summary>回り始めから当たり始めるまで</summary>
        public const float StrikeFrom = 0.13f;

        /// <summary>当たる長さ</summary>
        public const float StrikeSeconds = 0.33f;

        /// <summary>当たる範囲の、体からの半径。剣の振りが前へ届く距離（ケサで約 0.7、突きで約 0.85）より短い</summary>
        public const float Radius = 0.5f;

        /// <summary>進み具合 0〜1 のときに、抜ける距離のどれだけを進んだか。終わり際ほどゆっくりにする</summary>
        public static float Travelled(float progress)
        {
            float rest = 1f - Math.Max(0f, Math.Min(1f, progress));
            return 1f - rest * rest;
        }
    }

    /// <summary>当てた相手を飛ばす速さ</summary>
    public readonly struct Push
    {
        public Push(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public float X { get; }

        /// <summary>浮かせる上向きの成分</summary>
        public float Y { get; }

        public float Z { get; }
    }

    /// <summary>吹き飛ばし。当てた側から相手へ向かう水平の向きに飛ばし、少し浮かせる</summary>
    public static class KnockbackRule
    {
        /// <summary>浮かせる強さ。水平の強さに対する割合</summary>
        public const float LiftRatio = 1f / 3f;

        /// <summary>これより近いと向きが決まらない</summary>
        private const float MinDistance = 0.0001f;

        /// <summary>
        /// strength の強さで飛ばす速さ。重なっていて向きが決まらないときは、当てた側の向きへ飛ばす。
        /// facing は当てた側の向き。正なら右、負なら左
        /// </summary>
        public static Push Velocity(float fromX, float fromZ, float toX, float toZ, float strength, float facing)
        {
            if (strength <= 0f) return new Push(0f, 0f, 0f);

            float dx = toX - fromX;
            float dz = toZ - fromZ;
            float length = (float)Math.Sqrt(dx * dx + dz * dz);

            if (length < MinDistance)
            {
                dx = facing < 0f ? -1f : 1f;
                dz = 0f;
                length = 1f;
            }

            return new Push(dx / length * strength, strength * LiftRatio, dz / length * strength);
        }
    }
}
