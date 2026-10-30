using UnityEngine;

/// <summary>
/// プレイヤーの子に置き、親の動きをそのまま写さずに滑らかに追う。
/// 親の位置は物理の刻み（50Hz）で段になるうえ、跳ぶたびに上下するため
/// </summary>
public class MainCamera : MonoBehaviour
{
    /// <summary>横と奥行きを追い付くまでのおおよその秒数</summary>
    public const float FollowSeconds = 0.12f;

    /// <summary>上下を追い付くまでのおおよその秒数。跳ぶたびに画面が上下しないよう遅らせる</summary>
    public const float VerticalFollowSeconds = 0.4f;

    /// <summary>1 フレームでこれより離れたら、場所の移動とみなして追わずに飛ぶ</summary>
    public const float SnapDistance = 4f;

    /// <summary>当てたときの揺れの大きさ</summary>
    public const float HitShake = 0.04f;

    /// <summary>倒したときの揺れの大きさ</summary>
    public const float KillShake = 0.08f;

    /// <summary>揺れが収まるまでの秒数</summary>
    public const float ShakeSeconds = 0.15f;

    private Transform _target;
    private Vector3 _offset;
    private Vector3 _position;
    private Vector3 _velocity;

    private float _shakeStrength;
    private float _shakeEndsAt;

    private void Awake()
    {
        _target = transform.parent;
        if (_target == null)
        {
            enabled = false;
            return;
        }

        _offset = transform.position - _target.position;
        _position = transform.position;
    }

    private void LateUpdate()
    {
        Vector3 goal = _target.position + _offset;

        if ((goal - _position).sqrMagnitude > SnapDistance * SnapDistance)
        {
            _position = goal;
            _velocity = Vector3.zero;
        }
        else
        {
            float dt = Time.deltaTime;
            _position.x = Mathf.SmoothDamp(_position.x, goal.x, ref _velocity.x, FollowSeconds, Mathf.Infinity, dt);
            _position.y = Mathf.SmoothDamp(_position.y, goal.y, ref _velocity.y, VerticalFollowSeconds, Mathf.Infinity, dt);
            _position.z = Mathf.SmoothDamp(_position.z, goal.z, ref _velocity.z, FollowSeconds, Mathf.Infinity, dt);
        }

        transform.position = _position + ShakeOffset();
    }

    /// <summary>画面を揺らす。揺れている最中に弱いものが来ても、強い方を残す</summary>
    public void Shake(float strength)
    {
        if (Time.unscaledTime < _shakeEndsAt && strength < _shakeStrength) return;

        _shakeStrength = strength;
        _shakeEndsAt = Time.unscaledTime + ShakeSeconds;
    }

    /// <summary>止めた時間の中でも揺れるよう、実時間で減らす</summary>
    private Vector3 ShakeOffset()
    {
        float left = _shakeEndsAt - Time.unscaledTime;
        if (left <= 0f) return Vector3.zero;

        return (Vector3)(Random.insideUnitCircle * (_shakeStrength * left / ShakeSeconds));
    }
}
