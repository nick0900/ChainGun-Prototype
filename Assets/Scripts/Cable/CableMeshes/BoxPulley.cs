using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxPulley : CableMeshInterface
{
    [SerializeField] BoxCollider2D pulleyCollider = null;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

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

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        return pulleyCollider.transform.TransformPoint(point + pulleyCollider.offset);
    }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<BoxCollider2D>();
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
        Vector2 topRight = this.transform.TransformPoint(new Vector2(pulleyCollider.size.x, pulleyCollider.size.y));
        Vector2 topLeft = this.transform.TransformPoint(new Vector2(-pulleyCollider.size.x, pulleyCollider.size.y));
        Vector2 bottomleft = this.transform.TransformPoint(new Vector2(-pulleyCollider.size.x, -pulleyCollider.size.y));
        Vector2 bottomRight = this.transform.TransformPoint(new Vector2(pulleyCollider.size.x, -pulleyCollider.size.y));

        Vector2[] corners = {topRight, topLeft, bottomleft, bottomRight};

        Vector2 squareRightVector = PulleyToWorldTransform(Vector2.right) - PulleyCentreGeometrical;
        Vector2 squarePointVector = point - PulleyCentreGeometrical;

        float angle = Vector2.SignedAngle(squarePointVector, squareRightVector);
        if (angle < 0)
        {
            angle = -angle + 180.0f;
        }

        int squareSector = ((int)angle) / 45;

        angle -= squareSector * 45.0f;

        vertex = squareSector / 2;

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
                float tippingAngle = (squareSector % 4 == 0) ? pulleyCollider.size.y : pulleyCollider.size.x;

                //if the ratio is greater than one, then the point is too close for one side to be tangent or the point is potentially inside square.
                //Make sure before calling this function the point is not within the square
                if ((tippingAngle < 1) && (angle > Mathf.Asin(tippingAngle)))
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
                float tippingAngle = (squareSector % 4 == 3) ? pulleyCollider.size.y : pulleyCollider.size.x;

                //if the ratio is greater than one, then the point is too close for one side to be tangent or the point is potentially inside square.
                //Make sure before calling this function the point is not within the square
                if ((tippingAngle < 1) && (angle < Mathf.Asin(tippingAngle)))
                {
                    vertex--;
                }
            }
        }

        vertex = vertex % 4;

        Vector2 tangent = PulleyToWorldTransform(corners[vertex]) - PulleyCentreGeometrical;

        return tangent + tangent.normalized * cableWidth / 2;
    }

    public override float ShapeSurfaceDistance(Vector2 prevTangent, int prevVertex, Vector2 currentTangent, int currentVertex, bool orientation)
    {
        if (prevVertex == currentVertex) return 0;

        if (!orientation)
        {
            int aux = prevVertex;
            prevVertex = currentVertex;
            currentVertex = aux;
        }

        if ((prevVertex + 1) % 4 == currentVertex)
        {
            if (prevVertex % 2 == 0)
            {
                return pulleyCollider.size.x * 2;
            }
            else
            {
                return pulleyCollider.size.y * 2;
            }
        }

        if ((prevVertex - 1) % 4 == currentVertex)
        {
            if (prevVertex % 2 == 0)
            {
                return -pulleyCollider.size.y * 2;
            }
            else
            {
                return -pulleyCollider.size.x * 2;
            }
        }

        print("you did it, you broke the game!!!!");
        return pulleyCollider.size.x + pulleyCollider.size.y;
    }

    public override void CreateChainCollider(float chainWidth)
    {
        throw new System.NotImplementedException();
    }
}
