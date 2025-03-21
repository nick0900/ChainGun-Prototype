using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
//using System.Numerics;

[System.Serializable]
public class ConvexPolygonPulley : CableMeshInterface
{

    [HideInInspector][SerializeField] PolygonCollider2D pulleyCollider = null;

    [System.Serializable]
    struct VertexData
    {
        public Vector2 cornerNormal;
        public float edgeLength;
    }

    [HideInInspector][SerializeField] private List<VertexData> polygonData = null;

    [HideInInspector][SerializeField] float minSide = 0;

    public override CMPrimitives CableMeshPrimitiveType { get { return CMPrimitives.polygon; } }

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

    public override bool MeshGenerated { get { return pulleyCollider != null; } }

    public override bool Errornous { get { return !MeshGenerated || !IsConvex() || !DataCheck() || !pulleyCollider.OverlapPoint(PulleyCentreGeometrical); } }

    public override float SafeStoredLength { get { return minSide; } }

    protected override void SetupMesh()
    {
        pulleyCollider = GetComponent<PolygonCollider2D>();

        if (pulleyCollider == null) return;

        UpdatePolygonData();
    }

    protected override void RemoveCableMesh()
    {
        polygonData = null;

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

    protected override bool CorrectErrors()
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

            if (!pulleyCollider.OverlapPoint(PulleyCentreGeometrical))
            {
                print(this + "/Error: polygon collider offset point not contained in polygon");
                print(this + "/Fix: polygon offset and points are recalculated");
                RecalculatePolygonCollider();
            }

            if (!DataCheck())
            {
                print(this + "/Error: polygon help data unsynched");
                print(this + "/Fix: polygon help data is updated");
                UpdatePolygonData();
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
#if UNITY_EDITOR
        Object[] editedFields = { pulleyCollider, this };
        Undo.RecordObjects(editedFields, "recalculating polygon " + gameObject.name);
#endif

        Vector2 newCentreOffset = Polycenter();

        pulleyCollider.offset += newCentreOffset;
        
        Vector2[] points = pulleyCollider.points;
        for (int i = 0; i < pulleyCollider.points.Length; i++)
        {
            points[i] -= newCentreOffset;
        }
        pulleyCollider.SetPath(0, points);

#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }



    private bool IsConvex()
    {
        Vector2 first = pulleyCollider.points[pulleyCollider.points.Length - 1] - pulleyCollider.points[0];
        Vector2 second = pulleyCollider.points[1] - pulleyCollider.points[0];

        if (Vector2.SignedAngle(first, second) > 0) return false;

        for (int i = 1; i < pulleyCollider.points.Length - 1; i++)
        {
            first = pulleyCollider.points[i - 1] - pulleyCollider.points[i];
            second = pulleyCollider.points[i + 1] - pulleyCollider.points[i];

            if (Vector2.SignedAngle(first, second) > 0) return false;
        }

        first = pulleyCollider.points[pulleyCollider.points.Length - 2] - pulleyCollider.points[pulleyCollider.points.Length - 1];
        second = pulleyCollider.points[0] - pulleyCollider.points[pulleyCollider.points.Length - 1];

        if (Vector2.SignedAngle(first, second) > 0) return false;

        return true;
    }

    public void UpdatePolygonData()
    {
        if (pulleyCollider.points.Length < 3) return;

#if UNITY_EDITOR
        Undo.RecordObject(this, "updating polygon data " + gameObject.name);
#endif

        polygonData = new List<VertexData>();

        minSide = float.MaxValue;

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

            if (vertexData.edgeLength < minSide) minSide = vertexData.edgeLength;
        }
#if UNITY_EDITOR
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
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

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 cableVector = headPrevious - tailPrevious;
        Vector2 centreVector = previousPosition - tailPrevious;

        return Vector2.SignedAngle(cableVector, centreVector) >= 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, out float identity)
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

        identity = highestIndex;


        return PulleyToWorldTransform(pulleyCollider.points[highestIndex]) - PulleyCentreGeometrical + polygonData[highestIndex].cornerNormal * chainWidth / 2;
    }

    public override float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableWidth, bool useSmallest)
    {
        int prevVertex = (int)prevIdentity;
        int currVertex = (int)currIdentity;

        if (prevVertex == currVertex) return 0.0f;

        if (!orientation)
        {
            int aux = prevVertex;
            prevVertex = currVertex;
            currVertex = aux;
        }

        float firstDistance = 0.0f;
        float secondDistance = 0.0f;

        bool firstAccumulate = true;

        int index = prevVertex;
        for (int i = 0; i < polygonData.Count; i++)
        {
            if (firstAccumulate)
            {
                firstDistance += polygonData[index].edgeLength;

                index++;
                if (index >= polygonData.Count)
                {
                    index = 0;
                }

                if (index == currVertex)
                {
                    firstAccumulate = false;
                }
            }
            else
            {
                secondDistance += polygonData[index].edgeLength;

                index++;
                if (index >= polygonData.Count)
                {
                    index = 0;
                }
            }
        }

        if (useSmallest)
        {
            if (firstDistance > secondDistance) return -secondDistance;

            return firstDistance;
        }

        return firstDistance;
    }

    public override Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableWidth)
    {
        int pointIndex = Random.Range(0, polygonData.Count - 1);
        pointIdentity = pointIndex;

        return PulleyToWorldTransform(pulleyCollider.points[pointIndex]) - PulleyCentreGeometrical + polygonData[pointIndex].cornerNormal * cableWidth / 2;
    }

    public override Vector2 FurthestPoint(Vector2 direction)
    {
        Vector2 point = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + pulleyCollider.points[0]);
        float maxDot = Vector2.Dot(point, direction);

        for (int i = 1; i < pulleyCollider.points.Length; i++)
        {
            Vector2 tempPoint = pulleyCollider.transform.TransformPoint(pulleyCollider.offset + pulleyCollider.points[i]);
            float tempDot = Vector2.Dot(tempPoint, direction);
            if (tempDot > maxDot)
            {
                point = tempPoint;
                maxDot = tempDot;
            }
        }
        return point;
    }

    public override int IndexFromPoint(Vector2 point)
    {
        point = ((Vector2)pulleyCollider.transform.InverseTransformPoint(point)) + pulleyCollider.offset;

        for (int i = 0; i < pulleyCollider.points.Length; ++i)
        {
            if (Mathf.Abs((point.x - pulleyCollider.points[i].x) + (point.y - pulleyCollider.points[i].y)) <= 0.0001)
            {
                return i;
            }
        }
        return -1;
    }

    public override Vector2 GetNextPoint(int i)
    {
        if (i >= (pulleyCollider.points.Length - 1))
        {
            return pulleyCollider.points[0];
        }
        return pulleyCollider.points[i + 1];
    }

    public override Vector2 GetPreviousPoint(int i)
    {
        if (i <= 0)
        {
            return pulleyCollider.points[pulleyCollider.points.Length - 1];
        }
        return pulleyCollider.points[i - 1];
    }
}
