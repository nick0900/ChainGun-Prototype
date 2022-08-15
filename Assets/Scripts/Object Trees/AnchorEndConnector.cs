using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnchorEndConnector : GraphConnector
{
    [SerializeField] private CableJoint chain = null;

    private Transform parent = null;

    private void Awake()
    {
        if (rb2d == null)
        {
            rb2d = GetComponent<Rigidbody2D>();
        }
        if (chain == null)
        {
            chain = GetComponent<CableJoint>();
        }
        rb2d.useAutoMass = true;
        //offset = Vector2.zero;
        parent = this.transform.parent;
    }

    public override void ConnectTo(Rigidbody2D body)
    {
        chain.rb2d = body;

        Destroy(rb2d);
        rb2d = null;
        this.transform.parent = body.transform;
    }

    public override void Restore()
    {
        if (rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            rb2d.useAutoMass = true;
        }

        chain.rb2d = rb2d;
        this.transform.parent = parent;
    }
}
