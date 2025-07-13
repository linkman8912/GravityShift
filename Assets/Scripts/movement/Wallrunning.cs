using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallrunning : MonoBehaviour
{
    [Header("Wallrunning")]
    [SerializeField] private LayerMask whatIsWall;
    private LayerMask whatIsGround;
    [SerializeField] private float wallRunForce = 3;
    [SerializeField] private float maxWallrunTime = 1.5f;
    [SerializeField] private float wallMomentumAngle = 40;
    [SerializeField] private float wallrunDelay = 0.5f;
    private float targetCameraLean;
    private float wallrunTimer;
    private bool readyToWallrun = true;

    [Header("Wall Transition")]
    [SerializeField] private float maxTransitionAngle = 30f; // Maximum angle between walls to allow transition
    [SerializeField] private float transitionSmoothness = 10f; // How smooth the transition is
    [SerializeField] private float transitionCheckDistance = 1.5f; // How far ahead to check for wall transitions
    private Vector3 currentWallNormal;
    private Vector3 currentWallForward;
    private bool isTransitioning = false;

    [Header("Walljumping")]
    [SerializeField] private float walljumpUpForce = 100f;
    [SerializeField] private float walljumpSideForce = 50f;

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

    void Update() {
        Debug.Log(rb.velocity.magnitude);
        StateMachine();
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
        {
            // Check for wall transitions first
            if (!CheckForWallTransition())
            {
                // If no transition available and no wall, stop wallrun
                if (!(wallLeft || wallRight))
                {
                    StopWallrun();
                }
                else
                {
                    WallrunningMovement();
                }
            }
        }
    }

    private void CheckForWall()
    {
        wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
        wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
    }

    private bool CheckForWallTransition()
    {
        if (!pm.wallrunning) return false;

        // Determine which side we're wallrunning on
        bool isOnRightWall = wallRight && (!wallLeft || rightWallHit.distance < leftWallHit.distance);
        Vector3 wallDirection = isOnRightWall ? orientation.right : -orientation.right;
        RaycastHit currentWallHit = isOnRightWall ? rightWallHit : leftWallHit;

        // Check ahead in the movement direction for potential wall transitions
        Vector3 checkOrigin = transform.position + currentWallForward * transitionCheckDistance;
        RaycastHit transitionHit;

        // Cast multiple rays to detect corner transitions
        bool foundTransition = false;
        Vector3 bestTransitionNormal = Vector3.zero;
        float bestAngle = maxTransitionAngle;

        // Check in an arc around the current wall direction
        for (float angle = -60f; angle <= 60f; angle += 10f)
        {
            Vector3 checkDirection = Quaternion.AngleAxis(angle, Vector3.up) * wallDirection;
            if (Physics.Raycast(checkOrigin, checkDirection, out transitionHit, wallCheckDistance * 1.5f, whatIsWall))
            {
                // Check if this is a different wall (not the current one)
                float normalAngle = Vector3.Angle(currentWallNormal, transitionHit.normal);
                if (normalAngle > 5f && normalAngle <= maxTransitionAngle)
                {
                    // Check if the wall height is appropriate for continuing wallrun
                    if (IsWallHeightAppropriate(transitionHit))
                    {
                        if (normalAngle < bestAngle)
                        {
                            foundTransition = true;
                            bestTransitionNormal = transitionHit.normal;
                            bestAngle = normalAngle;
                        }
                    }
                }
            }
        }

        if (foundTransition)
        {
            // Start transitioning to the new wall
            StartWallTransition(bestTransitionNormal);
            return true;
        }

        return false;
    }

    private bool IsWallHeightAppropriate(RaycastHit newWallHit)
    {
        // Check if there's ground below the hit point at an appropriate distance
        Vector3 checkPoint = newWallHit.point;
        float playerHeight = transform.position.y - checkPoint.y;

        // If the new wall is too high above the player, check if there's wall at player height
        if (playerHeight > 0.5f)
        {
            RaycastHit heightCheckHit;
            Vector3 playerHeightPoint = new Vector3(checkPoint.x, transform.position.y, checkPoint.z);
            Vector3 directionToWall = (checkPoint - playerHeightPoint).normalized;

            if (!Physics.Raycast(playerHeightPoint, directionToWall, out heightCheckHit, wallCheckDistance * 2f, whatIsWall))
            {
                return false;
            }
        }

        // Check if there's enough wall below for running
        RaycastHit groundCheckHit;
        if (Physics.Raycast(checkPoint, Vector3.down, out groundCheckHit, minJumpHeight, whatIsGround))
        {
            return false; // Too close to ground
        }

        return true;
    }

    private void StartWallTransition(Vector3 newWallNormal)
    {
        isTransitioning = true;
        StartCoroutine(TransitionToNewWall(newWallNormal));
    }

    private IEnumerator TransitionToNewWall(Vector3 newWallNormal)
    {
        Vector3 startNormal = currentWallNormal;
        Vector3 startForward = currentWallForward;

        // Store the current velocity magnitude to preserve momentum
        float currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        // Calculate new wall forward
        Vector3 newWallForward = Vector3.Cross(newWallNormal, transform.up);
        if (Vector3.Angle(currentWallForward, newWallForward) > 90f)
            newWallForward = -newWallForward;

        float transitionTime = 0.2f;
        float elapsedTime = 0f;

        while (elapsedTime < transitionTime)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / transitionTime;
            t = Mathf.SmoothStep(0, 1, t);

            // Smoothly interpolate wall normal and forward
            currentWallNormal = Vector3.Slerp(startNormal, newWallNormal, t);
            currentWallForward = Vector3.Slerp(startForward, newWallForward, t);

            // Maintain momentum during transition by setting velocity along interpolated forward
            Vector3 desiredVelocity = currentWallForward * currentSpeed;
            rb.velocity = new Vector3(desiredVelocity.x, rb.velocity.y, desiredVelocity.z);

            yield return null;
        }

        currentWallNormal = newWallNormal;
        currentWallForward = newWallForward;
        isTransitioning = false;
    }

    private bool AboveGround()
    {
        return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
    }

    void StateMachine()
    {
        horizontalInput = Input.GetAxisRaw("Horizontal");
        verticalInput = Input.GetAxisRaw("Vertical");

        if (verticalInput > 0 && AboveGround() && readyToWallrun && ((horizontalInput > 0 && wallRight) || (horizontalInput < 0 && wallLeft)))
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
        else if (pm.wallrunning && !isTransitioning)
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

        // Initialize wall normal and forward
        if (wallRight)
        {
            currentWallNormal = rightWallHit.normal;
        }
        else if (wallLeft)
        {
            currentWallNormal = leftWallHit.normal;
        }

        currentWallForward = Vector3.Cross(currentWallNormal, transform.up);
        if (Vector3.Angle(rb.velocity.normalized, currentWallForward) > 90f)
            currentWallForward = -currentWallForward;
    }

    void WallrunningMovement()
    {
        // Store current velocity magnitude before any modifications
        float currentSpeed = new Vector3(rb.velocity.x, 0f, rb.velocity.z).magnitude;

        // Update current wall normal if not transitioning
        if (!isTransitioning)
        {
            currentWallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
            currentWallForward = Vector3.Cross(currentWallNormal, transform.up);
            if ((orientation.forward - currentWallForward).magnitude > (orientation.forward - -currentWallForward).magnitude)
                currentWallForward = -currentWallForward;
        }

        // Redirect velocity along the wall while preserving momentum
        Vector3 redirectedVelocity = currentWallForward * currentSpeed;

        rb.useGravity = false;
        rb.velocity = new Vector3(redirectedVelocity.x, 0f, redirectedVelocity.z);

        if (rb.velocity.magnitude < 40) {
          rb.AddForce(currentWallForward * wallRunForce, ForceMode.Force);
          rb.AddForce(-currentWallNormal * wallRunForce / 2, ForceMode.Force);
        }
    }

    void StopWallrun()
    {
        if (pm.wallrunning)
        {
            exitedWallrunRecently = true;
            wallCoyoteTimer = wallCoyoteTime;

            // Safely get wall normal and object
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
                if (lastWallNormal == Vector3.zero)
                {
                    lastWallNormal = wallRight ? Vector3.left : Vector3.right;
                }
                lastWallObject = null;
            }

            rb.useGravity = true;
        }

        pm.wallrunning = false;
        wallrunTimer = 0;
        isTransitioning = false;
    }

    void Walljump()
    {
        Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
        GameObject currentTarget = wallRight ? rightWallHit.collider.gameObject : leftWallHit.collider.gameObject;
        Walljump(wallNormal, currentTarget);
    }

    void Walljump(Vector3 wallNormal, GameObject wallObj)
    {
        if (wallObj == null) return;
        wallNormal = wallRight ? -transform.right : transform.right;
        Vector3 forceToApply = transform.up * walljumpUpForce / 10 + wallNormal * walljumpSideForce / 10;
        rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
        rb.AddForce(forceToApply, ForceMode.Impulse);
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
        // During transition, calculate lean based on current interpolated normal
        float currentLean = 0f;
        if (pm.wallrunning)
        {
            // Determine which side the wall is on based on the current normal
            float dotRight = Vector3.Dot(orientation.right, -currentWallNormal);
            currentLean = dotRight > 0 ? cameraLeanAngle : -cameraLeanAngle;
        }

        targetCameraLean = currentLean;

        Quaternion targetRot = Quaternion.Euler(0f, 0f, targetCameraLean);
        camera.localRotation = Quaternion.Slerp(
            camera.localRotation,
            targetRot,
            cameraLeanSpeed * Time.deltaTime
        );
    }
}
