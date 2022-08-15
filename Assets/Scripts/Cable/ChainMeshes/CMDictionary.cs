using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CMDictionary : MonoBehaviour
{
    public static CMDictionary CMD;

    [SerializeField] Dictionary<Collider2D, ChainMesh> dictionary;
    
    void Awake()
    {
        if (CMD != null)
            GameObject.Destroy(CMD);
        else
            CMD = this;

        DontDestroyOnLoad(this);
    }

    [ContextMenu("GenerateAllCM")]
    public void GenerateAllCM()
    {
        int count = 0;
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (!mesh.MeshGenerated && !mesh.Submesh && !mesh.ProtectSettings)
            {
                mesh.GenerateChainMesh();
                count++;
            }
        }

        print(count + " chain meshes generated");
    }

    [ContextMenu("RegenerateAllCM")]
    public void RegenreateAllCM()
    {
        int count = 0;
        dictionary.Clear();
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (!mesh.Submesh && !mesh.ProtectSettings)
            {
                mesh.GenerateChainMesh();
                count++;
            }
        }
        print(count + " chain meshes generated");
    }

    [ContextMenu("CheckErrors")]
    public void CheckErrors()
    {
        int count = 0;
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (mesh.PrintErrors())
            {
                count++;
            }
        }
        print(count + " errors detected");
    }

    [ContextMenu("CorrectErroneous")]
    public void CorrectErroneous()
    {
        int errors = 0;
        int fixedErrors = 0;
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (mesh.Errornous && !mesh.ProtectSettings)
            {
                if (mesh.CorrectErrors())
                {
                    fixedErrors++;
                }
                errors++;
            }
        }
        print(fixedErrors + "/" + errors + " errors fixed");
    }

    [ContextMenu("CleanErroneous")]
    public void CleanErroneous()
    {
        int count = 0;
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (mesh.Errornous && !mesh.ProtectSettings)
            {
                mesh.RemoveChainMesh();
                count++;
            }
        }
        print(count + " chain meshes removed");
    }

    [ContextMenu("ClearAllCM")]
    public void ClearAllCM()
    {
        ChainMesh[] meshes = Resources.FindObjectsOfTypeAll<ChainMesh>();

        foreach (ChainMesh mesh in meshes)
        {
            if (!mesh.ProtectSettings)
            {
                mesh.RemoveChainMesh();
            }
        }

        dictionary.Clear();
    }

    public void Register(in Collider2D collider, in ChainMesh chainMesh)
    {
        if (dictionary.ContainsKey(collider))
        {
            if (dictionary[collider] == chainMesh)
            {
                return;
            }
            
            dictionary.Remove(collider);
        }

        dictionary.Add(collider, chainMesh);
    }

    public void Unregister(in Collider2D collider)
    {
        if (dictionary.ContainsKey(collider))
        {
            dictionary.Remove(collider);
        }
    }

    public bool IsRegistered(in Collider2D collider)
    {
        return dictionary.ContainsKey(collider);
    }

    public bool IsRegistered(in ChainMesh chainMesh)
    {
        return dictionary.ContainsValue(chainMesh);
    }

    public bool IsRegistered(in Collider2D collider ,in ChainMesh chainMesh)
    {
        if (dictionary.ContainsKey(collider))
        {
            return dictionary[collider] == chainMesh;
        }

        return false;
    }
}
