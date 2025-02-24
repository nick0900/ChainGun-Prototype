using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class CableBase : MonoBehaviour
{
    [HideInInspector] public CableBase head = null;

    [HideInInspector] public CableBase tail = null;

    [HideInInspector] public CableJoint node = null;

    [HideInInspector] public CableAnchor anchor = null;

    //bias for adjusting position correction
    [HideInInspector] static public float bias = 0.2f;

    //the rigid body the cable constraint will affect
    abstract public Rigidbody2D RB2D { get; }

    //the total cable stored in object
    [HideInInspector] public float storedLength;

    //tangent offset on this pulley towards the head joint
    [HideInInspector] public Vector2 tangentOffsetHead;

    //tangent offset on this pulley towards the tail joint
    [HideInInspector] public Vector2 tangentOffsetTail;


    //Previous global position of the cable joint head position
    [HideInInspector] public Vector2 prevCableThisPosition;

    //Previous global position of the cable joint tail position
    [HideInInspector] public Vector2 prevCableTailPosition;

    abstract public Vector2 NodePosition { get; }
    public Vector2 CableEndPosition { get { return tangentOffsetTail + NodePosition; } }
    public Vector2 CableStartPosition { get { return tail != null ?  tail.tangentOffsetHead + tail.NodePosition : tangentOffsetHead + NodePosition; } }

    public float CableWidth { get { return anchor != null ? anchor.cableWidth : 0.01f; } }

    public bool DoSlipSimulation { get { return anchor != null ? anchor.CableSlipping : false; } }

    public enum LinkType
    {
        Rolling,
        AnchorStart,
        AnchorEnd
    }

    public LinkType linkType;

    public void ChainUpdate(CableBase start)
    {
        start.node.CableSegmentPreSlipUpdate();
        if (start.head != null)
        {
            ChainUpdate(start.head);
        }
    }

    public void ConstraintUpdate(CableBase start)
    {
        ChainSlipUpdateRec(start);
        ChainConstraintUpdateRec(start, null, 0);
    }
    void ChainSlipUpdateRec(CableBase start)
    {
        start.node.CableSlipConditionsUpdate();
        if (start.head != null)
        {
            ChainSlipUpdateRec(start.head);
        }
    }

    void ChainConstraintUpdateRec(CableBase start, CableBase slippingNodesStart, int slippingCount)
    {
        start.node.CableConstraintsInitialization(ref slippingNodesStart, ref slippingCount);
        if (start.head != null)
        {
            ChainConstraintUpdateRec(start.head, slippingNodesStart, slippingCount);
        }
    }

    public void ChainDestroy(CableBase start)
    {
        if (start.head != null)
        {
            ChainDestroy(start.head);
        }
        Destroy(start.gameObject);
    }

    public void ChainRecordPositions(CableBase start)
    {
        start.prevCableThisPosition = start.CableEndPosition;
        start.prevCableTailPosition = start.CableStartPosition;

        if (start.head != null)
        {
            ChainRecordPositions(start.head);
        }
    }

    public void AddFront(CableBase newNode)
    {
        newNode.head = this.head;

        head.tail =newNode;

        newNode.tail = this;

        this.head = newNode;
    }

    public void AddBack(CableBase newNode)
    {
        newNode.tail = this.tail;

        tail.head = newNode;

        newNode.head = this;

        this.tail = newNode;
    }

    public void CutChain()
    {
        head.tail = tail;

        tail.head = head;
    }
}
