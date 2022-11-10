using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableAnchor : CableBase
{
    [SerializeField] private CableBase startHead;

    [SerializeField] private float chainWidth = 0.1f;

    [SerializeField] private float chainTriggerWidth = 0.5f;

    void Start()
    {
        linkType = LinkType.AnchorStart;

        this.AssignHead(startHead);

        head.linkType = LinkType.AnchorEnd;

        head.node.chainWidth = chainWidth;

        head.node.restLength = (this.transform.position - head.transform.position).magnitude;

        head.AssignTail(this);

        head.node.Initilizebox(chainTriggerWidth);

        StartExtra();
    }

    protected virtual void StartExtra()
    {

    }

    private void FixedUpdate()
    {
        if (head == null) return;

        ChainUpdate(head);

        /*
        for (int i = 0; i < 10; i++)
        {
            ChainSolve(head);
        }
        */
    }

    private void OnAnimatorIK(int layerIndex)
    {
        print("bruh");
    }
}
