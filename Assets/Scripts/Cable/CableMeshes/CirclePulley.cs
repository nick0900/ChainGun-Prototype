using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

[System.Serializable]
public class CirclePulley : CableMeshInterface
{

    [HideInInspector][SerializeField] CircleCollider2D pulleyCollider = null;

    public Vector2 WorldPosition { get { return PulleyCentreGeometrical; } }
    public float Radius { get { return pulleyCollider.radius; } }

    public override CMPrimitives CableMeshPrimitiveType { get { return CMPrimitives.Circle; } }

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

    public override Transform ColliderTransform { get { return pulleyCollider.transform; } }

    public override float SafeStoredLength { get { return 0.0f; } }

    public override Bounds PulleyBounds { get { return pulleyCollider.bounds; } }

    public override Vector2 CenterOfMass { get { return PulleyAttachedRigidBody != null ? PulleyAttachedRigidBody.worldCenterOfMass : PulleyCentreGeometrical; } }

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

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 cableVector = tailPrevious - PulleyCentreGeometrical;
        Vector2 centreVector = headPrevious - PulleyCentreGeometrical;

        return Vector2.SignedAngle(cableVector, centreVector) >= 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out float identity)
    {
        Vector2 output;

        TangentPointCircle(point, PulleyCentreGeometrical, pulleyCollider.radius, orientation, out output, out identity, cableWidth);

        return output;
    }

    public void CircleToCircleTangent(bool thisOrientation, out Vector2 thisTangentOffset, out float thisIdentity, in CirclePulley otherCircle, bool otherOrientation, out Vector2 otherTangentOffset, out float otherIdentity, float cableWidth)
    {
        TangentCircleCircle(PulleyCentreGeometrical, pulleyCollider.radius, thisOrientation, out thisTangentOffset, out thisIdentity, otherCircle.WorldPosition, otherCircle.Radius, otherOrientation, out otherTangentOffset, out otherIdentity, cableWidth, otherCircle);
    }

    void TangentPointCircle(Vector2 P1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2, out float angle, float cableHalfWidth)
    {
        Vector2 d = P2 - P1;

        if (d.magnitude < r2 + cableHalfWidth)
        {
            //print("previously fuck");
            d = (d / d.magnitude) * (r2 + cableHalfWidth);
        }

        float alpha = d.x >= 0 ? Mathf.Asin(d.y / d.magnitude) : Mathf.PI - Mathf.Asin(d.y / d.magnitude);

        float phi = Mathf.Asin((r2 + cableHalfWidth) / d.magnitude);

        alpha = orientation2 ? alpha - Mathf.PI / 2 - phi : alpha + Mathf.PI / 2 + phi;

        tangentOffset2 = (r2 + cableHalfWidth) * new Vector2(Mathf.Cos(alpha), Mathf.Sin(alpha));

        float globalAngle = -Vector2.SignedAngle(tangentOffset2, Vector2.left);
        angle = globalAngle - pulleyCollider.transform.rotation.eulerAngles.z + 180.0f;
        if (angle > 360.0f) angle -= 360.0f;
        else if (angle < 0) angle += 360.0f;
    }

    void TangentCircleCircle(Vector2 P1, float r1, bool orientation1, out Vector2 tangentOffset1, out float angle1, Vector2 P2, float r2, bool orientation2, out Vector2 tangentOffset2, out float angle2, float cableHalfWidth, in CirclePulley pulley2)
    {
        angle1 = 0;
        angle2 = 0;

        Vector2 d = P2 - P1;

        bool sameOrientation = (orientation1 && orientation2) || !(orientation1 || orientation2);

        float r = sameOrientation ? r2 - r1 : r1 + r2 + cableHalfWidth * 2;

        if (d.magnitude <= r)
        {
            //print("previously double fuck");
            d = d.normalized;
            tangentOffset1 = d * r1;
            tangentOffset2 = -d * r2;

            angle1 = -Vector2.SignedAngle(d, Vector2.left) - this.ColliderTransform.rotation.eulerAngles.z + 180.0f;
            angle2 = -Vector2.SignedAngle(-d, Vector2.left) - pulley2.ColliderTransform.rotation.eulerAngles.z + 180.0f;

            if (angle1 > 360.0f) angle1 -= 360.0f;
            else if (angle1 < 0) angle1 += 360.0f;

            if (angle2 > 360.0f) angle2 -= 360.0f;
            else if (angle2 < 0) angle2 += 360.0f;
        }

        if (sameOrientation)
        {
            if (r1 == r2)
            {
                d = Vector2.Perpendicular(d.normalized) * (r1 + cableHalfWidth);

                if (!orientation1)
                {
                    d = -d;
                }

                tangentOffset1 = d;
                tangentOffset2 = d;

                float globalAngle = -Vector2.SignedAngle(d, Vector2.left);
                angle1 = globalAngle - this.ColliderTransform.rotation.eulerAngles.z + 180.0f;
                angle2 = globalAngle - pulley2.ColliderTransform.rotation.eulerAngles.z + 180.0f;

                if (angle1 > 360.0f) angle1 -= 360.0f;
                else if (angle1 < 0) angle1 += 360.0f;

                if (angle2 > 360.0f) angle2 -= 360.0f;
                else if (angle2 < 0) angle2 += 360.0f;
            }
            else
            {
                Vector2 tangentIntersection = (P2 * (r1 + cableHalfWidth) - P1 * (r2 + cableHalfWidth)) / (r1 - r2);

                TangentPointCircle(tangentIntersection, P1, r1, !orientation1, out tangentOffset1, out angle1, cableHalfWidth);
                TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2, out angle2, cableHalfWidth);
            }
        }
        else
        {
            Vector2 tangentIntersection = (P2 * (r1 + cableHalfWidth) + P1 * (r2 + cableHalfWidth)) / (r1 + r2 + cableHalfWidth * 2);

            TangentPointCircle(tangentIntersection, P1, r1, orientation1, out tangentOffset1, out angle1, cableHalfWidth);
            TangentPointCircle(tangentIntersection, P2, r2, !orientation2, out tangentOffset2, out angle2, cableHalfWidth);
        }
    }

    public override float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableHalfWidth, bool useSmallest)
    {
        if (!orientation)
        {
            float aux = prevIdentity;
            prevIdentity = currIdentity;
            currIdentity = aux;
        }

        float angle1 = currIdentity - prevIdentity;
        bool sign = angle1 < 0;
        angle1 = Mathf.Abs(angle1);
        float angle2 = 360.0f - angle1;

        if (useSmallest)
        {
            if (angle1 > angle2)
            {
                angle1 = angle2;
            }
            // needed for edge case where the smallest angle crosses over the angle boundaries
            if (sign)
            {
                if (((currIdentity - angle1) <= 0.0f) && ((prevIdentity + angle1) >= 360.0f))
                    sign = false;
            }
            else
            {
                if (((prevIdentity - angle1) <= 0.0f) && ((currIdentity + angle1) >= 360.0f))
                    sign = true;
            }

            return (pulleyCollider.radius + cableHalfWidth) * angle1 * Mathf.Deg2Rad * (sign ? -1.0f : 1.0f);
        }

        return (pulleyCollider.radius + cableHalfWidth) * (sign ? angle2 : angle1) * Mathf.Deg2Rad;
    }

    public override Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableHalfWidth)
    {
        float angle = Random.Range(0.0f, 360.0f);
        Vector2 tangent = (Radius + cableHalfWidth) * new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        pointIdentity = (angle - pulleyCollider.transform.rotation.eulerAngles.z + 180.0f) % 360;
        if (pointIdentity < 0) pointIdentity += 360.0f;

        return tangent;
    }

    protected override float ShapeFrictionFactor(float slipSign, bool slipping, float storedCable, float cableWidth)
    {
        float wrapAngle = storedCable / (Radius + cableWidth / 2);
        return Mathf.Exp(slipSign * (slipping ? kineticFrictionCoeff : staticFrictionCoeff) * wrapAngle);
    }

    public override Vector2 FurthestPoint(Vector2 direction)
    {
        return WorldPosition + direction * Radius;
    }

    public override int IndexFromPoint(Vector2 point)
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
}
