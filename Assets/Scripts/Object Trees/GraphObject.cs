using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GraphObject : MonoBehaviour
{
    //-------VIKTIGT--------//
    // Om ett graphobjekt har flera stickpoints måste det garanteras att två av dem ej fäster sig på samma föremål.
    // Implementera detta i en derived class

    [SerializeField] private Transform body;

    private Rigidbody2D rb2d;

    [SerializeField] private GraphConnector graphConnector = null;

    private Vector2 connectorOffset;

    [HideInInspector] public Rigidbody2D root = null;


    private void Awake()
    {
        rb2d = this.GetComponent<Rigidbody2D>();

        if (graphConnector != null)
        {
            ConnectObject(graphConnector);
        }

        AdditionalSetup();
    }

    virtual protected void AdditionalSetup() { }

    private void Start()
    {
        GraphManager.GM.ObjectAdd(this);
    }

    private void OnDestroy()
    {
        if (GraphManager.GM != null)
        {
            GraphManager.GM.ObjektRemove(this);
        }
    }

    public bool Frozen
    {
        get { return this.transform.parent == body; }
    }

    public void Freeze()
    {
        if (!Frozen)
        {
            body.parent = null;
            this.transform.parent = body;
            rb2d.simulated = false;
        }
    }

    public void Unfreeze()
    {
        if (Frozen)
        {
            this.transform.parent = null;
            rb2d.simulated = true;
            body.parent = this.transform;
        }
    }

    public void Attach(Rigidbody2D root)
    {
        this.root = root;

        Freeze();

        body.parent = root.transform;

        if (graphConnector != null)
        {
            graphConnector.ConnectTo(root);
        }
    }

    public void Release()
    {
        Vector2 velocity = Vector2.zero;
        float angularVel = 0;
        if (root != null)
        {
            velocity = root.velocity;
            angularVel = root.angularVelocity;
            this.root = null;
        }
        
        if (graphConnector != null)
        {
            Freeze();
            body.parent = graphConnector.transform;
            this.transform.position = graphConnector.transform.position + (Vector3)connectorOffset;
            graphConnector.Restore();
        }
        else
        {
            Unfreeze();
        }
        rb2d.velocity = velocity;
        rb2d.angularVelocity = angularVel;
    }

    public void ConnectObject(GraphConnector connector)
    {
        if (graphConnector != null)
        {
            graphConnector.Restore();
        }
        if (connector != null)
        {
            connectorOffset = this.transform.position - connector.transform.position;
            Freeze();
            body.parent = connector.transform;

            if (root != null)
            {
                connector.ConnectTo(root);
            }
        }
        else
        {
            if (root == null)
            {
                Vector2 velocity = graphConnector.Body.velocity;
                float angularVel = graphConnector.Body.angularVelocity;
                
                Unfreeze();

                rb2d.velocity = velocity;
                rb2d.angularVelocity = angularVel;
            }
        }
        this.graphConnector = connector;
    }

    virtual public void Unstick() { }

    virtual public void StickEvent() { }

    virtual public void UnstickEvent() { }

    virtual public void StickToggleEvent(bool isSticky) { }
}
