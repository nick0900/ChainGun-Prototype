using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoxPulley : CableMeshInterface
{
    [SerializeField] BoxCollider2D data = null;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override bool MeshGenerated { get { return data != null; } }

    public override bool Errornous { get { return !MeshGenerated; } }

    protected override void SetupMesh()
    {
        data = GetComponent<BoxCollider2D>();
    }

    public override void RemoveChainMesh()
    {
        data = null;
    }

    public override bool PrintErrors()
    {
        bool error = false;

        if (!MeshGenerated)
        {
            error = true;
            print(this + "/Error: mesh not generated");
        }

        return error;
    }

    public override bool CorrectErrors()
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
        Vector2 tailDirection = tailPrevious - CenterWorldPosition(data);
        Vector2 headDirection = headPrevious - CenterWorldPosition(data);

        return Vector2.SignedAngle(tailDirection, headDirection) > 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache)
    {
        Vector2 topRight = this.transform.TransformPoint(new Vector2(data.size.x, data.size.y));
        Vector2 topLeft = this.transform.TransformPoint(new Vector2(-data.size.x, data.size.y));
        Vector2 bottomleft = this.transform.TransformPoint(new Vector2(-data.size.x, -data.size.y));
        Vector2 bottomRight = this.transform.TransformPoint(new Vector2(data.size.x, -data.size.y));

        Vector2[] points = { topRight, topLeft, bottomleft, bottomRight };

        int highestIndex = cache.polygonIndex;
        if (highestIndex < 0 || highestIndex >= data.points.Length)
        {
            highestIndex = 0;
        }
        Vector2 highestVector = (Vector2)data.transform.position + data.offset + data.points[highestIndex] - point;

        int currentIndex = highestIndex + 1;
        if (currentIndex >= data.points.Length)
        {
            currentIndex = 0;
        }
        Vector2 currentVector = (Vector2)data.transform.position + data.offset + data.points[currentIndex] - point;

        float angle = Vector2.SignedAngle(highestVector, currentVector);

        if (((angle == 0) && (currentVector.magnitude < highestVector.magnitude)) || ((angle < 0) == orientation))
        {
            currentIndex = highestIndex - 1;
            if (currentIndex < 0)
            {
                currentIndex = data.points.Length - 1;
            }
            currentVector = (Vector2)data.transform.position + data.offset + data.points[currentIndex] - point;

            angle = Vector2.SignedAngle(highestVector, currentVector);

            while (((angle == 0) && (currentVector.magnitude < highestVector.magnitude)) || ((angle < 0) == orientation))
            {
                highestIndex = currentIndex;
                highestVector = currentVector;

                currentIndex--;
                if (currentIndex < 0)
                {
                    currentIndex = data.points.Length - 1;
                }
                currentVector = (Vector2)data.transform.position + data.offset + data.points[currentIndex] - point;

                angle = Vector2.SignedAngle(highestVector, currentVector);
            }
        }
        else
        {
            while (((angle == 0) && (currentVector.magnitude < highestVector.magnitude)) || ((angle < 0) == orientation))
            {
                highestIndex = currentIndex;
                highestVector = currentVector;

                currentIndex++;
                if (currentIndex >= data.points.Length)
                {
                    currentIndex = 0;
                }
                currentVector = (Vector2)data.transform.position + data.offset + data.points[currentIndex] - point;
            }
        }

        cache.polygonIndex = highestIndex;
        return highestVector + point;
    }

    public override void CreateChainCollider(float chainWidth)
    {
        throw new System.NotImplementedException();
    }
}
