using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CirclePulley : CableMeshInterface
{

    [SerializeField] CircleCollider2D pulleyCollider = null;

    public Vector2 WorldPosition { get { return PulleyCentreGeometrical; } }
    public float Radius { get { return pulleyCollider.radius; } }

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override bool MeshGenerated { get { return pulleyCollider != null; } }

    public override bool Errornous
    {
        get
        {
            return !MeshGenerated;
        }
    }

    public override Vector2 PulleyCentreGeometrical 
    {
        get 
        {
            return pulleyCollider.transform.TransformPoint(pulleyCollider.offset);
        }
    }

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        return pulleyCollider.transform.TransformPoint(point + pulleyCollider.offset);
    }

    public override Rigidbody2D PulleyAttachedRigidBody
    {
        get
        {
            return pulleyCollider.attachedRigidbody;
        }
    }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<CircleCollider2D>();
    }
    protected override void RemoveCableMesh()
    {
        pulleyCollider = null;
    }

    protected override bool PrintErrors()
    {
        bool error = false;

        if (!MeshGenerated)
        {
            error = true;
            print(this + "/Error: mesh not generated");
        }
        return error;
    }

    protected override bool CorrectErrors()
    {
        bool errorsFixed = true;

        if (!MeshGenerated)
        {
            errorsFixed = false;
            print(this + "/Error: mesh not generated");
            print(this + "/FixFailed: you need to manually generate this or all cablemeshes");
        }
        return errorsFixed;
    }

    private void Awake()
    {
        previousPosition = PulleyCentreGeometrical;
    }
    private void FixedUpdate()
    {
        if (pulleyCollider.attachedRigidbody.bodyType != RigidbodyType2D.Static)
        {
            previousPosition = PulleyCentreGeometrical;
        }
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 cableVector = headPrevious - tailPrevious;
        Vector2 centreVector = previousPosition - tailPrevious;

        return Vector2.SignedAngle(cableVector, centreVector) >= 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out int vertex)
    {
        Vector2 output;

        TangentPointCircle(point, PulleyCentreGeometrical, pulleyCollider.radius, orientation, out output, cableWidth);

        vertex = -1;

        return output;
    }

    public void CircleToCircleTangent(bool thisOrientation, out Vector2 thisTangentOffset, in CirclePulley otherCircle, bool otherOrientation, out Vector2 otherTangentOffset, float cableWidth)
    {
        TangentCircleCircle(PulleyCentreGeometrical, pulleyCollider.radius, thisOrientation, out thisTangentOffset, otherCircle.WorldPosition, otherCircle.Radius, otherOrientation, out otherTangentOffset, cableWidth);
    }

    void TangentPointCircle(Vector2 P1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2, float cableWidth)
    {
        Vector2 d = P2 - P1;

        if (d.magnitude <= r2 + cableWidth / 2)
        {
            print("fuck");
            throw new System.Exception();
        }

        float alpha = d.x >= 0 ? Mathf.Asin(d.y / d.magnitude) : Mathf.PI - Mathf.Asin(d.y / d.magnitude);

        float phi = Mathf.Asin((r2 + cableWidth / 2) / d.magnitude);

        alpha = orientation2 ? alpha - Mathf.PI / 2 - phi : alpha + Mathf.PI / 2 + phi;

        tangentOffset2 = r2 * new Vector2(Mathf.Cos(alpha), Mathf.Sin(alpha));
    }

    void TangentCircleCircle(Vector2 P1, float r1, bool orientation1, out Vector2 tangentOffset1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2, float cableWidth)
    {
        Vector2 d = P2 - P1;

        bool sameOrientation = (orientation1 && orientation2) || !(orientation1 || orientation2);

        float r = sameOrientation ? r2 - r1 : r1 + r2 + cableWidth;

        if (d.magnitude <= r)
        {
            print("double fuck");
            throw new System.Exception();
        }

        if (sameOrientation)
        {
            if (r1 == r2)
            {
                d = Vector2.Perpendicular(d.normalized) * r1;

                if (!orientation1)
                {
                    d = -d;
                }

                tangentOffset1 = d;
                tangentOffset2 = d;
            }
            else
            {
                Vector2 tangentIntersection = (P2 * (r1 + cableWidth / 2) - P1 * (r2 + cableWidth / 2)) / (r1 - r2);

                TangentPointCircle(tangentIntersection, P1, r1, !orientation1, out tangentOffset1, cableWidth);
                TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2, cableWidth);
            }
        }
        else
        {
            Vector2 tangentIntersection = (P2 * (r1 + cableWidth / 2) + P1 * (r2 + cableWidth / 2)) / (r1 + r2 + cableWidth);

            TangentPointCircle(tangentIntersection, P1, r1, orientation1, out tangentOffset1, cableWidth);
            TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2, cableWidth);
        }
    }

    public override float ShapeSurfaceDistance(Vector2 prevTangent, int prevVertex, Vector2 currentTangent, int currentVertex, bool orientation)
    {
        if (!orientation)
        {
            Vector2 aux = prevTangent;
            prevTangent = currentTangent;
            currentTangent = aux;
        }

        float degrees = Vector2.SignedAngle(prevTangent, currentTangent);

        return pulleyCollider.radius * degrees * Mathf.Deg2Rad;
    }

    public override void CreateChainCollider(float cableWidth)
    {
        throw new System.NotImplementedException();
    }
}
