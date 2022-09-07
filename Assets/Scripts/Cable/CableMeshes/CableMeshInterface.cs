using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
abstract public class CableMeshInterface : CableMeshGeneration
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

    abstract public CMPrimitives ChainMeshPrimitiveType { get; }

    abstract public bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious);

    abstract public Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache);

    abstract public void CreateChainCollider(float chainWidth);

    protected Vector2 CenterWorldPosition(Collider2D colider)
    {
        return (Vector2)this.transform.TransformPoint(colider.offset);
    }
}
