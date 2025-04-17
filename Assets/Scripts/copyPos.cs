using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class copyPos : MonoBehaviour
{
    public Transform otherT;

    // Update is called once per frame
    void Update()
    {
        this.transform.position = otherT.position;
    }
}
