using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
public class CableEditorMenu : MonoBehaviour
{

    [MenuItem("Cablesystem/GenerateAllCM")]
    static public void GenerateAllCM()
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
    static public void RegenreateAllCM()
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
    static public void CheckErrors()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (mesh.PrintErrorMenu())
            {
                count++;
            }
        }
        print(count + " errors detected");
    }

    [MenuItem("Cablesystem/CorrectErroneous")]
    static public void CorrectErroneous()
    {
        int errors = 0;
        int fixedErrors = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (mesh.Errornous && !mesh.ProtectSettings)
            {
                if (mesh.CorrectErrorMenu())
                {
                    fixedErrors++;
                }
                errors++;
            }
        }
        print(fixedErrors + "/" + errors + " errors fixed");
    }

    [MenuItem("Cablesystem/RemoveErroneous")]
    static public void CleanErroneous()
    {
        int count = 0;
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (mesh.Errornous && !mesh.ProtectSettings)
            {
                mesh.RemoveCableMeshMenu();
                count++;
            }
        }
        print(count + " chain meshes removed");
    }

    [MenuItem("Cablesystem/RemoveAllCM")]
    static public void ClearAllCM()
    {
        CableMeshGeneration[] meshes = Resources.FindObjectsOfTypeAll<CableMeshGeneration>();

        foreach (CableMeshGeneration mesh in meshes)
        {
            if (!mesh.ProtectSettings)
            {
                mesh.RemoveCableMeshMenu();
            }
        }
    }
}
#endif
