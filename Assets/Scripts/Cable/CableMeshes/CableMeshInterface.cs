using System.Collections;
using System.Collections.Generic;
using Unity.Burst.Intrinsics;
using UnityEngine;

[System.Serializable]
abstract public class CableMeshInterface : CableMeshGeneration
{
    protected Vector2 previousPosition = Vector2.zero;

    public void RecordPosition()
    {
        previousPosition = PulleyCentreGeometrical;
    }

    public enum CMPrimitives
    {
        Point,
        Circle,
        polygon,
        Box
    }

    //global position of pulley geometrical centre
    abstract public Vector2 PulleyCentreGeometrical { get; }

    //transformation from a local point on the pulley to world space
    abstract protected Vector2 PulleyToWorldTransform(Vector2 point);

    //attached rigid body
    abstract public Rigidbody2D PulleyAttachedRigidBody { get; }

    abstract public Transform ColliderTransform { get; }

    abstract public CMPrimitives CableMeshPrimitiveType { get; }

    abstract public float SafeStoredLength { get; }

    //Considering the cable direction going from tail to head, a true orientation will have the cable wrapping counter-clockwise
    //The orientation is calculated throught the relative movement of the cable to the pulley. Only call function at first collision and save that orientation
    //most accurate when called during a collision hit after the fixed update is done
    abstract public bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious);

    // will return the global tangent offset from the pulley center. the width of the cable is taken into consideration meaning the tangent point is in the middle of the cable
    //a orientation of true means the function will return the rightmost point
    abstract public Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out float identity);

    abstract public Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableWidth);

    //The Identity is a local representation of the tangent offset of the pulley. For circles the angular position in degrees is used and for polygons the vertex indecies is used.
    //a positive difference between prev and curr identity in a true orientation will result in a positive surface distance.
    //if useSmallest is set to false distance side will be be based on the orientation.
    abstract public float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableWidth, bool useSmallest);


    public bool infiniteFriction = false;
    public bool constantFriction = false;
    public float staticFrictionCoeff = 0.2f;
    public float kineticFrictionCoeff = 0.1f;
    public float FrictionFactor(float slipSign, bool slipping, float storedCable, float cableWidth)
    {
        if (infiniteFriction) return 0.0f;

        if (constantFriction)
        {
            float ret = (slipping) ? kineticFrictionCoeff : staticFrictionCoeff;
            if (slipSign < 0.0f) ret = 1 / ret;
            return ret;
        }

        return ShapeFrictionFactor(slipSign, slipping, storedCable, cableWidth);
    }

    virtual protected float ShapeFrictionFactor(float slipSign, bool slipping, float storedCable, float cableWidth)
    {
        return 1.0f;
    }

    abstract public Vector2 FurthestPoint(Vector2 direction);

    [System.Serializable]
    public struct ContactPoint
    {
        public Vector2 A;
        public Vector2 B;
    }

    [System.Serializable]
    public struct CablePinchManifold
    {
        public bool hasContact;

        public float depth;
        public Vector2 normal;

        public CableMeshInterface bodyA;
        public CableMeshInterface bodyB;
        public ContactPoint contact1;
        public ContactPoint contact2;
        public int contactCount;
    }

    struct SupportPoint
    {
        public Vector2 res;
        public Vector2 A;
        public Vector2 B;
    }
    static private SupportPoint Support(CableMeshInterface s1, CableMeshInterface s2, Vector2 d)
    {
        SupportPoint support = new SupportPoint();
        support.A = s1.FurthestPoint(d);
        support.B = s2.FurthestPoint(-d);
        support.res = support.A - support.B;
        return support;
    }

    static private Vector2 TripleProd(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return Vector3.Cross(Vector3.Cross(v1, v2), v3);
    }

    static private bool HandleSimplex(ref List<SupportPoint> simplex, ref Vector2 d, float margin)
    {
        if (simplex.Count == 2)
        {
            Vector2 A = simplex[1].res;
            Vector2 B = simplex[0].res;

            Vector2 AB = B - A;
            Vector2 AO = -A;
            d = TripleProd(AB, AO, AB).normalized;
            return false;
        }
        {
            Vector2 A = simplex[2].res;
            Vector2 B = simplex[1].res;
            Vector2 C = simplex[0].res;

            Vector2 AB = B - A;
            Vector2 AC = C - A;
            Vector2 AO = -A;
            Vector2 ABperp = TripleProd(AC, AB, AB);
            Vector2 ACperp = TripleProd(AB, AC, AC);

            if (Vector2.Dot(ABperp, AO) > margin)
            {
                d = ABperp.normalized;
                simplex.RemoveAt(0);
                return false;
            }
            else if (Vector2.Dot(ACperp, AO) > margin)
            {
                d = ACperp.normalized;
                simplex.RemoveAt(1);
                return false;
            }
        }
        return true;
    }

    static public CablePinchManifold GJKIntersection(CableMeshInterface s1, CableMeshInterface s2, float cableMargin, float margin)
    {
        Vector2 d = (s2.PulleyCentreGeometrical - s1.PulleyCentreGeometrical).normalized;

        List<SupportPoint> simplex = new List<SupportPoint>();
        simplex.Add(Support(s1, s2, d));
        d = -simplex[0].res.normalized;

        while (true)
        {
            SupportPoint A = Support(s1, s2, d);
            if (Vector2.Dot(A.res, d) < -(cableMargin + margin))
            {
                CablePinchManifold result = new CablePinchManifold();
                result.hasContact = false;
                return result;
            }
            
            simplex.Add(A);
            if (HandleSimplex(ref simplex, ref d, cableMargin + margin))
            {
                return EPA(simplex, s1, s2, cableMargin, margin);
            }
        }
    }

    static private CablePinchManifold EPA(List<SupportPoint> polytope, CableMeshInterface s1, CableMeshInterface s2, float cableMargin, float margin)
    {
        int minIndex1 = 0;
        int minIndex2 = 0;
        float minDistance = Mathf.Infinity;
        Vector2 minNormal = Vector2.zero;

        int iteration = 0;

        while ((minDistance == Mathf.Infinity) && (iteration < 64))
        {
            iteration++;
            for (int i = 0; i < polytope.Count; i++)
            {
                int j = (i + 1) % polytope.Count;
                int k = (j + 1) % polytope.Count;
                Vector2 A = polytope[i].res;
                Vector2 B = polytope[j].res;
                Vector2 C = polytope[k].res;

                Vector2 AB = B - A;
                Vector2 AC = C - A;

                Vector2 normal = new Vector2(AB.y, -AB.x).normalized;
                if (Vector2.Dot(AC, normal) > 0.0f)
                {
                    normal *= -1;
                }

                float distance = Vector2.Dot(normal, A) + (cableMargin + margin);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minNormal = normal;
                    minIndex1 = i;
                    minIndex2 = j;
                }
            }

            SupportPoint support = Support(s1, s2, minNormal);
            float sDistance = Vector2.Dot(minNormal, support.res) + (cableMargin + margin);
            
            if (Mathf.Abs(sDistance - minDistance) > 0.001f)
            {
                minDistance = Mathf.Infinity;
                polytope.Insert(minIndex2, support);
            }
        }
        print(iteration);
        CablePinchManifold result = new CablePinchManifold();
        result.hasContact = true;
        result.depth = minDistance;
        result.normal = minNormal;
        result.bodyA = s1;
        result.bodyB = s2;
        ContactPoints(polytope[minIndex1], polytope[minIndex2], ref result, cableMargin, margin);
        return result;
    }

    // made by me!
    static private void ContactPoints(SupportPoint p1, SupportPoint p2, ref CablePinchManifold manifold, float cableMargin, float margin)
    {
        if (manifold.bodyA.CableMeshPrimitiveType == CMPrimitives.Circle)
        {
            if (manifold.bodyB.CableMeshPrimitiveType == CMPrimitives.Circle)
            {
                CircleCircleContact(ref manifold);
            }
            else
            {
                manifold.contactCount = 1;
                CirclePolygonContact(manifold.bodyA, manifold.normal, manifold.depth, cableMargin, margin, out manifold.contact1.A, out manifold.contact1.B);
            }
        }
        else
        {
            if (manifold.bodyB.CableMeshPrimitiveType == CMPrimitives.Circle)
            {
                manifold.contactCount = 1;
                CirclePolygonContact(manifold.bodyB, -manifold.normal, manifold.depth, cableMargin, margin, out manifold.contact1.B, out manifold.contact1.A);
            }
            else
            {
                PolygonPolygonContact(p1, p2, ref manifold);
            }
        }
    }

    static private void CircleCircleContact(ref CablePinchManifold manifold)
    {
        manifold.contactCount = 1;
        manifold.contact1.A = manifold.bodyA.FurthestPoint(manifold.normal);
        manifold.contact1.B = manifold.bodyB.FurthestPoint(-manifold.normal);
    }

    static private void CirclePolygonContact(CableMeshInterface circle, Vector2 contactNormal, float depth, float cableMargin, float margin, out Vector2 circleContact, out Vector2 polygonContact)
    {
        circleContact = circle.FurthestPoint(contactNormal);
        polygonContact = circleContact - contactNormal * (depth - (cableMargin + margin));
    }

    static private void PolygonPolygonContact(SupportPoint sp1, SupportPoint sp2, ref CablePinchManifold manifold)
    {
        if (sp1.A == sp2.A)
        {
            manifold.contactCount = 1;
            manifold.contact1.A = sp1.A;
            manifold.contact1.B = sp1.A - manifold.normal * manifold.depth;
        }
        else if (sp1.B == sp2.B)
        {
            manifold.contactCount = 1;
            manifold.contact1.B = sp1.B;
            manifold.contact1.B = sp1.A - manifold.normal * manifold.depth;
        }
        else
        {
            Vector2 normPerp = new Vector2(manifold.normal.y, -manifold.normal.x);

            float A1 = Vector2.Dot(sp1.A, normPerp);
            float A2 = Vector2.Dot(sp2.A, normPerp);
            float B1 = Vector2.Dot(sp1.B, normPerp);
            float B2 = Vector2.Dot(sp2.B, normPerp);

            if (A1 <= B1 ? A1 > B2 : A1 <= B2)
            {
                float d = DistanceToEdge(sp1.A, manifold.normal, sp1.B, sp2.B);
                if (Mathf.Abs(d - manifold.depth) <= 0.001)
                {
                    manifold.contactCount++;
                    manifold.contact1.A = sp1.A;
                    manifold.contact1.B = sp1.A + manifold.normal * d;
                }
            }

            if (A2 <= B1 ? A2 > B2 : A2 <= B2)
            {
                float d = DistanceToEdge(sp2.A, manifold.normal, sp1.B, sp2.B);
                if (Mathf.Abs(d - manifold.depth) <= 0.001)
                {
                    manifold.contactCount++;
                    if (manifold.contactCount == 1)
                    {
                        manifold.contact1.A = sp2.A;
                        manifold.contact1.B = sp2.A + manifold.normal * d;
                    }
                    else
                    {
                        manifold.contact2.A = sp2.A;
                        manifold.contact2.B = sp2.A + manifold.normal * d;
                        return;
                    }
                }
            }

            if (B1 <= A1 ? B1 > A2 : B1 <= A2)
            {
                float d = DistanceToEdge(sp1.B, manifold.normal, sp1.A, sp2.A);
                if (Mathf.Abs(d - manifold.depth) <= 0.001)
                {
                    manifold.contactCount++;
                    if (manifold.contactCount == 1)
                    {
                        manifold.contact1.B = sp1.B;
                        manifold.contact1.A = sp1.B + manifold.normal * d;
                    }
                    else
                    {
                        manifold.contact2.B = sp1.B;
                        manifold.contact2.A = sp1.B + manifold.normal * d;
                        return;
                    }
                }
            }

            if (B2 <= A1 ? B2 > A2 : B2 <= A2)
            {
                float d = DistanceToEdge(sp2.B, manifold.normal, sp1.A, sp2.A);
                if (Mathf.Abs(d - manifold.depth) <= 0.001)
                {
                    manifold.contactCount++;
                    if (manifold.contactCount == 1)
                    {
                        manifold.contact1.B = sp2.B;
                        manifold.contact1.A = sp2.B + manifold.normal * d;
                    }
                    else
                    {
                        manifold.contact2.B = sp2.B;
                        manifold.contact2.A = sp2.B + manifold.normal * d;
                    }
                }
            }
        }
    }

    static private float DistanceToEdge(Vector2 point, Vector2 normal, Vector2 edge1, Vector2 edge2)
    {
        Vector2 edgeVec = edge2 - edge1;
        Vector2 pointVec = point - edge1;

        Vector2 proj = (Vector2.Dot(pointVec, edgeVec) / edgeVec.sqrMagnitude) * edgeVec;
        return Mathf.Abs(Vector2.Dot(proj, normal));
    }
}
