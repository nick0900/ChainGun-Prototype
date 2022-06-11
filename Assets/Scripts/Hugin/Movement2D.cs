using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Movement2D : MonoBehaviour
{

    //walking spead
    public float walkSpeed;

    //jumping speed
    public float jumpForce;

    //walljumping speed
    public float walljumpForce;

    //Rigidbody component
    Rigidbody2D rb2d;

    //Collider component
    BoxCollider2D col;

    // Input on x (Horizontal)
    float hAxis = 0;

    // vector for movement
    Vector2 moveVariable = new Vector2(0, 0);

    //flag to keep track of key pressing for jumping
    bool pressedJump = false;

    //size of the player
    Vector2 size;

    //y that represent that you fell
    float minY = -1.5f;




    [SerializeField] Animator huginAnim;

    int velocityHash;




    // Use this for initialization
    void Start()
    {
        // Grab our components
        rb2d = GetComponent<Rigidbody2D>();
        col = GetComponent<BoxCollider2D>();

        // get player size
        size = col.bounds.size;

        velocityHash = Animator.StringToHash("velocity");
    }

    // Update is called once per frame
    private void Update()
    {
        // Aquire inputs;
        hAxis = Input.GetAxis("Horizontal");

        AnimUpdate();
    }

    void AnimUpdate()
    {
        huginAnim.SetFloat(velocityHash, rb2d.velocity.x);
    }

    // Update is called per fixed delta time
    void FixedUpdate()
    {
        // Check that we are moving
        if (hAxis != 0)
        {
            WalkHandler();
        }
        JumpHandler();
        FallHandler();
    }

    // check if the player fell
    void FallHandler()
    {
        if (transform.position.y <= minY)
        {
            // Game over!
            GameManager.instance.GameOver();
        }
    }

    // Takes care of the walking logic
    void WalkHandler()
    {
        moveVariable = new Vector2((walkSpeed * hAxis - rb2d.velocity.x), 0);

        rb2d.AddForce(moveVariable, ForceMode2D.Impulse);
    }

    // takes care of the jumping logic
    void JumpHandler()
    {
        // Input on the Jump axis
        float jAxis = Input.GetAxis("Jump");

        

        // If the key has been pressed
        if (jAxis > 0)
        {
            bool isGrounded = CheckGrounded();

            //make sure we are not already jumping
            if (!pressedJump && isGrounded)
            {
                pressedJump = true;

                //jumping vector
                Vector2 jumpVector = new Vector2(0, jumpForce);

                //apply force
                rb2d.AddForce(jumpVector, ForceMode2D.Impulse);
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
        // location of 2 corners
        Vector2 corner1 = transform.position + new Vector3(size.x / 2, -size.y / 2 + 0.01f);
        Vector2 corner2 = transform.position + new Vector3(-size.x / 2, -size.y / 2 + 0.01f);

        // check if we are grounded
        bool grounded1 = Physics2D.Raycast(corner1, -Vector2.up, 0.01f);
        bool grounded2 = Physics2D.Raycast(corner2, -Vector2.up, 0.01f);

        return (grounded1 || grounded2);
    }

    /*
    bool CheckWalled()
    {
        // location of all left and right side of player
        Vector2 sideleft = transform.position + new Vector3(-size.x / 2, 0, 0);
        Vector2 sideright = transform.position + new Vector3(size.x / 2, 0, 0);

        // check if we are walled
        bool walled1 = Physics.Raycast(sideleft, Vector2.left, 0.03f);
        bool walled2 = Physics.Raycast(sideright, Vector2.right, 0.03f);


        return (walled1 || walled2);
    }
    */

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Coin"))
        {
            // Increase our score
            GameManager.instance.IncreaseScore(1);

            // Destroy coin
            Destroy(other.gameObject);
        }
        else if (other.CompareTag("Enemy"))
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

    
}
