using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class copyPos : MonoBehaviour
{
    public Transform otherT;

    public bool copyX = true;
    public bool copyY = true;
    public bool copyZ = true;

    // Update is called once per frame
    void Update()
    {
        if (otherT != null)
        {
            Vector3 temp = this.transform.position;
            if (copyX)
                temp.x = otherT.position.x;
            if (copyY)
                temp.y = otherT.position.y;
            if (copyZ)
                temp.z = otherT.position.z;
            this.transform.position = temp;
        }
    }
}
