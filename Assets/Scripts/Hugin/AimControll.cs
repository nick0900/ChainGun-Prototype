using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AimControll : MonoBehaviour
{
    public float stretchRest = 0.3f;

    public float stretchDistance = 0.7f;

    public float stretchSpeed = 5;

    public float angularSpeed = 5;

    //initiate direction
    Vector2 direction = new Vector2(0, 0);
    //initiate stretch
    float stretch = 0;

    //component
    SliderJoint2D sj2d;

    JointMotor2D sm2d;

    JointTranslationLimits2D sl2d;

    HingeJoint2D hj2d;

    JointMotor2D hm2d;

    [SerializeField] Animator huginAnim;

    [SerializeField] Transform gun;

    int joyHorizontalHash;

    int joyVerticalHash;

    // Input on x (Horizontal)
    float hAim = 0;

    // Input on z (Vertical)
    float vAim = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Grab our components
        sj2d = GetComponent<SliderJoint2D>();
        sm2d = sj2d.motor;
        sl2d = sj2d.limits;
        sl2d.max = stretchRest + stretchDistance;
        sl2d.min = stretchRest;
        sj2d.limits = sl2d;

        hj2d = GetComponent<HingeJoint2D>();
        hm2d = hj2d.motor;

        joyHorizontalHash = Animator.StringToHash("horizontal");
        joyVerticalHash = Animator.StringToHash("vertical");

        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        switch (GlobalSettings.GS.aimInputMode)
        {
            case GlobalSettings.AimInput.Joystick:
                // Input on x (Horizontal)
                hAim = Input.GetAxis("JoyAim horizontal");

                // Input on y (Vertical)
                vAim = Input.GetAxis("JoyAim vertical");
                break;

            case GlobalSettings.AimInput.GrossMouse:
                // Input on x (Horizontal)
                hAim -= Input.GetAxis("MouseAim horizontal") * GlobalSettings.GS.grossMouseAimHorizontalSensitivity;

                // Input on y (Vertical)
                vAim += Input.GetAxis("MouseAim vertical") * GlobalSettings.GS.grossMouseAimVerticalSensitivity;
                break;

            case GlobalSettings.AimInput.Mouse:
                // Input on x (Horizontal)
                hAim = Input.GetAxis("MouseAim horizontal") * GlobalSettings.GS.mouseSensitivity;

                // Input on y (Vertical)
                vAim = Input.GetAxis("MouseAim vertical") * GlobalSettings.GS.mouseSensitivity;
                break;
        }

        AnimUpdate();
    }

    void AnimUpdate()
    {
        Vector2 gunPosition = gun.position - this.transform.position;

        huginAnim.SetFloat(joyHorizontalHash, gunPosition.x);
        huginAnim.SetFloat(joyVerticalHash, gunPosition.y);
    }

    void FixedUpdate()
    {
        switch (GlobalSettings.GS.aimInputMode)
        {
            case GlobalSettings.AimInput.Joystick:
                if (Mathf.Sqrt(hAim * hAim + vAim * vAim) > 0.1)
                {
                    direction = new Vector2(hAim, vAim);
                }
                stretch = direction.magnitude;
                if (hAim == 0 && vAim == 0)
                {
                    stretch = 0;
                    //direction = new Vector2(Mathf.Cos(Mathf.Rad2Deg * transform.localEulerAngles.z), Mathf.Sin(Mathf.Rad2Deg * transform.localEulerAngles.z));
                }
                if (stretch > 1) stretch = 1;
                break;

            case GlobalSettings.AimInput.GrossMouse:
                direction = Quaternion.Euler(0.0f, 0.0f, hAim) * Vector3.right;

                if (vAim > 1) vAim = 1;
                if (vAim < 0) vAim = 0;

                stretch = vAim;
                break;

            case GlobalSettings.AimInput.Mouse:
                direction += new Vector2(hAim, vAim);

                stretch = direction.magnitude;
                if (stretch > 1)
                {
                    direction.Normalize();
                    stretch = 1.0f;
                }
                break;
        }

        Vector3 stretchVector = this.transform.position + this.transform.rotation * (Vector3)sj2d.anchor - (sj2d.connectedBody.transform.position + this.transform.rotation * (Vector3)sj2d.connectedAnchor);

        sm2d.motorSpeed = Mathf.Sin((stretchRest + stretchDistance * stretch - stretchVector.magnitude) / (stretchRest + stretchDistance) * Mathf.PI / 2) * stretchSpeed;

        float directionAngle = Mathf.Rad2Deg * Mathf.Atan2(direction.y, direction.x);
        if (directionAngle < 0)
        {
            directionAngle = directionAngle + 360;
        }

        hm2d.motorSpeed = Mathf.Sin(-Mathf.DeltaAngle(transform.localEulerAngles.z, directionAngle) * Mathf.PI / 360) * angularSpeed;
        
        sj2d.motor = sm2d;
        hj2d.motor = hm2d;
    }
}