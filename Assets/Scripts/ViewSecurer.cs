using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;

public class ViewSecurer : MonoBehaviour
{
    private Color target;
    [SerializeField] private float alpha;

    private void OnTriggerEnter(Collider other)
    {
        if (other.name.IndexOf("tree") != -1)
        {
            var target = other.GetComponent<SpriteRenderer>();
            var newcolor = target.color;
            newcolor.a = alpha;
            target.color = newcolor;
        }
        if (other.name.IndexOf("Cube") != -1)
        {
            var target = other.GetComponent<Renderer>().material;
            var newcolor = target.color;
            newcolor.a = alpha;
            target.color = newcolor;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.name.IndexOf("tree") != -1)
        {
            var target = other.GetComponent<SpriteRenderer>();
            var newcolor = target.color;
            newcolor.a = 1;
            target.color = newcolor;
        }
        if (other.name.IndexOf("Cube") != -1)
        {
            var target = other.GetComponent<Renderer>().material;
            var newcolor = target.color;
            newcolor.a = 1;
            target.color = newcolor;
        }
    }



}
