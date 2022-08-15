using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class CableEditorMenu : MonoBehaviour
{

    [MenuItem("Cablesystem/GenerateAllCM")]
    public void GenerateAllCM()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (!mesh.MeshGenerated && !mesh.Submesh && !mesh.ProtectSettings)
            {
                mesh.GenerateChainMesh();
                count++;
            }
        }

        print(count + " chain meshes generated");
    }

    [MenuItem("Cablesystem/RegenerateAllCM")]
    public void RegenreateAllCM()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (!mesh.Submesh && !mesh.ProtectSettings)
            {
                mesh.GenerateChainMesh();
                count++;
            }
        }
        print(count + " chain meshes generated");
    }

    [MenuItem("Cablesystem/CheckErrors")]
    public void CheckErrors()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (mesh.PrintErrors())
            {
                count++;
            }
        }
        print(count + " errors detected");
    }

    [MenuItem("Cablesystem/Erroneous")]
    public void CorrectErroneous()
    {
        int errors = 0;
        int fixedErrors = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
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

    [MenuItem("Cablesystem/Erroneous")]
    public void CleanErroneous()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (mesh.Errornous && !mesh.ProtectSettings)
            {
                mesh.RemoveChainMesh();
                count++;
            }
        }
        print(count + " chain meshes removed");
    }

    [MenuItem("Cablesystem/AllCM")]
    public void ClearAllCM()
    {
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (!mesh.ProtectSettings)
            {
                mesh.RemoveChainMesh();
            }
        }
    }
}
#endif
