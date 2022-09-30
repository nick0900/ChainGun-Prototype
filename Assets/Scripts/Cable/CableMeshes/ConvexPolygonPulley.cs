using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class ConvexPolygonPulley : CableMeshInterface
{

    [SerializeField] PolygonCollider2D pulleyCollider = null;
    
    struct VertexData
    {
        public Vector2 cornerNormal;
        public float edgeLength;
    }

    private List<VertexData> polygonData = null;

    private Vector2 previousPosition = Vector2.zero;
    private Vector2 currentPosition = Vector2.zero;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override Vector2 PulleyCentreGeometrical
    {
        get
        {
            return pulleyCollider.attachedRigidbody.worldCenterOfMass;
        }
    }

    protected override Vector2 PulleyToWorldTransform(Vector2 point)
    {
        return pulleyCollider.transform.TransformPoint(point + pulleyCollider.offset);
    }

    public override Vector2 PulleyCentreOfMass
    {
        get
        {
            return pulleyCollider.transform.TransformPoint(pulleyCollider.offset);
        }
    }

    public override bool MeshGenerated { get { return pulleyCollider != null; } }

    public override bool Errornous { get { return !MeshGenerated || !IsConvex() || !DataCheck() || !pulleyCollider.OverlapPoint(PulleyCentreGeometrical); } }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<PolygonCollider2D>();

        if (pulleyCollider == null) return;

        UpdatePolygonData();
    }

    public override void RemoveChainMesh()
    {
        polygonData = null;

        pulleyCollider = null;
    }

    public override bool PrintErrors()
    {
        bool error = false;

        if (!MeshGenerated)
        {
            error = true;
            print(this + "/Error: mesh not generated");
        }
        else
        {
            if (!IsConvex())
            {
                error = true;
                print(this + "/Error: Polygon primitive is not convex");
            }

            if (!DataCheck())
            {
                error = true;
                print(this + "/Error: polygon help data unsynched");
            }

            if (!pulleyCollider.OverlapPoint(PulleyCentreGeometrical))
            {
                error = true;
                print(this + "/Error: polygon collider offset point not contained in polygon");
            }
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
        else
        {
            if (!IsConvex())
            {
                print(this + "/Error: Polygon primitive is not convex");
                print(this + "/FixFailed: no automatic fix implemented, manual manipulation of polygon needed");
                errorsFixed = false;
            }

            if (!DataCheck())
            {
                print(this + "/Error: polygon help data unsynched");
                print(this + "/Fix: polygon help data is updated");
                UpdatePolygonData();
            }

            if (!pulleyCollider.OverlapPoint(PulleyCentreGeometrical))
            {
                print(this + "/Error: polygon collider offset point not contained in polygon");
                print(this + "/Fix: polygon offset and points are recalculated");
                RecalculatePolygonCollider();
            }
        }

        return errorsFixed;
    }

    private Vector2 Polycenter()
    {
        Vector2 sumCenter = Vector2.zero;
        float sumWeight = 0;

        for (int i = 0; i <  pulleyCollider.points.Length; i++)
        {
            int next = i + 1;
            if (next >= pulleyCollider.points.Length)
            {
                next = 0;
            }

            int prev = i - 1;
            if (prev <= -1)
            {
                prev = pulleyCollider.points.Length - 1;
            }

            float weight = (pulleyCollider.points[i] - pulleyCollider.points[next]).magnitude + (pulleyCollider.points[i] - pulleyCollider.points[prev]).magnitude;
            sumCenter += pulleyCollider.points[i] * weight;
            sumWeight += weight;
        }
        return sumCenter / sumWeight;
    }

    public void RecalculatePolygonCollider()
    {
        Vector2 newCentreOffset = Polycenter();

        pulleyCollider.offset += newCentreOffset;
        for (int i = 0; i < pulleyCollider.points.Length; i++)
        {
            pulleyCollider.points[i] -= newCentreOffset;
        }
    }



    private bool IsConvex()
    {
        Vector2 first = pulleyCollider.points[pulleyCollider.points.Length - 1] - pulleyCollider.points[0];
        Vector2 second = pulleyCollider.points[1] - pulleyCollider.points[0];

        if (Vector2.SignedAngle(first, second) < 0) return false;

        for (int i = 1; i < pulleyCollider.points.Length - 1; i++)
        {
            first = pulleyCollider.points[i - 1] - pulleyCollider.points[i];
            second = pulleyCollider.points[i + 1] - pulleyCollider.points[i];

            if (Vector2.SignedAngle(first, second) < 0) return false;
        }

        first = pulleyCollider.points[pulleyCollider.points.Length - 2] - pulleyCollider.points[pulleyCollider.points.Length - 1];
        second = pulleyCollider.points[0] - pulleyCollider.points[pulleyCollider.points.Length - 1];

        if (Vector2.SignedAngle(first, second) < 0) return false;

        return true;
    }

    public void UpdatePolygonData()
    {
        if (pulleyCollider.points.Length < 3) return;

        polygonData = new List<VertexData>();

        for (int i = 0; i < pulleyCollider.points.Length; i++)
        {
            Vector2 edgeBack;
            Vector2 edgeFront;
            VertexData vertexData = new VertexData();
            if (i == 0)
            {
                edgeBack = pulleyCollider.points[pulleyCollider.points.Length - 1] - pulleyCollider.points[0];
                edgeFront = pulleyCollider.points[1] - pulleyCollider.points[0];
            }
            else if (i == pulleyCollider.points.Length - 1)
            {
                edgeBack = pulleyCollider.points[pulleyCollider.points.Length - 2] - pulleyCollider.points[pulleyCollider.points.Length - 1];
                edgeFront = pulleyCollider.points[0] - pulleyCollider.points[pulleyCollider.points.Length - 1];
            }
            else
            {
                edgeBack = pulleyCollider.points[i - 1] - pulleyCollider.points[i];
                edgeFront = pulleyCollider.points[i + 1] - pulleyCollider.points[i];
            }

            vertexData.cornerNormal = (-Vector2.Lerp(edgeBack, edgeFront, 0.5f)).normalized;

            vertexData.edgeLength = edgeFront.magnitude;

            polygonData.Add(vertexData);
        }
    }

    public bool DataCheck()
    {
        if (polygonData == null) return false;
        if (pulleyCollider.points.Length < 3) return false;
        if (pulleyCollider.points.Length != polygonData.Count) return false;

        for (int i = 0; i < pulleyCollider.points.Length; i++)
        {
            Vector2 edgeBack;
            Vector2 edgeFront;
            if (i == 0)
            {
                edgeBack = pulleyCollider.points[pulleyCollider.points.Length - 1] - pulleyCollider.points[0];
                edgeFront = pulleyCollider.points[1] - pulleyCollider.points[0];
            }
            else if (i == pulleyCollider.points.Length - 1)
            {
                edgeBack = pulleyCollider.points[pulleyCollider.points.Length - 2] - pulleyCollider.points[pulleyCollider.points.Length - 1];
                edgeFront = pulleyCollider.points[0] - pulleyCollider.points[pulleyCollider.points.Length - 1];
            }
            else
            {
                edgeBack = pulleyCollider.points[i - 1] - pulleyCollider.points[i];
                edgeFront = pulleyCollider.points[i + 1] - pulleyCollider.points[i];
            }

            if (polygonData[i].cornerNormal != (-Vector2.Lerp(edgeBack, edgeFront, 0.5f)).normalized) return false;

            if (polygonData[i].edgeLength != edgeFront.magnitude) return false;
        }

        return true;
    }

    private void Awake()
    {
        previousPosition = PulleyCentreGeometrical;
        currentPosition = PulleyCentreGeometrical;

        if (MeshGenerated)
        {
            UpdatePolygonData();
        }
    }
    private void FixedUpdate()
    {
        if (pulleyCollider.attachedRigidbody.bodyType != RigidbodyType2D.Static)
        {
            previousPosition = currentPosition;
            currentPosition = PulleyCentreGeometrical;
        }
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 cableVector = headPrevious - tailPrevious;
        Vector2 centreVector = previousPosition - tailPrevious;

        return Vector2.SignedAngle(cableVector, centreVector) >= 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, out int vertex)
    {
        int highestIndex = 0;
        Vector2 highestVector = PulleyCentreGeometrical + pulleyCollider.points[highestIndex] - point;

        for (int i = 1; i < pulleyCollider.points.Length; i++)
        {
            Vector2 currentVector = (Vector2)pulleyCollider.transform.position + pulleyCollider.offset + pulleyCollider.points[i] - point;

            float angle = Vector2.SignedAngle(highestVector, currentVector);

            if (((angle == 0) && (currentVector.magnitude < highestVector.magnitude)) || ((angle < 0) == orientation))
            {
                highestIndex = i;
                highestVector = currentVector;
            }
        }

        vertex = highestIndex;


        return PulleyToWorldTransform(pulleyCollider.points[highestIndex]) + polygonData[highestIndex].cornerNormal * chainWidth / 2;
    }

    public override float ShapeSurfaceDistance(Vector2 prevTangent, int prevVertex, Vector2 currentTangent, int currentVertex, bool orientation)
    {
        float distance = 0.0f;
        
        if (orientation)
        {
            int aux = prevVertex;
            prevVertex = currentVertex;
            currentVertex = aux;
        }

        while (prevVertex != currentVertex)
        {
            distance += polygonData[prevVertex].edgeLength;
            prevVertex++;
            if (prevVertex >= polygonData.Count - 1)
            {
                prevVertex = 0;
            }
        }

        return distance;
    }

    public override void CreateChainCollider(float chainWidth)
    {
        throw new System.NotImplementedException();
    }

    
}
