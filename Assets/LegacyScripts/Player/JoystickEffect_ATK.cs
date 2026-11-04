using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Game;
using UnityEngine;

public class JoystickEffect_ATK : MonoBehaviour
{
    [SerializeField] FixedJoystick joystick;

    [SerializeField] private GameObject up;
    [SerializeField] private GameObject down;
    [SerializeField] private GameObject left;
    [SerializeField] private GameObject right;

    private Player2 player;
    private AttackGuide guide;

    void Start()
    {
        //攻撃のキーは右下、向きのキーは左上に分けて置く。← と → を隣に並べると、1 本の長い矢印に見える。
        //下の辺に 2 つ並べると、スティックの幅に収まらず重なる
        KeyHint.Put((RectTransform)transform, Player2.AttackKeyName);
        KeyHint.Put((RectTransform)transform, "←↑↓→", Vector2.up);
        guide = AttackGuide.Put((RectTransform)transform, Player2.AttackKeyName);
    }

    /// <summary>攻撃の案内に、出ている行動と構えている行動を出すプレイヤーを受け取る</summary>
    public void Bind(Player2 owner)
    {
        player = owner;
    }

    void Update()
    {
        //矢印キーで向けているあいだも出す
        Vector2 aim = Player2.AttackAim(joystick);
        up.SetActive(aim.y > 0.5f);
        down.SetActive(aim.y < -0.5f);
        right.SetActive(aim.x > 0.5f);
        left.SetActive(aim.x < -0.5f);

        if (player != null) guide.Show(player.Doing, player.Aimed);
    }
}
