using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainMeshDynamic : ChainMesh
{
    public Vector2 prevPos;
    public Quaternion prevRot;

    private void Awake()
    {
        prevPos = this.transform.position;
        prevRot = this.transform.rotation;
    }
    private void FixedUpdate()
    {
        Record();
    }
    IEnumerator Record()
    {
        yield return new WaitForFixedUpdate();
        prevPos = this.transform.position;
        prevRot = this.transform.rotation;
    }
}
