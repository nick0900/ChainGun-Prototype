using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class GraphConnector : MonoBehaviour
{
    [SerializeField] protected Rigidbody2D rb2d;

    public Rigidbody2D Body
    {
        get { return rb2d; }
    }

    protected Vector2 offset;

    abstract public void ConnectTo(Rigidbody2D body);

    abstract public void Restore();
}
