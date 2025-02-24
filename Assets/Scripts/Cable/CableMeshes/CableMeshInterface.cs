using System.Collections;
using System.Collections.Generic;
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
}
