using System.Collections;
using System.Collections.Generic;
using System.Drawing;
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

    //AABB of pulley
    public abstract Bounds PulleyBounds { get; }

    public abstract float MaxExtent { get; }

    public abstract Vector2 CenterOfMass { get; }

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
    abstract public Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableHalfWidth, out float identity);

    static public void TangentAlgorithm(CableMeshInterface pulley1, CableMeshInterface pulley2, out Vector2 tangent1, out Vector2 tangent2, out float tangentIdentity1, out float tangentIdentity2, bool orientation1, bool orientation2, float cableHalfWidth)
    {
        if ((pulley1.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle) && (pulley2.CableMeshPrimitiveType == CableMeshInterface.CMPrimitives.Circle))
        {
            (pulley1 as CirclePulley).CircleToCircleTangent(orientation1, out tangent1, out tangentIdentity1, pulley2 as CirclePulley, orientation2, out tangent2, out tangentIdentity2, cableHalfWidth);
            return;
        }

        bool alternator = false;

        tangent1 = Vector2.zero;
        tangent2 = Vector2.zero;
        tangentIdentity1 = 0;
        tangentIdentity2 = 0;

        float newIdentity1 = 0;
        float newIdentity2 = 0;
        Vector2 newTan1 = pulley1.RandomSurfaceOffset(ref newIdentity1, cableHalfWidth);
        Vector2 newTan2 = Vector2.zero;

        do
        {
            if (alternator)
            {
                tangent2 = newTan2;
                tangentIdentity2 = newIdentity2;

                newTan1 = pulley1.PointToShapeTangent(pulley2.PulleyCentreGeometrical + newTan2, orientation1, cableHalfWidth, out newIdentity1);

                alternator = false;
            }
            else
            {
                tangent1 = newTan1;
                tangentIdentity1 = newIdentity1;

                newTan2 = pulley2.PointToShapeTangent(pulley1.PulleyCentreGeometrical + newTan1, !orientation2, cableHalfWidth, out newIdentity2);

                alternator = true;
            }

        } while (alternator ? (tangent2 != newTan2) : (tangent1 != newTan1));
    }

    abstract public Vector2 RandomSurfaceOffset(ref float pointIdentity, float cableHalfWidth);

    //The Identity is a local representation of the tangent offset of the pulley. For circles the angular position in degrees is used and for polygons the vertex indecies is used.
    //a positive difference between prev and curr identity in a true orientation will result in a positive surface distance.
    //if useSmallest is set to false distance side will be be based on the orientation.
    abstract public float ShapeSurfaceDistance(float prevIdentity, float currIdentity, bool orientation, float cableHalfWidth, bool useSmallest);


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

    static public CablePinchManifold GJKIntersection(CableMeshInterface s1, CableMeshInterface s2, float margin)
    {
        if ((s1.CableMeshPrimitiveType == CMPrimitives.Circle) && (s2.CableMeshPrimitiveType == CMPrimitives.Circle))
            return CircleCircleIntersection((CirclePulley)s1, (CirclePulley)s2, margin);

        Vector2 d = (s2.PulleyCentreGeometrical - s1.PulleyCentreGeometrical).normalized;

        List<SupportPoint> simplex = new List<SupportPoint>();
        simplex.Add(Support(s1, s2, d));
        d = -simplex[0].res.normalized;

        while (true)
        {
            SupportPoint A = Support(s1, s2, d);
            if (Vector2.Dot(A.res, d) < -margin)
            {
                CablePinchManifold result = new CablePinchManifold();
                result.hasContact = false;
                return result;
            }
            
            simplex.Add(A);
            if (HandleSimplex(ref simplex, ref d, margin))
            {
                return EPA(simplex, s1, s2, margin);
            }
        }
    }

    static private CablePinchManifold EPA(List<SupportPoint> polytope, CableMeshInterface s1, CableMeshInterface s2, float margin)
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
                Vector2 normal;

                if (AB == Vector2.zero)
                {
                    // needed for corner corner distance of polygons
                    normal = (polytope[i].B - polytope[i].A).normalized;
                }
                else
                {
                    normal = new Vector2(AB.y, -AB.x).normalized;
                    if (Vector2.Dot(AC, normal) > 0.0f)
                    {
                        normal *= -1;
                    }
                }

                float distance = Vector2.Dot(normal, A);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minNormal = normal;
                    minIndex1 = i;
                    minIndex2 = j;
                }
            }

            SupportPoint support = Support(s1, s2, minNormal);
            float sDistance = Vector2.Dot(minNormal, support.res);
            
            if (Mathf.Abs(sDistance - minDistance) > 0.001f)
            {
                minDistance = Mathf.Infinity;
                polytope.Insert(minIndex2, support);
            }
        }
        //print(iteration);
        CablePinchManifold result = new CablePinchManifold();
        if (minDistance <= -margin)
        {
            result.hasContact = false;
            return result;
        }
        result.hasContact = true;
        result.depth = minDistance + margin;
        result.normal = minNormal;
        result.bodyA = s1;
        result.bodyB = s2;
        ContactPoints(polytope[minIndex1], polytope[minIndex2], ref result, margin);
        return result;
    }

    // made by me!
    static private void ContactPoints(SupportPoint p1, SupportPoint p2, ref CablePinchManifold manifold, float margin)
    {
        if (manifold.bodyA.CableMeshPrimitiveType == CMPrimitives.Circle)
        {
            manifold.contactCount = 1;
            CirclePolygonContact(manifold.bodyA, manifold.normal, manifold.depth, margin, out manifold.contact1.A, out manifold.contact1.B);
        }
        else if (manifold.bodyB.CableMeshPrimitiveType == CMPrimitives.Circle)
        {
            manifold.contactCount = 1;
            CirclePolygonContact(manifold.bodyB, -manifold.normal, manifold.depth, margin, out manifold.contact1.B, out manifold.contact1.A);
        }
        else
        {
            PolygonPolygonContact(p1, p2, ref manifold, margin);
        }
    }

    static private void CirclePolygonContact(CableMeshInterface circle, Vector2 contactNormal, float depth, float margin, out Vector2 circleContact, out Vector2 polygonContact)
    {
        circleContact = circle.FurthestPoint(contactNormal);
        polygonContact = circleContact - contactNormal * (depth - margin);
    }

    static private void PolygonPolygonContact(SupportPoint sp1, SupportPoint sp2, ref CablePinchManifold manifold, float margin)
    {
        manifold.contactCount = 0;
        bool isA = false;
        if (sp1.A == sp2.A)
        {
            isA = true;
            manifold.contactCount++;
            ContactsFromPoint(sp1.A, true, in manifold, margin, out manifold.contact1);
        }
        if (sp1.B == sp2.B)
        {
            if (manifold.contactCount == 1) return;

            manifold.contactCount++;
            ContactsFromPoint(sp1.B, false, in manifold, margin, out manifold.contact1);
        }
        
        if (manifold.contactCount == 1)
        {
            if (isA)
            {
                CableMeshInterface polygon = manifold.bodyA;
                int index = polygon.IndexFromPoint(sp1.A);

                Vector2 newPoint = polygon.GetNextPoint(index);
                if (Mathf.Abs(Vector2.Dot((newPoint - sp1.A).normalized, manifold.normal)) <= 0.01)
                {
                    PolygonMultiContact(sp1.A, newPoint, sp1.B, sp2.B, ref manifold, margin, 0);
                    return;
                }

                newPoint = polygon.GetPreviousPoint(index);
                if (Mathf.Abs(Vector2.Dot((newPoint - sp1.A).normalized, manifold.normal)) <= 0.01)
                {
                    PolygonMultiContact(sp1.A, newPoint, sp1.B, sp2.B, ref manifold, margin, 0);
                    return;
                }
            }
            else
            {
                CableMeshInterface polygon = manifold.bodyB;
                int index = polygon.IndexFromPoint(sp1.B);

                Vector2 newPoint = polygon.GetNextPoint(index);
                if (Mathf.Abs(Vector2.Dot((newPoint - sp1.B).normalized, manifold.normal)) <= 0.01)
                {
                    PolygonMultiContact(sp1.A, sp2.A, newPoint, sp1.B, ref manifold, margin, 3);
                    return;
                }

                newPoint = polygon.GetPreviousPoint(index);
                if (Mathf.Abs(Vector2.Dot((newPoint - sp1.B).normalized, manifold.normal)) <= 0.01)
                {
                    PolygonMultiContact(sp1.A, sp2.A, newPoint, sp1.B, ref manifold, margin, 3);
                    return;
                }
            }
        }
        else
        {
            PolygonMultiContact(sp1.A, sp2.A, sp1.B, sp2.B, ref manifold, margin);
        }
    }

    public abstract int IndexFromPoint(Vector2 point);
    public abstract Vector2 GetNextPoint(int i);
    public abstract Vector2 GetPreviousPoint(int i);

    static private void ContactsFromPoint(Vector2 point, bool onA, in CablePinchManifold manifold, float margin, out ContactPoint contact)
    {
        if (onA)
        {
            contact.A = point;
            contact.B = point - manifold.normal * (manifold.depth - margin);
        }
        else
        {
            contact.B = point;
            contact.A = point + manifold.normal * (manifold.depth - margin);
        }
    }

    private class PolygonPoint
    {
        public Vector2 point;
        public float value;
        public bool isA = false;
        public bool ignore = false;
    }
    static private void PolygonMultiContact(Vector2 pointA1, Vector2 pointA2, Vector2 pointB1, Vector2 pointB2, ref CablePinchManifold manifold, float margin, int ignoreIndex = -1)
    {
        Vector2 normPerp = new Vector2(manifold.normal.y, -manifold.normal.x);

        List<PolygonPoint> points = new List<PolygonPoint>();
        points.Add(new PolygonPoint());
        points[0].point = pointA1;
        points[0].isA = true;
        points.Add(new PolygonPoint());
        points[1].point = pointB1;
        points.Add(new PolygonPoint());
        points[2].point = pointA2;
        points[2].isA = true;
        points.Add(new PolygonPoint());
        points[3].point = pointB2;
        if (ignoreIndex != -1)
        {
            points[ignoreIndex].ignore = true;
        }

        for (int i = 0; i < points.Count; i++)
        {
            points[i].value = Vector2.Dot(points[i].point, normPerp);
        }
        points.Sort((p1, p2) => p1.value.CompareTo(p2.value));

        for (int i = 1; i < 3; i++)
        {
            if (points[i].ignore) continue;

            manifold.contactCount++;
            if (manifold.contactCount == 1)
            {
                ContactsFromPoint(points[i].point, points[i].isA, in manifold, margin, out manifold.contact1);
            }
            else
            {
                ContactsFromPoint(points[i].point, points[i].isA, in manifold, margin, out manifold.contact2);
            }
        }
    }

    static private CablePinchManifold CircleCircleIntersection(CirclePulley s1, CirclePulley s2, float margin)
    {
        CablePinchManifold result = new CablePinchManifold();

        Vector2 distanceVector = s2.PulleyCentreGeometrical - s1.PulleyCentreGeometrical;
        float distance = distanceVector.magnitude;
        float d = s1.Radius + s2.Radius + margin - distance;

        if (d <= 0.0f)
        {
            result.hasContact = false;
            return result;
        }

        result.hasContact = true;
        result.bodyA = s1;
        result.bodyB = s2;
        
        result.depth = d;
        result.normal = distanceVector / distance;

        result.contactCount = 1;
        result.contact1.A = s1.FurthestPoint(result.normal);
        result.contact1.B = s2.FurthestPoint(-result.normal);
        return result;
    }

    static public bool AABBMarginCheck(Bounds aabb1, Bounds aabb2, float margin)
    {
        float dx = aabb1.center.x - aabb2.center.x;
        float px = aabb1.extents.x + aabb2.extents.x + margin - Mathf.Abs(dx);
        if (px <= 0.0f)
        {
            return false;
        }

        float dy = aabb1.center.y - aabb2.center.y;
        float py = aabb1.extents.y + aabb2.extents.y + margin - Mathf.Abs(dx);
        if (py <= 0.0f)
        {
            return false;
        }

        return true;
    }

    static Vector2 CableSegmentFurthestPoint(CableRoot.Joint joint, CableRoot.Joint jointTail, Vector2 d)
    {
        float l = Vector2.Dot(joint.cableUnitVector, d);
        if (l < 0.0f)
        {
            return jointTail.tangentPointHead;
        }
        else
        {
            return joint.tangentPointTail;
        }
    }
    static SupportPoint CableSegmentSupport(CableRoot.Joint joint, CableRoot.Joint jointTail, CableMeshInterface body, Vector2 d)
    {
        SupportPoint support = new SupportPoint();
        support.A = CableSegmentFurthestPoint(joint, jointTail, d);
        support.B = body.FurthestPoint(-d);
        support.res = support.A - support.B;
        return support;
    }
    static public bool GJKCableSegmentIntersection(CableRoot cable, CableRoot.Joint joint, CableRoot.Joint jointTail, CableMeshInterface body)
    {
        Vector2 d = joint.cableUnitVector;

        List<SupportPoint> simplex = new List<SupportPoint>();
        simplex.Add(CableSegmentSupport(joint, jointTail, body, d));
        d = -simplex[0].res.normalized;

        while (true)
        {
            SupportPoint A = CableSegmentSupport(joint, jointTail, body, d);
            if (Vector2.Dot(A.res, d) < -cable.CableHalfWidth)
            {
                return false;
            }

            simplex.Add(A);
            if (HandleSimplex(ref simplex, ref d, cable.CableHalfWidth))
            {
                return CableSegmentEPA(simplex, cable, joint, jointTail, body);
            }
        }
    }

    static bool CableSegmentEPA(List<SupportPoint> polytope, CableRoot cable, CableRoot.Joint joint, CableRoot.Joint jointTail, CableMeshInterface body)
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
                Vector2 normal;

                if (AB == Vector2.zero)
                {
                    // needed for corner corner distance of polygons
                    normal = (polytope[i].B - polytope[i].A).normalized;
                }
                else
                {
                    normal = new Vector2(AB.y, -AB.x).normalized;
                    if (Vector2.Dot(AC, normal) > 0.0f)
                    {
                        normal *= -1;
                    }
                }

                float distance = Vector2.Dot(normal, A);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    minNormal = normal;
                    minIndex1 = i;
                    minIndex2 = j;
                }
            }

            SupportPoint support = CableSegmentSupport(joint, jointTail, body, minNormal);
            float sDistance = Vector2.Dot(minNormal, support.res);

            if (Mathf.Abs(sDistance - minDistance) > 0.001f)
            {
                minDistance = Mathf.Infinity;
                polytope.Insert(minIndex2, support);
            }
        }

        if (minDistance <= -cable.CableHalfWidth)
        {
            return false;
        }

        if (polytope[minIndex1].B == polytope[minIndex2].B)
        {
            Vector2 v1 = polytope[minIndex1].B - jointTail.tangentPointHead;
            float d1 = Vector2.Dot(v1, joint.cableUnitVector);
            if ((d1 < 0.0f) || (d1 > joint.currentLength)) return false;
        }
        else
        {
            Vector2 v1 = polytope[minIndex1].B - jointTail.tangentPointHead;
            Vector2 v2 = polytope[minIndex2].B - jointTail.tangentPointHead;
            float d1 = Vector2.Dot(v1, joint.cableUnitVector);
            float d2 = Vector2.Dot(v2, joint.cableUnitVector);
            if (((d1 < 0.0f) && (d2 < 0.0f)) || ((d1 > joint.currentLength) && (d2 > joint.currentLength))) return false;
        }

        return true;
    }
}
