using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.ProBuilder.MeshOperations;
using UnityEngine.Rendering;

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
        if (other.name.IndexOf("Cube") != -1 && Fades(other))
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
        if (other.name.IndexOf("Cube") != -1 && Fades(other))
        {
            var target = other.GetComponent<Renderer>().material;
            var newcolor = target.color;
            newcolor.a = 1;
            target.color = newcolor;
        }
    }

    /// <summary>
    /// 透ける素材か。不透明の素材は色の透明度を変えても見た目が変わらないので触らない。
    /// 触ると素材が物ごとに複製され、まとめて描けなくなる（static batching が崩れる）
    /// </summary>
    private static bool Fades(Collider other)
    {
        Renderer shown = other.GetComponent<Renderer>();
        return shown != null && shown.sharedMaterial != null && shown.sharedMaterial.renderQueue >= (int)RenderQueue.AlphaTest;
    }
}
