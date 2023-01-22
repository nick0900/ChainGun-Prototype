using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

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
#if UNITY_EDITOR
        Undo.RecordObject(this, "Correcting cablemesh " + gameObject.name);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        return CorrectErrors();
    }
    abstract protected bool CorrectErrors();

    [ContextMenu("Generate Chain Mesh")]
    public void GenerateChainMesh()
    {
        if (submesh || protectSettings) return;
#if UNITY_EDITOR
        Undo.RecordObject(this, "Generated cablemesh " + gameObject.name);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        SetupMesh();
        PrintErrors();
    }

    public void GenerateSubMesh(CableMeshInterface root)
    {
        if (!submesh || protectSettings) return;
#if UNITY_EDITOR
        Undo.RecordObject(this, "Generated cablemesh " + gameObject.name);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
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
#if UNITY_EDITOR
        Undo.RecordObject(this, "Destroyed cablemesh " + gameObject.name);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
        RemoveCableMesh();
    }
    abstract protected void RemoveCableMesh();
}
