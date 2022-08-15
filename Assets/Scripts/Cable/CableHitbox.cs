using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableHitbox : MonoBehaviour
{
    private CableJoint chainNode;

    [SerializeField] BoxCollider2D box;

    public CableJoint ChainNode
    {
        set { chainNode = value; }
    }

    public void Transmit(Collider2D hit)
    {
        //chainNode.NodeAdder(hit);
    }
}
