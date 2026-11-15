using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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
    //落ちる範囲は場所の原点からの位置。場所は世界のどこかへずらして置いてある
    private void BranchDrop()
    {
        MemorialFloor.Game.Area area = MemorialFloor.Game.Area.In(gameObject.scene);
        Vector3 origin = area != null ? area.Origin : Vector3.zero;
        Vector3 dropposition = origin + new Vector3(Random.Range(minusrange.x,plusrange.x), 5, Random.Range(minusrange.z,plusrange.z));
        var drop = Instantiate(dropcapsule, dropposition, Quaternion.identity);
        SceneManager.MoveGameObjectToScene(drop, gameObject.scene);
        drop.GetComponent<SpriteRenderer>().sprite = branch;
        Invoke("BranchDrop", Random.Range(20, 36));
    }
}
