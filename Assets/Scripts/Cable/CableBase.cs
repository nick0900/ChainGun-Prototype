using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class CableBase : MonoBehaviour
{
    protected CableBase head = null;

    protected CableBase tail = null;

    [HideInInspector] public CableJoint node = null;

    [HideInInspector] static public float bias = 0.2f;

    public Rigidbody2D rb2d;

    [HideInInspector] public float invMass;

    [HideInInspector] public Matrix4x4 invInertiaTensor;

    [HideInInspector] public Vector3 impulseRadiusTail;

    [HideInInspector] public Vector3 impulseRadiusHead;

    [HideInInspector] public float storedLength;

    [HideInInspector] public Vector2 tangentOffsetHead;

    [HideInInspector] public Vector2 tangentOffsetTail;

    public enum LinkType
    {
        Rolling,
        AnchorStart,
        AnchorEnd
    }

    public LinkType linkType;

    public void ChainUpdate(CableBase start)
    {
        start.node.CableSegmentUpdate();
        if (start.head != null)
        {
            ChainUpdate(start.head);
        }
    }
    public void ChainSolve(CableBase start)
    {
        start.node.CableSegmentSolve();
        if (start.head != null)
        {
            ChainSolve(start.head);
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

    public CableBase GetHead()
    {
        return head;
    }

    public CableBase GetTail()
    {
        return tail;
    }

    public void AssignHead(CableBase head)
    {
        this.head = head;
    }

    public void AssignTail(CableBase tail)
    {
        this.tail = tail;
    }

    public void AddFront(CableBase newNode)
    {
        newNode.AssignHead(head);

        head.AssignTail(newNode);

        newNode.AssignTail(this);

        AssignHead(newNode);
    }

    public void AddBack(CableBase newNode)
    {
        newNode.AssignTail(tail);

        tail.AssignHead(newNode);

        newNode.AssignHead(this);

        AssignTail(newNode);
    }

    public void CutChain()
    {
        head.GetComponent<CableBase>().AssignTail(tail);

        tail.GetComponent<CableBase>().AssignHead(head);
    }
}
