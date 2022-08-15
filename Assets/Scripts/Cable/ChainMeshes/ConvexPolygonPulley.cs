using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConvexPolygonPulley : ChainMesh
{

    [SerializeField] PolygonCollider2D data = null;
    [SerializeField] Vector2 center = Vector2.zero;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override bool MeshGenerated { get { return data != null; } }

    public override bool Errornous { get { return (MeshGenerated && !IsConvex(data.points)) || 
                (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this)) || 
                (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this)) ||
                (MeshGenerated && (Polycenter(data.points) != center)) || 
                (!MeshGenerated && CMDictionary.CMD.IsRegistered(this)); } }

    public override void GenerateChainMesh()
    {
        if (submesh) return;

        data = GetComponent<PolygonCollider2D>();

        if (data == null) return;

        center = Polycenter(data.points) - (Vector2)data.transform.position;

        CMDictionary.CMD.Register(data, this);

        PrintErrors();
    }
    public override void RemoveChainMesh()
    {
        if (data != null)
        {
            CMDictionary.CMD.Unregister(data);
        }

        center = Vector2.zero;

        data = null;
    }

    public override void GenerateSubMesh(ChainMesh root)
    {
        if (submesh) return;

        data = GetComponent<PolygonCollider2D>();

        if (data == null) return;

        center = Polycenter(data.points);

        CMDictionary.CMD.Register(data, root);

        PrintErrors();
    }

    public override void ForceGenerateSubMesh(ChainMesh root)
    {
        if (!ProtectSettings)
        {
            submesh = true;
        }

        GenerateSubMesh(root);
    }

    public override bool PrintErrors()
    {
        bool error = false;

        if (MeshGenerated && !IsConvex(data.points))
        {
            error = true;
            print(this + "/Error: Polygon primitive is not convex");
        }

        if (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this))
        {
            if (!CMDictionary.CMD.IsRegistered(data))
            {
                print(this + "/Error: collider:" + data + " not registered");
            }
            else
            {
                print(this + "/Error: collider:" + data + " registered to another chainmesh");
            }
            error = true;
        }

        if (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this))
        {
            print(this + "/Error: collider:" + data + " registered to this chainmesh in spite of being a submesh");
            error = true;
        }

        if (MeshGenerated && (Polycenter(data.points) != center))
        {
            error = true;
            print(this + "/Error: center is not geometric center");
        }

        if (!MeshGenerated && CMDictionary.CMD.IsRegistered(this))
        {
            error = true;
            print(this + "/Error: Unknown collider is registered to chainmesh");
        }

        return error;
    }

    public override bool CorrectErrors()
    {
        bool errorsFixed = true;

        if (MeshGenerated && !IsConvex(data.points))
        {
            print(this + "/Error: Polygon primitive is not convex");
            print(this + "/FixFailed: no automatic fix implemented");
            errorsFixed = false;
        }

        if (MeshGenerated && !submesh && !CMDictionary.CMD.IsRegistered(data, this))
        {
            if (!CMDictionary.CMD.IsRegistered(data))
            {
                print(this + "/Error: collider:" + data + " not registered");
            }
            else
            {
                print(this + "/Error: collider:" + data + " registered to another chainmesh");
            }

            CMDictionary.CMD.Register(data, this);

            print(this + "/Fix: re registering");
            if (!CMDictionary.CMD.IsRegistered(data, this))
            {
                print("Failed");
                errorsFixed = false;
            }
            else
            {
                print("Succeded");
            }
        }

        if (MeshGenerated && submesh && CMDictionary.CMD.IsRegistered(data, this))
        {
            print(this + "/Error: collider:" + data + " registered to this chainmesh in spite of being a submesh");
            print(this + "/Fix: removing chainmesh");

            RemoveChainMesh();
        }

        if (MeshGenerated && (Polycenter(data.points) != center))
        {
            print(this + "/Error: center is not geometric center");
            print(this + "/Fix: Recalculating Center");
            center = Polycenter(data.points);
        }

        if (!MeshGenerated && CMDictionary.CMD.IsRegistered(this))
        {
            errorsFixed = false;
            print(this + "/Error: Unknown collider is registered to chainmesh");
            print(this + "/FixFailed: No way of unregistering collider. global collider recalculation or manual removal of collider needed!");
        }

        return errorsFixed;
    }

    static public Vector2 Polycenter(Vector2[] polygon)
    {
        Vector2 sumCenter = Vector2.zero;
        float sumWeight = 0;

        for (int i = 0; i < polygon.Length; i++)
        {
            int next = i + 1;
            if (next >= polygon.Length)
            {
                next = 0;
            }

            int prev = i - 1;
            if (prev <= -1)
            {
                prev = polygon.Length - 1;
            }

            float weight = (polygon[i] - polygon[next]).magnitude + (polygon[i] - polygon[prev]).magnitude;
            sumCenter += polygon[i] * weight;
            sumWeight += weight;
        }
        return sumCenter / sumWeight;
    }

    static public bool IsConvex(Vector2[] polygon)
    {
        Vector2 first = polygon[polygon.Length - 1] - polygon[0];
        Vector2 second = polygon[1] - polygon[0];

        if (Vector2.SignedAngle(first, second) < 0) return false;

        for (int i = 1; i < polygon.Length - 1; i++)
        {
            first = polygon[i - 1] - polygon[i];
            second = polygon[i + 1] - polygon[i];

            if (Vector2.SignedAngle(first, second) < 0) return false;
        }

        first = polygon[polygon.Length - 2] - polygon[polygon.Length - 1];
        second = polygon[0] - polygon[polygon.Length - 1];

        if (Vector2.SignedAngle(first, second) < 0) return false;

        return true;
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 tailDirection = tailPrevious - ((Vector2)data.transform.position + data.offset);
        Vector2 headDirection = headPrevious - ((Vector2)data.transform.position + data.offset);

        return Vector2.SignedAngle(tailDirection, headDirection) > 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache)
    {
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
