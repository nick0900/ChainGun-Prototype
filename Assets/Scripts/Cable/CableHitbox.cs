using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableHitbox : MonoBehaviour
{
    private CableJoint cableNode;

    [SerializeField] BoxCollider2D box;

    public CableJoint CableNode
    {
        set { cableNode = value; }
    }
}
