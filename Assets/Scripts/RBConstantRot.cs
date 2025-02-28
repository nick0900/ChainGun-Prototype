using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RBConstantRot : MonoBehaviour
{

    public float rotVel = 0.0f;
    Rigidbody2D rb2d = null;
    // Start is called before the first frame update
    void Start()
    {
        rb2d = GetComponent<Rigidbody2D>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        rb2d.angularVelocity = rotVel;
    }
}
