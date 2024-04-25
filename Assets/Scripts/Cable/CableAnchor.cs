using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableAnchor : CableBase
{
    public override Vector2 NodePosition { get { return this.transform.position; } }

    [SerializeField] Rigidbody2D rb2d = null;
    public override Rigidbody2D RB2D { get { return rb2d; } }

    [SerializeField] private CableBase startHead;

    public float cableWidth = 0.1f;

    [SerializeField] private float chainTriggerWidth = 0.5f;

    public bool CableSlipping = false;

    public float StaticFriction = 0.1f;

    public float KineticFriction = 0.05f;

    public uint solveIterations = 10;

    void Start()
    {
        if (rb2d == null) rb2d = GetComponent<Rigidbody2D>();

        this.anchor = this;

        linkType = LinkType.AnchorStart;

        this.head = startHead;

        startHead.anchor = this;

        head.linkType = LinkType.AnchorEnd;

        head.node.restLength = (this.transform.position - head.transform.position).magnitude;

        head.tail = this;

        head.node.Initilizebox(chainTriggerWidth);

        StartExtra();
    }

    protected virtual void StartExtra()
    {

    }

    public void CableUpdate()
    {
        if (head == null) return;

        ChainUpdate(head);
        ChainSlipUpdate(head);
    }

    public void CableSolve()
    {
        if (head == null) return;

        for (int i = 0; i < solveIterations; i++)
        {
            ChainSolve(head);
            ChainBalanceSolve(head);
        }
    }
}
