using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class CableMeshGeneration : MonoBehaviour
{
    [SerializeField] private bool protectSettings = false;
    public bool ProtectSettings { get { return protectSettings; } }

    [SerializeField] private bool submesh = false;
    public bool Submesh { get { return submesh; } }

    abstract public bool MeshGenerated { get; }
    abstract public bool Errornous { get; }

    [ContextMenu("Check Errors")]
    public bool PrintErrorMenu()
    {
        return PrintErrors();
    }
    abstract protected bool PrintErrors();

    [ContextMenu("Correct Errors")]
    public bool CorrectErrorMenu()
    {
        return CorrectErrors();
    }
    abstract protected bool CorrectErrors();

    [ContextMenu("Generate Chain Mesh")]
    public void GenerateChainMesh()
    {
        if (submesh || protectSettings) return;
        SetupMesh();
        PrintErrors();
    }

    public void GenerateSubMesh(CableMeshInterface root)
    {
        if (!submesh || protectSettings) return;
        SetupMesh();
        PrintErrors();
    }

    public void ForceGenerateSubMesh(CableMeshInterface root)
    {
        if (protectSettings) return;
        submesh = true;
        GenerateSubMesh(root);
    }

    abstract protected void SetupMesh();

    [ContextMenu("Remove Chain Mesh")]
    public void RemoveCableMeshMenu()
    {
        RemoveCableMesh();
    }
    abstract protected void RemoveCableMesh();
}
