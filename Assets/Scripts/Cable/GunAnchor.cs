using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunAnchor : ChainAnchor
{
    [SerializeField] float startLength = 2;

    protected override void StartExtra()
    {
        head.node.restLength = startLength;
    }
}
