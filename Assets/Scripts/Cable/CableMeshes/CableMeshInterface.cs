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

    public struct cableIntersection
    {
        public bool intersecting;
        public float distance;
        public Vector2 normal;
    }

    static private Vector2 Support(CableMeshInterface s1, CableMeshInterface s2, Vector2 d)
    {
        return s1.FurthestPoint(d) - s2.FurthestPoint(-d);
    }

    static private Vector2 TripleProd(Vector3 v1, Vector3 v2, Vector3 v3)
    {
        return Vector3.Cross(Vector3.Cross(v1, v2), v3);
    }

    static private bool HandleSimplex(ref List<Vector2> simplex, ref Vector2 d)
    {
        if (simplex.Count == 2)
        {
            Vector2 A = simplex[1];
            Vector2 B = simplex[0];

            Vector2 AB = B - A;
            Vector2 AO = -A;
            d = TripleProd(AB, AO, AB).normalized;
            return false;
        }
        {
            Vector2 A = simplex[2];
            Vector2 B = simplex[1];
            Vector2 C = simplex[0];

            Vector2 AB = B - A;
            Vector2 AC = C - A;
            Vector2 AO = -A;
            Vector2 ABperp = TripleProd(AC, AB, AB);
            Vector2 ACperp = TripleProd(AB, AC, AC);

            if (Vector2.Dot(ABperp, AO) > 0)
            {
                d = ABperp.normalized;
                simplex.RemoveAt(0);
                return false;
            }
            else if (Vector2.Dot(ACperp, AO) > 0)
            {
                d = ACperp.normalized;
                simplex.RemoveAt(1);
                return false;
            }
        }
        return true;
    }

    static public cableIntersection GJKIntersection(CableMeshInterface s1, CableMeshInterface s2)
    {
        cableIntersection result = new cableIntersection();

        Vector2 d = (s2.PulleyCentreGeometrical - s1.PulleyCentreGeometrical).normalized;

        List<Vector2> simplex = new List<Vector2>();
        simplex.Add(Support(s1, s2, d));
        d = -simplex[0].normalized;

        while (true)
        {
            Vector2 A = Support(s1, s2, d);
            if (Vector2.Dot(A, d) < 0)
            {
                result.intersecting = false;
                return result;
            }
            
            simplex.Add(A);
            if (HandleSimplex(ref simplex, ref d))
            {
                result.intersecting = true;
                return result;
            }

        }
    }
}
