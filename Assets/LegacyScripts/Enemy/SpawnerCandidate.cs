using System.Collections;
using System.Collections.Generic;
using MemorialFloor.Domain;
using UnityEngine;

public class SpawnerCandidate : MonoBehaviour
{
    [SerializeField] private int maxHp = 100;

    private readonly Health health = new Health();

    void Awake()
    {
        health.SetMax(maxHp);
        health.SetCurrent(maxHp);
    }

    public void Candidate(int damage)
    {
        health.Take(damage);

        if (health.IsDead)
        {
            gameObject.SetActive(false);
        }
    }

}
