using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CableSegment : CableMeshInterface
{
    public Transform endpoint;
    public float width = 0.1f;

    public override Vector2 PulleyCentreGeometrical
    {
        get
        {
            return this.transform.position + 0.5f * (endpoint.transform.position - this.transform.position);
        }
    }

    public override Rigidbody2D PulleyAttachedRigidBody => throw new System.NotImplementedException();

    public override Transform ColliderTransform => throw new System.NotImplementedException();

    public override CMPrimitives CableMeshPrimitiveType => throw new System.NotImplementedException();

    public override float SafeStoredLength => throw new System.NotImplementedException();

    public override bool MeshGenerated => throw new System.NotImplementedException();

    public override bool Errornous => throw new System.NotImplementedException();

    public override Vector2 FurthestPoint(Vector2 direction)
    {
        Vector2 point1 = this.transform.position;
        Vector2 point2 = endpoint.transform.position;
        if (Vector2.Dot(point1, direction) > Vector2.Dot(point2, direction))
            return point1;
        return point2;
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

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out float identity)
    {
        throw new System.NotImplementedException();
    }

    public override Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableWidth)
    {
        throw new System.NotImplementedException();
    }

    public override float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableWidth, bool useSmallest)
    {
        throw new System.NotImplementedException();
    }

    protected override bool CorrectErrors()
    {
        throw new System.NotImplementedException();
    }

    protected override bool PrintErrors()
    {
        throw new System.NotImplementedException();
    }

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        throw new System.NotImplementedException();
    }

    protected override void RemoveCableMesh()
    {
        throw new System.NotImplementedException();
    }

    protected override void SetupMesh()
    {
        throw new System.NotImplementedException();
    }

    private void Update()
    {
        LineRenderer line = GetComponent<LineRenderer>();

        line.startWidth = width;
        line.endWidth = width;
        line.positionCount = 2;

        line.SetPosition(0, this.transform.position);
        line.SetPosition(1, endpoint.transform.position);
    }
}
