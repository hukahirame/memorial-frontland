using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BranchFallSystem : MonoBehaviour
{
    [SerializeField]
    private Vector3 minusrange;
    [SerializeField]
    private Vector3 plusrange;
    [SerializeField]
    private GameObject dropcapsule;
    [SerializeField]
    private Sprite branch;
    void Start()
    {
        Invoke("BranchDrop", Random.Range(10, 20));
    }
    private void BranchDrop()
    {
        Vector3 dropposition = new Vector3(Random.Range(minusrange.x,plusrange.x), 5, Random.Range(minusrange.z,plusrange.z));
        var drop = Instantiate(dropcapsule, dropposition, Quaternion.identity);
        drop.GetComponent<SpriteRenderer>().sprite = branch;
        Invoke("BranchDrop", Random.Range(20, 36));
    }
}
