using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GunConnector : GraphConnector
{
    [SerializeField] private SliderJoint2D joint = null;

    private Transform parent = null;

    private void Awake()
    {
        if (rb2d == null)
        {
            rb2d = GetComponent<Rigidbody2D>();
        }
        if ( rb2d != joint.connectedBody)
        {
            joint.connectedBody = rb2d;
        }
        rb2d.useAutoMass = true;
        offset = joint.anchor;
        parent = this.transform.parent;
    }

    public override void ConnectTo(Rigidbody2D body)
    {
        Vector3 stretch = joint.transform.position + joint.transform.rotation * joint.anchor - (joint.connectedBody.transform.position + joint.transform.rotation * joint.connectedAnchor);

        joint.connectedBody = body;

        joint.anchor = Quaternion.Inverse(joint.transform.rotation) * (body.transform.position - joint.transform.position) + stretch.magnitude * Vector3.left;

        Destroy(rb2d);
        rb2d = null;
        this.transform.parent = body.transform;
    }

    public override void Restore()
    {
        if(rb2d == null)
        {
            rb2d = gameObject.AddComponent<Rigidbody2D>();
            rb2d.useAutoMass = true;
        }
        
        joint.connectedBody = rb2d;
        this.transform.parent = parent;
        joint.anchor = offset;
    }
}
