using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using MemorialFloor.Game;
using UnityEngine;

public class SpawnerCandidate : MonoBehaviour
{
    /// <summary>体力のゲージの色</summary>
    private static readonly Color GaugeColor = new Color(0.9f, 0.3f, 0.3f);

    [SerializeField] private int maxHp = 100;

    [Tooltip("核になったとき、湧く地面をこの高さから真下へ探す。2 階建ての場所では、候補の立つ階の天井より低くする")]
    [SerializeField] private float seekFrom = Seeker.DefaultFrom;

    [Tooltip("湧く地面を探す深さ。2 階建ての上の階では、床の穴から下の階に湧かないよう、下の階に届かない深さにする")]
    [SerializeField] private float seekDepth = Seeker.DefaultDepth;

    private readonly Health health = new Health();

    /// <summary>残りの体力を、絵のてっぺんの上に見せる</summary>
    private WorldGauge gauge;

    /// <summary>核になったとき、湧く地面を探し始める高さ</summary>
    public float SeekFrom
    {
        get { return seekFrom; }
    }

    /// <summary>核になったとき、湧く地面を探す深さ</summary>
    public float SeekDepth
    {
        get { return seekDepth; }
    }

    void Awake()
    {
        health.SetMax(maxHp);
        health.SetCurrent(maxHp);
        gauge = WorldGauge.Put(transform, GaugeColor);
    }

    public void Candidate(int damage)
    {
        health.Take(damage);
        gauge.Show((float)health.Current / health.Max, GaugeColor);

        if (health.IsDead)
        {
            gameObject.SetActive(false);
        }
    }

}
