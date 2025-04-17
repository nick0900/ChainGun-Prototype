using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class rotateTransform : MonoBehaviour
{
    public float rotationSpeed = 10.0f;
    Transform t = null;
    // Start is called before the first frame update
    void Start()
    {
        t = GetComponent<Transform>();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        Vector3 euler = t.eulerAngles;
        euler.z += rotationSpeed * Time.fixedDeltaTime;
        t.eulerAngles = euler;
    }
}
