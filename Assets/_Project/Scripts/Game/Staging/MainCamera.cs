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

    /// <summary>
    /// プレイヤーが 1 フレームでこれより動いたら、歩いたのではなく移されたとみなし、追わずに飛ぶ。
    /// 復活の地点のように近くへ移されたときも、明転の途中で画面が流れないように
    /// </summary>
    public const float SnapDistance = 1f;

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
    private Vector3 _lastTarget;

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
        _lastTarget = _target.position;
    }

    private void LateUpdate()
    {
        Vector3 target = _target.position;
        Vector3 goal = target + _offset;

        if ((target - _lastTarget).sqrMagnitude > SnapDistance * SnapDistance)
        {
            _position = goal;
            _velocity = Vector3.zero;
        }
        else if (Time.deltaTime > 0f) //時間止めの間は追わない。経過 0 の SmoothDamp は、追い付き切った軸の速さを 0 割りで NaN にする
        {
            float dt = Time.deltaTime;
            _position.x = Mathf.SmoothDamp(_position.x, goal.x, ref _velocity.x, FollowSeconds, Mathf.Infinity, dt);
            _position.y = Mathf.SmoothDamp(_position.y, goal.y, ref _velocity.y, VerticalFollowSeconds, Mathf.Infinity, dt);
            _position.z = Mathf.SmoothDamp(_position.z, goal.z, ref _velocity.z, FollowSeconds, Mathf.Infinity, dt);
        }

        transform.position = _position + ShakeOffset();
        _lastTarget = target;
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
