using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FieldCreator : MonoBehaviour // OutField用
{
    // シード値を使うなら、フィールドの動的変化にあわせて値もその時に変化させる

    public struct FirstSeedSet
    {
        public string seed;
        public Vector3 spownerpos;
        public Vector2 uipos;
    }

    public FirstSeedSet SeedCreate()
    {
        FirstSeedSet fss = new FirstSeedSet();
        fss.seed = null;
        fss.seed = string.Empty;


        return fss;
    }

}
