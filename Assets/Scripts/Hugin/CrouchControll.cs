using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CrouchControll : MonoBehaviour
{
    //components
    SliderJoint2D sj2d;

    JointMotor2D jm2d;


    [SerializeField] Animator huginAnim;

    [SerializeField] Transform player;

    int heightHash;


    public float stretchDistance = 0.5f;

    public float restHeight = 0.5f;

    public float stretchSpeed = 2;

    public float tollerance = 0.01f;

    // Input on x (Horizontal)
    float vAxis = 0;

    // Start is called before the first frame update
    void Start()
    {
        // Grab our components
        sj2d = GetComponent<SliderJoint2D>();
        jm2d = sj2d.motor;

        heightHash = Animator.StringToHash("height");
    }

    // Update is called once per frame
    void Update()
    {
        // Aquire inputs;
        vAxis = Input.GetAxis("Vertical");

        AnimUpdate();
    }

    void AnimUpdate()
    {
        Vector2 crouchPosition = this.transform.position - player.position;

        huginAnim.SetFloat(heightHash, crouchPosition.y);
    }

    // Update is called per fixed delta time
    private void FixedUpdate()
    {
        float distance = (restHeight + stretchDistance * vAxis - transform.localPosition.y) / (restHeight + stretchDistance);

        jm2d.motorSpeed = Mathf.Sin(distance*Mathf.PI/2) * stretchSpeed;

        sj2d.motor = jm2d;
    }
}
