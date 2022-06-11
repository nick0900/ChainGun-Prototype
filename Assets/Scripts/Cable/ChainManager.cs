using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class ChainManager : MonoBehaviour
{
    protected ChainManager head = null;

    protected ChainManager tail = null;

    [HideInInspector] public NodeFunctionality node = null;

    [HideInInspector] static public float bias = 0.2f;

    public Rigidbody2D rb2d;

    [HideInInspector] public float invMass;

    [HideInInspector] public Matrix4x4 invInertiaTensor;

    [HideInInspector] public Vector3 impulseRadiusTail;

    [HideInInspector] public Vector3 impulseRadiusHead;

    [HideInInspector] public float storedLength;

    [HideInInspector] public Vector2 tangentOffsetHead;

    [HideInInspector] public Vector2 tangentOffsetTail;

    [HideInInspector] public Vector2 prevTangentHead;

    [HideInInspector] public Vector2 prevTangentTail;

    [HideInInspector] public Vector2 prevPos;

    public enum LinkType
    {
        Rolling,
        AnchorStart,
        AnchorEnd
    }

    public LinkType linkType;

    public void ChainUpdate(ChainManager start)
    {
        start.node.CableSegmentUpdate();
        if (start.head != null)
        {
            ChainUpdate(start.head);
        }
    }
    public void ChainSolve(ChainManager start)
    {
        start.node.CableSegmentSolve();
        if (start.head != null)
        {
            ChainSolve(start.head);
        }
    }

    public void PositionSave(ChainManager start)
    {
        prevPos = this.transform.position;
        prevTangentHead = this.tangentOffsetHead;
        prevTangentTail = this.tangentOffsetTail;
        
        if (start.head != null)
        {
            ChainSolve(start.head);
        }
    }

    public void ChainDestroy(ChainManager start)
    {
        if (start.head != null)
        {
            ChainDestroy(start.head);
        }
        Destroy(start.gameObject);
    }

    public ChainManager GetHead()
    {
        return head;
    }

    public ChainManager GetTail()
    {
        return tail;
    }

    public void AssignHead(ChainManager head)
    {
        this.head = head;
    }

    public void AssignTail(ChainManager tail)
    {
        this.tail = tail;
    }

    public void AddFront(ChainManager newNode)
    {
        newNode.AssignHead(head);

        head.AssignTail(newNode);

        newNode.AssignTail(this);

        AssignHead(newNode);
    }

    public void AddBack(ChainManager newNode)
    {
        newNode.AssignTail(tail);

        tail.AssignHead(newNode);

        newNode.AssignHead(this);

        AssignTail(newNode);
    }

    public void CutChain()
    {
        head.GetComponent<ChainManager>().AssignTail(tail);

        tail.GetComponent<ChainManager>().AssignHead(head);
    }
}
