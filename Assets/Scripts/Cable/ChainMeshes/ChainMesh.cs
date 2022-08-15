using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
abstract public class ChainMesh : MonoBehaviour
{
    public enum CMPrimitives
    {
        Point,
        Circle,
        polygon
    }

    public struct ChainJointCache
    {
        public int submeshIndex;
        public int polygonIndex;
    }

    [SerializeField] private bool protectSettings = false;
    public bool ProtectSettings { get { return protectSettings; } }

    [SerializeField] protected bool submesh = false;
    public bool Submesh { get { return submesh; } }

    abstract public CMPrimitives ChainMeshPrimitiveType { get; }
    abstract public bool MeshGenerated { get; }
    abstract public bool Errornous { get; }

    [ContextMenu("Check Errors")]
    abstract public bool PrintErrors();

    [ContextMenu("Correct Errors")]
    abstract public bool CorrectErrors();

    [ContextMenu("Generate Chain Mesh")]
    abstract public void GenerateChainMesh();

    abstract public void GenerateSubMesh(ChainMesh root);

    abstract public void ForceGenerateSubMesh(ChainMesh root);

    [ContextMenu("Remove Chain Mesh")]
    abstract public void RemoveChainMesh();

    abstract public bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious);

    abstract public Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache);

    abstract public void CreateChainCollider(float chainWidth);
}
