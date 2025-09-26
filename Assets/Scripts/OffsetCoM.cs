using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OffsetCoM : MonoBehaviour
{
    public Vector2 Offset;
    private void Start()
    {
        this.GetComponent<Rigidbody2D>().centerOfMass = Offset;
    }
}
