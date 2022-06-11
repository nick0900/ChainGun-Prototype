using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainTrigger : MonoBehaviour
{
    private NodeFunctionality chainNode;

    [SerializeField] BoxCollider2D box;

    public NodeFunctionality ChainNode
    {
        set { chainNode = value; }
    }

    public void Transmit(Collider2D hit)
    {
        //chainNode.NodeAdder(hit);
    }
}
