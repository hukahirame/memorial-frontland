using UnityEngine;

namespace MemorialFloor.Game
{
    /// <summary>
    /// 回避攻撃の見た目。ためて縮み、伸びて小さく跳ねながら縦の軸で 1 回転し、降りて縮んでから戻る。
    /// 回っているあいだは残像を残す。縮み伸びは足もとを地面に付けたまま行う。抜ける距離・無敵・当たりは持たない
    /// </summary>
    public sealed class RollStaging
    {
        /// <summary>ためる長さ。全体に対する割合。このあいだは回さずに縮む</summary>
        public const float WindupRatio = 0.12f;

        /// <summary>降りて縮む長さ。全体に対する割合</summary>
        public const float LandRatio = 0.15f;

        /// <summary>跳ねる高さ</summary>
        public const float HopHeight = 0.12f;

        /// <summary>ためと着地で縮む割合。横に広がり、縦に縮む</summary>
        public const float Squash = 0.12f;

        /// <summary>跳び出しで伸びる割合。縦に伸び、横に細る</summary>
        public const float Stretch = 0.1f;

        /// <summary>残像を残す間隔（秒）</summary>
        public const float AfterimageEvery = 0.05f;

        /// <summary>残像が消えきるまで（秒）</summary>
        public const float AfterimageSeconds = 0.22f;

        /// <summary>残像の色。青みがかった半透明</summary>
        public static readonly Color AfterimageTint = new Color(0.7f, 0.88f, 1f, 0.5f);

        private readonly Transform _body;
        private readonly Transform _root;
        private readonly float _turn;
        private readonly Vector3 _home;
        private readonly Vector3 _size;
        private readonly Quaternion _rest;

        /// <summary>body の原点から足もとまでの高さ。親の座標で測る</summary>
        private readonly float _feet;

        /// <summary>跳ねる高さを親の座標に直したもの</summary>
        private readonly float _hop;

        private float _nextAfterimage;

        /// <summary>body を回す。root はプレイヤーの一番上、turn は回す角度（度）、feetY は足もとの高さ</summary>
        public RollStaging(Transform body, Transform root, float turn, float feetY)
        {
            _body = body;
            _root = root;
            _turn = turn;
            _home = body.localPosition;
            _size = body.localScale;
            _rest = body.localRotation;

            Transform parent = body.parent;
            _feet = parent.InverseTransformPoint(new Vector3(body.position.x, feetY, body.position.z)).y - _home.y;
            _hop = HopHeight / parent.lossyScale.y;
        }

        /// <summary>進み具合 0〜1 の姿にする</summary>
        public void Pose(float progress)
        {
            float spinTo = 1f - LandRatio;
            float spin = Mathf.InverseLerp(WindupRatio, spinTo, progress);

            //正なら横に広がって縦に縮み、負なら縦に伸びる
            float squash;
            if (progress < WindupRatio) squash = Squash * Mathf.SmoothStep(0f, 1f, progress / WindupRatio);
            else if (progress < spinTo) squash = -Stretch * (1f - Mathf.SmoothStep(0f, 1f, spin * 2f));
            else squash = Squash * Mathf.Sin(Mathf.InverseLerp(spinTo, 1f, progress) * Mathf.PI);

            float tall = 1f - squash;
            _body.localScale = Vector3.Scale(_size, new Vector3(1f + squash, tall, 1f));
            _body.localPosition = _home + Vector3.up * (_feet * (1f - tall) + 4f * spin * (1f - spin) * _hop);
            _body.localRotation = _rest * Quaternion.Euler(0f, _turn * Mathf.SmoothStep(0f, 1f, spin), 0f);

            if (progress > WindupRatio && progress < spinTo && Time.time >= _nextAfterimage)
            {
                Afterimage.Leave(_body, _root, AfterimageTint, AfterimageSeconds);
                _nextAfterimage = Time.time + AfterimageEvery;
            }
        }

        /// <summary>元の姿に戻す</summary>
        public void Restore()
        {
            _body.localPosition = _home;
            _body.localScale = _size;
            _body.localRotation = _rest;
        }
    }
}
