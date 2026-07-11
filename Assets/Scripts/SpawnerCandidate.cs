using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnerCandidate : MonoBehaviour
{
    private int hp = 100;
    public void Candidate(int damage)
    {
        hp -= damage;

        if(hp <= 0)
        {
            gameObject.SetActive(false);
        }
    }

}
