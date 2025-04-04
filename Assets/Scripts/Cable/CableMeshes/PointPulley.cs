using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PointPulley : CableMeshInterface
{
    Collider2D pulleyCollider = null;
    Rigidbody2D rb2d = null;

    public override Vector2 PulleyCentreGeometrical { get { return this.transform.position; } }

    public override Bounds PulleyBounds => throw new System.NotImplementedException();

    public override Vector2 CenterOfMass 
    { 
        get 
        {
            Rigidbody2D body = PulleyAttachedRigidBody;
            return body != null ? body.worldCenterOfMass : PulleyCentreGeometrical;
        }
    }

    public override Rigidbody2D PulleyAttachedRigidBody
    {
        get
        {
            return pulleyCollider != null ? pulleyCollider.attachedRigidbody : rb2d;
        }
    }

    public override Transform ColliderTransform => throw new System.NotImplementedException();

    public override CMPrimitives CableMeshPrimitiveType { get { return CMPrimitives.Point; } }

    public override float SafeStoredLength => throw new System.NotImplementedException();

    public override bool MeshGenerated { get { return true; } }

    public override bool Errornous { get { return false; } }

    public override float MaxExtent => throw new System.NotImplementedException();

    public override Vector2 FurthestPoint(Vector2 direction)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 GetNextPoint(int i)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 GetPreviousPoint(int i)
    {
        throw new System.NotImplementedException();
    }

    public override int IndexFromPoint(Vector2 point)
    {
        throw new System.NotImplementedException();
    }

    public override float LoopLength(float cableHalfWidth)
    {
        throw new System.NotImplementedException();
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableHalfWidth, out float identity)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableHalfWidth)
    {
        throw new System.NotImplementedException();
    }

    public override float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableHalfWidth, bool useSmallest)
    {
        throw new System.NotImplementedException();
    }

    protected override bool CorrectErrors()
    {
        return false;
    }

    protected override bool PrintErrors()
    {
        return false;
    }

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        throw new System.NotImplementedException();
    }

    protected override void RemoveCableMesh()
    {
        pulleyCollider = null;
        rb2d = null;
    }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<Collider2D>();
        if (pulleyCollider != null) return;
        rb2d = GetComponent<Rigidbody2D>();
    }
}
