using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMDictionary : MonoBehaviour
{
    public static CMDictionary CMD;

    public Dictionary<Collider2D, ChainMesh> dictionary;
    
    void Awake()
    {
        if (CMD != null)
            GameObject.Destroy(CMD);
        else
            CMD = this;

        DontDestroyOnLoad(this);
    }
}
