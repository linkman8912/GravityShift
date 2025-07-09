using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallrunning : MonoBehaviour
{
    [Header("Wallrunning")]
    [SerializeField] private LayerMask whatIsWall;
    private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce = 200;
    [SerializeField] private float maxWallrunTime = 1.5f;
    [SerializeField] private float wallMomentumAngle = 40;
    [SerializeField] private float wallrunDelay = 0.5f;
    private float targetCameraLean;
    private float wallrunTimer;
    private bool readyToWallrun = true;

    [Header("Walljumping")]
    [SerializeField] private float walljumpUpForce = 100f;
    [SerializeField] private float walljumpSideForce = 50f;
    //private GameObject mostRecentWalljump;

    [Header("Coyote Time")]
    [SerializeField] private float wallCoyoteTime = 0.5f;
    private float wallCoyoteTimer = 0f;
    private bool exitedWallrunRecently = false;
    private Vector3 lastWallNormal;
    private GameObject lastWallObject;

    [Header("Input")]
    private float horizontalInput;
    private float verticalInput;

    [Header("Detection")]
    [SerializeField] private float wallCheckDistance = 2;
    [SerializeField] private float minJumpHeight = 1;
    private RaycastHit leftWallHit;
    private RaycastHit rightWallHit;
    private bool wallLeft;
    private bool wallRight;

    [Header("References")]
    public Transform orientation;
    public Transform camera;
    private PlayerMovement pm;
    private Rigidbody rb;
    private Footsteps footsteps;

    [Header("Visual")]
    [SerializeField] private float cameraLeanAngle = 15f;
    [SerializeField] private float cameraLeanSpeed = 5f;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        pm = GetComponent<PlayerMovement>();
        whatIsGround = pm.whatIsGround;
        footsteps = GetComponent<Footsteps>();
    }

    void Update()
    {
        StateMachine();
        //if (pm.grounded) mostRecentWalljump = null;
        HandleCameraLean();

        // Handle wall coyote time timer
        if (exitedWallrunRecently)
        {
            wallCoyoteTimer -= Time.deltaTime;
            if (wallCoyoteTimer <= 0f)
            {
                exitedWallrunRecently = false;
            }
        }
    }

    void FixedUpdate()
    {
        CheckForWall();
        if (pm.wallrunning)
            WallrunningMovement();
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && readyToWallrun && ((horizontalInput > 0 && wallRight) || (horizontalInput < 0 && wallLeft)))
        {
            if (!pm.wallrunning)
            {
                StartWallrun();
            }

            footsteps.PlayFootstep();

            if (wallrunTimer < maxWallrunTime)
            {
                wallrunTimer += Time.deltaTime;
            }
            else
            {
                StopWallrun();
            }

            if (Input.GetButtonDown("Jump"))
            {
                StopWallrun();
                Walljump();
            }
        }
        else if (pm.wallrunning)
        {
            StopWallrun();
        }

        else if (exitedWallrunRecently && Input.GetButtonDown("Jump") && readyToWallrun && lastWallObject != null)
        {
            Walljump(lastWallNormal, lastWallObject);
            exitedWallrunRecently = false;
        }
    }

    void StartWallrun()
    {
        pm.wallrunning = true;
        pm.secondJump = true;
    }

    void WallrunningMovement()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);
        if ((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
            wallForward = -wallForward;
        //if (Vector3.Angle(rb.velocity.normalized, wallForward) <= wallMomentumAngle) {
        rb.velocity = rb.velocity.magnitude * wallForward;
        //}


        rb.useGravity = false;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

        rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
        rb.AddForce(-wallNormal * wallRunForce / 2, ForceMode.Force);
    }

    void StopWallrun()
    {
        if (pm.wallrunning)
        {
            // Store the current velocity before stopping wallrun
            Vector3 currentVelocity = rb.velocity;

            exitedWallrunRecently = true;
            wallCoyoteTimer = wallCoyoteTime;

            // Safely get wall normal and object - check if collider exists
            if (wallRight && rightWallHit.collider != null)
            {
                lastWallNormal = rightWallHit.normal;
                lastWallObject = rightWallHit.collider.gameObject;
            }
            else if (wallLeft && leftWallHit.collider != null)
            {
                lastWallNormal = leftWallHit.normal;
                lastWallObject = leftWallHit.collider.gameObject;
            }
            else
            {
                // If no valid wall hit, preserve the last known normal or use a default
                // Don't set to Vector3.zero as this loses direction information
                if (lastWallNormal == Vector3.zero)
                {
                    // Use a reasonable default based on the last known wall side
                    lastWallNormal = wallRight ? Vector3.left : Vector3.right;
                }
                lastWallObject = null;
            }

            // Re-enable gravity
            rb.useGravity = true;

            // Preserve horizontal momentum when exiting wallrun
            // Add a small downward velocity to make the transition feel natural
            rb.velocity = new Vector3(currentVelocity.x, currentVelocity.y - 2f, currentVelocity.z);
        }

        pm.wallrunning = false;
        wallrunTimer = 0;
    }

    void Walljump()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        GameObject currentTarget = wallRight ? rightWallHit.collider.gameObject : leftWallHit.collider.gameObject;
        Walljump(wallNormal, currentTarget);
    }

    void Walljump(Vector3 wallNormal, GameObject wallObj)
    {
        if (wallObj == null /*|| wallObj == mostRecentWalljump*/) return;
        wallNormal = wallRight ? -transform.right : transform.right;
        //Vector3 forceToApply = transform.up * walljumpUpForce / 10 + wallNormal * walljumpSideForce / 10;
        Vector3 forceToApply = transform.up * walljumpUpForce / 10 + wallNormal * walljumpSideForce / 10;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
        //mostRecentWalljump = wallObj;
        readyToWallrun = false;
        Invoke("ResetWallrunDelay", wallrunDelay);
        footsteps.PlayFootstep();
    }

    void ResetWallrunDelay()
    {
        readyToWallrun = true;
    }

    private void HandleCameraLean()
    {
        targetCameraLean = wallLeft && pm.wallrunning ? -cameraLeanAngle
            : wallRight && pm.wallrunning ? cameraLeanAngle
            : 0f;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetCameraLean);
        camera.localRotation = Quaternion.Slerp(
            camera.localRotation,
            targetRot,
            cameraLeanSpeed * Time.deltaTime
        );
    }
}