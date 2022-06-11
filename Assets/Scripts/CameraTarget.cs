using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraTarget : MonoBehaviour
{

    //camera distance z
    public float cameraDistZ = 6;

    public float cameraDistY = 1.3f;

    Vector3 cameraPos;

    // Update is called once per frame
    void Update()
    {
        // grab the camera position
        cameraPos = Camera.main.transform.position;

        // modify it's position according to cameraDistZ
        cameraPos.z = transform.position.z - cameraDistZ;
        cameraPos.x = transform.position.x;
        cameraPos.y = transform.position.y + cameraDistY;

        // set the camera position
        Camera.main.transform.position = cameraPos;
    }
}
