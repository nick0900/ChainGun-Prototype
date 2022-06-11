﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour {

    //walking spead
    public float walkSpeed;

    //jumping speed
    public float jumpForce;

    //walljumping speed
    public float walljumpForce;

    //camera distance z
    public float cameraDistZ = 6;

    public float cameraDistY = 6;

    //Rigidbody component
    Rigidbody rb;

    //Collider component
    Collider col;

    //flag to keep track of key pressing for jumping
    bool pressedJump = false;

    //size of the player
    Vector3 size;

    //y that represent that you fell
    float minY = -1.5f;

    // Use this for initialization
    void Start () {
        // Grab our components
        rb = GetComponent<Rigidbody>();
        col = GetComponent<Collider>();

        // get player size
        size = col.bounds.size;

        // set the camera position
        CameraFollowPlayer();
    }
	
	// Update is called once per frame
	void FixedUpdate () {
        WalkHandler();
        JumpHandler();
        CameraFollowPlayer();
        FallHandler();
    }

    // check if the player fell
    void FallHandler()
    {
        if(transform.position.y <= minY)
        {
            // Game over!
            GameManager.instance.GameOver();
        }
    }

    // Takes care of the walking logic
    void WalkHandler()
    {
        // Input on x (Horizontal)
        float hAxis = Input.GetAxis("Horizontal");

        // Input on z (Vertical)
        float vAxis = Input.GetAxis("Vertical");

        

        // Check that we are moving
        if(hAxis != 0 || vAxis != 0)
        {
            // Movement vector
            Vector3 movement = new Vector3(hAxis * walkSpeed * Time.deltaTime, 0, 0);

            // Calculate the new position
            Vector3 newPos = transform.position + movement;

            // Move
            rb.MovePosition(newPos);

            // Movement direction
            //Vector3 direction = new Vector3(hAxis, 0, vAxis);

            // option 1: modify the transform
            //transform.forward = direction;

            // option 2: using our rigid body
            //rb.rotation = Quaternion.LookRotation(direction);
        }
    }

    // takes care of the jumping logic
    void JumpHandler()
    {
        // Input on the Jump axis
        float jAxis = Input.GetAxis("Jump");

        if (CheckWalled()) print("Walled"); 

        // If the key has been pressed
        if(jAxis > 0)
        {
            bool isGrounded = CheckGrounded();
            bool isWalled = CheckWalled();

            //make sure we are not already jumping
            if (!pressedJump && isGrounded)
            {
                pressedJump = true;

                //jumping vector
                Vector3 jumpVector = new Vector3(0, jAxis * jumpForce, 0);

                //apply force
                rb.AddForce(jumpVector, ForceMode.VelocityChange);
            }

            //make sure we are not already jumping
            else if (!pressedJump && isWalled)
            {
                pressedJump = true;

                //jumping vector
                Vector3 jumpVector = new Vector3(0, walljumpForce, 0);

                //apply force
                rb.AddForce(jumpVector, ForceMode.VelocityChange);

                print ("Wall!");
            }


        }
        else
        {
            //set flag to false
            pressedJump = false;
        }
    }

    // will check if the player is touching the ground
    bool CheckGrounded()
    {
        // location of all 4 corners
        Vector3 corner1 = transform.position + new Vector3(size.x / 2, -size.y / 2 + 0.01f, size.z / 2);
        Vector3 corner2 = transform.position + new Vector3(-size.x / 2, -size.y / 2 + 0.01f, size.z / 2);
        Vector3 corner3 = transform.position + new Vector3(size.x / 2, -size.y / 2 + 0.01f, -size.z / 2);
        Vector3 corner4 = transform.position + new Vector3(-size.x / 2, -size.y / 2 + 0.01f, -size.z / 2);

        // check if we are grounded
        bool grounded1 = Physics.Raycast(corner1, -Vector3.up, 0.01f);
        bool grounded2 = Physics.Raycast(corner2, -Vector3.up, 0.01f);
        bool grounded3 = Physics.Raycast(corner3, -Vector3.up, 0.01f);
        bool grounded4 = Physics.Raycast(corner4, -Vector3.up, 0.01f);

        return (grounded1 || grounded2 || grounded3 || grounded4);
    }

    bool CheckWalled()
    {
        // location of all left and right side of player
        Vector3 sideleft = transform.position + new Vector3(-size.x / 2, 0, 0);
        Vector3 sideright = transform.position + new Vector3(size.x / 2, 0, 0);

        // check if we are walled
        bool walled1 = Physics.Raycast(sideleft, Vector3.left, 0.03f);
        bool walled2 = Physics.Raycast(sideright, Vector3.right, 0.03f);


        return (walled1 || walled2);
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Coin"))
        {
            // Increase our score
            GameManager.instance.IncreaseScore(1);

            // Destroy coin
            Destroy(other.gameObject);
        }
        else if(other.CompareTag("Enemy"))
        {
            // Game over!
            GameManager.instance.GameOver();
        }
        else if (other.CompareTag("Goal"))
        {
            // Send player to the next level
            GameManager.instance.IncreaseLevel();
        }
    }

    void CameraFollowPlayer()
    {
        // grab the camera position
        Vector3 cameraPos = Camera.main.transform.position;

        // modify it's position according to cameraDistZ
        cameraPos.z = transform.position.z - cameraDistZ;
        cameraPos.x = transform.position.x;
        cameraPos.y = transform.position.y + cameraDistY;

        // set the camera position
        Camera.main.transform.position = cameraPos;
    }
}
