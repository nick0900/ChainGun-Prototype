using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GlobalSettings : MonoBehaviour
{
    public enum AimInput
    {
        Joystick,
        GrossMouse,
        Mouse
    }

    public AimInput aimInputMode = AimInput.Joystick;

    public float grossMouseAimHorizontalSensitivity = 1.0f;
    public float grossMouseAimVerticalSensitivity = 1.0f;

    public float mouseSensitivity = 1.0f;

    // static instance of the GS can be accessed from anywhere
    public static GlobalSettings GS;

    void Awake()
    {
        // check that it exists
        if (GS == null)
        {
            //assign it to the current object
            GS = this;
        }

        // make sure that it is equal to the current object
        else if (GS != this)
        {
            // destroy the current game object - we only need 1 and we already have it!
            Destroy(this.gameObject);
        }

        // don't destroy this object when changing scenes!
        DontDestroyOnLoad(gameObject);
    }
}
