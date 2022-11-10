using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
abstract public class CableMeshInterface : CableMeshGeneration
{
    protected Vector2 previousPosition1 = Vector2.zero;
    protected Vector2 previousPosition2 = Vector2.zero;

    public Vector2 PreviousPosition(bool alternator)
    {

    }

    bool InternalAlternator = false;

    private void FixedUpdate()
    {
        previousPosition2 = previousPosition1;
        previousPosition1 = PulleyCentreGeometrical;

        InternalAlternator = !InternalAlternator;
    }

    public enum CMPrimitives
    {
        Point,
        Circle,
        polygon
    }

    //global position of pulley geometrical centre
    abstract public Vector2 PulleyCentreGeometrical { get; }

    //transformation from a local point on the pulley to world space
    abstract protected Vector2 PulleyToWorldTransform(Vector2 point);

    //attached rigid body
    abstract public Rigidbody2D PulleyAttachedRigidBody { get; }


    abstract public CMPrimitives ChainMeshPrimitiveType { get; }

    //Considering the cable direction going from tail to head, a true orientation will have the cable wrapping counter-clockwise
    //The orientation is calculated throught the relative movement of the cable to the pulley. Only call function at first collision and save that orientation
    //most accurate when called during a collision hit after the fixed update is done
    abstract public bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious);

    // will return the global tangent offset from the pulley center. the width of the cable is taken into consideration meaning the tangent point is in the middle of the cable
    abstract public Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float cableWidth, out int vertex);

    abstract public float ShapeSurfaceDistance(Vector2 prevTangent, int prevVertex, Vector2 currentTangent, int currentVertex, bool orientation);

    abstract public void CreateChainCollider(float cableWidth);
}
