using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class BoxPulley : CableMeshInterface
{
    [HideInInspector][SerializeField] BoxCollider2D pulleyCollider = null;
    [HideInInspector][SerializeField] float minSide = 0;

    public override CMPrimitives CableMeshPrimitiveType { get { return CMPrimitives.Box; } }

    public override bool MeshGenerated { get { return pulleyCollider != null; } }

    public override bool Errornous { get { return !MeshGenerated; } }

    public override Vector2 PulleyCentreGeometrical
    {
        get
        {
            return pulleyCollider.transform.TransformPoint(pulleyCollider.offset);
        }
    }

    public override Rigidbody2D PulleyAttachedRigidBody
    {
        get
        {
            return pulleyCollider.attachedRigidbody;
        }
    }

    public override float SafeStoredLength { get { return minSide; } }

    public override Transform ColliderTransform { get { return pulleyCollider.transform; } }

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        return pulleyCollider.transform.TransformPoint(point + pulleyCollider.offset);
    }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<BoxCollider2D>();
        if (pulleyCollider != null)
        {
            if (pulleyCollider.size.x < pulleyCollider.size.y)
            {
                minSide = pulleyCollider.size.x;
            }
            else
            {
                minSide = pulleyCollider.size.y;
            }
        }
    }

    protected override void RemoveCableMesh()
    {
        pulleyCollider = null;
        minSide = 0;
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
        Vector2 cableVector = headPrevious - tailPrevious;
        Vector2 centreVector = previousPosition - tailPrevious;

        return Vector2.SignedAngle(cableVector, centreVector) >= 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out float identity)
    {
        Vector2 topRight = this.transform.TransformPoint(new Vector2((pulleyCollider.size.x + cableWidth) / 2, (pulleyCollider.size.y + cableWidth) / 2));
        Vector2 topLeft = this.transform.TransformPoint(new Vector2(-(pulleyCollider.size.x + cableWidth) / 2, (pulleyCollider.size.y + cableWidth) / 2));
        Vector2 bottomleft = this.transform.TransformPoint(new Vector2(-(pulleyCollider.size.x + cableWidth) / 2, -(pulleyCollider.size.y + cableWidth) / 2));
        Vector2 bottomRight = this.transform.TransformPoint(new Vector2((pulleyCollider.size.x + cableWidth) / 2, -(pulleyCollider.size.y + cableWidth) / 2));

        Vector2[] corners = {topRight, topLeft, bottomleft, bottomRight};

        Vector2 squareRightVector = PulleyToWorldTransform(Vector2.right) - PulleyCentreGeometrical;
        Vector2 squarePointVector = point - PulleyCentreGeometrical;

        float angle = Vector2.SignedAngle(squareRightVector, squarePointVector);
        if (angle < 0)
        {
            angle += 360.0f;
        }

        int squareSector = ((int)angle) / 45;

        angle -= squareSector * 45.0f;

        int vertex = squareSector / 2;

        if (squareSector % 2 == 0)
        {
            if (!orientation)
            {
                vertex--;
            }
            else
            {
                //The angle where both corners of the square sector is tangent to the point
                //first needs to make sure ratio is within the range of arcsin
                float tippingRatio = ((squareSector % 4 == 0) ? (pulleyCollider.size.y + cableWidth) / 2 : (pulleyCollider.size.x + cableWidth) / 2) / squarePointVector.magnitude;

                //if the ratio is greater than one, then the point is too close for one side to be tangent or the point is potentially inside square.
                //Make sure before calling this function the point is not within the square
                if ((tippingRatio < 1) && (angle > (Mathf.Asin(tippingRatio) * Mathf.Rad2Deg)))
                {
                    vertex++;
                }
            }
        }
        else
        {
            if (orientation)
            {
                vertex++;
            }
            else
            {
                //The angle where both corners of the square sector is tangent to the point
                //first needs to make sure ratio is within the range of arcsin
                float tippingRatio = ((squareSector % 4 == 3) ? (pulleyCollider.size.y + cableWidth) / 2 : (pulleyCollider.size.x + cableWidth) / 2) / squarePointVector.magnitude;

                //if the ratio is greater than one, then the point is too close for one side to be tangent or the point is potentially inside square.
                //Make sure before calling this function the point is not within the square
                if ((tippingRatio < 1) && (angle < (45.0f - Mathf.Asin(tippingRatio) * Mathf.Rad2Deg)))
                {
                    vertex--;
                }
            }
        }

        vertex = vertex % 4;
        if (vertex < 0) vertex += 4;

        identity = vertex;

        return corners[vertex] - PulleyCentreGeometrical;
    }

    public override float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableWidth, bool useSmallest)
    {
        int prevVertex = (int)prevIdentity;
        int currVertex = (int)currIdentity;
        if (prevVertex == currVertex) return 0;

        if (!orientation)
        {
            int aux = prevVertex;
            prevVertex = currVertex;
            currVertex = aux;
        }

        if (useSmallest)
        {
            if ((prevVertex + 1) % 4 == currVertex)
            {
                if (prevVertex % 2 == 0)
                {
                    return pulleyCollider.size.x + cableWidth;
                }
                else
                {
                    return pulleyCollider.size.y + cableWidth;
                }
            }

            int temp = (prevVertex - 1) % 4;
            if (temp < 0) temp += 4;
            if (temp == currVertex)
            {
                if (prevVertex % 2 == 0)
                {
                    return -pulleyCollider.size.y - cableWidth;
                }
                else
                {
                    return -pulleyCollider.size.x - cableWidth;
                }
            }

            print("you did it, you broke the game!!!!");
            return pulleyCollider.size.x + pulleyCollider.size.y;
        }

        int index = (++prevVertex) % 4;
        float distance = (index % 2 == 0) ? pulleyCollider.size.y + cableWidth : pulleyCollider.size.x + cableWidth;
        if (index == currVertex) return distance;

        index = (++index) % 4;
        distance += (index % 2 == 0) ? pulleyCollider.size.y + cableWidth : pulleyCollider.size.x + cableWidth;
        if (index == currVertex) return distance;

        index = (++index) % 4;
        distance += (index % 2 == 0) ? pulleyCollider.size.y + cableWidth : pulleyCollider.size.x + cableWidth;
        return distance;
    }

    public override Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableWidth)
    {
        Vector2 topRight = this.transform.TransformPoint(new Vector2((pulleyCollider.size.x + cableWidth) / 2, (pulleyCollider.size.y + cableWidth) / 2));
        Vector2 topLeft = this.transform.TransformPoint(new Vector2(-(pulleyCollider.size.x + cableWidth) / 2, (pulleyCollider.size.y + cableWidth) / 2));
        Vector2 bottomleft = this.transform.TransformPoint(new Vector2(-(pulleyCollider.size.x + cableWidth) / 2, -(pulleyCollider.size.y + cableWidth) / 2));
        Vector2 bottomRight = this.transform.TransformPoint(new Vector2((pulleyCollider.size.x + cableWidth) / 2, -(pulleyCollider.size.y + cableWidth) / 2));

        Vector2[] corners = { topRight, topLeft, bottomleft, bottomRight };
        
        int pointIndex = Random.Range(0, 3);
        pointIdentity = pointIndex;

        return corners[pointIndex] - PulleyCentreGeometrical;
    }

    public override Vector2 FurthestPoint(Vector2 direction)
    {
        Vector2 point = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + new Vector2(pulleyCollider.size.x / 2, pulleyCollider.size.y / 2));
        float maxDot = Vector2.Dot(point, direction);

        {
            Vector2 tempPoint = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + new Vector2(-pulleyCollider.size.x / 2, pulleyCollider.size.y / 2));
            float tempDot = Vector2.Dot(tempPoint, direction);
            if (tempDot > maxDot)
            {
                point = tempPoint;
                maxDot = tempDot;
            }
        }
        {
            Vector2 tempPoint = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + new Vector2(pulleyCollider.size.x / 2, -pulleyCollider.size.y / 2));
            float tempDot = Vector2.Dot(tempPoint, direction);
            if (tempDot > maxDot)
            {
                point = tempPoint;
                maxDot = tempDot;
            }
        }
        {
            Vector2 tempPoint = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + new Vector2(-pulleyCollider.size.x / 2, -pulleyCollider.size.y / 2));
            float tempDot = Vector2.Dot(tempPoint, direction);
            if (tempDot > maxDot)
            {
                point = tempPoint;
                maxDot = tempDot;
            }
        }
        return point;
    }
}
