using UnityEngine;
using System.Collections;
using Fragsurf.Movement;  // Needed for SurfCharacter

[RequireComponent(typeof(SurfCharacter))]
public class WallJumpAndRun : MonoBehaviour
{
    [Header("Wall Jump Settings")]
    [Tooltip("Distance to check for a wall.")]
    public float wallCheckDistance = 0.6f;

    [Tooltip("Surfaces with a Y normal below this value are considered vertical.")]
    public float wallNormalThreshold = 0.5f;

    [Tooltip("Fixed lateral force applied during a wall jump.")]
    public float wallJumpHorizontalForce = 10f;

    [Tooltip("Fixed upward force applied during a wall jump.")]
    public float wallJumpUpForce = 7f;

    [Tooltip("Cooldown time (in seconds) between wall jumps.")]
    public float wallJumpCooldown = 0.5f;

    [Tooltip("Duration during which air control is disabled after a wall jump.")]
    public float airStrafeLockDuration = 0.3f;

    [Header("Wall Run Settings")]
    [Tooltip("Maximum duration for a wall run.")]
    public float wallRunDuration = 2f;

    [Tooltip("Speed during a wall run.")]
    public float wallRunSpeed = 8f;

    [Tooltip("Factor to reduce gravity during a wall run (0 = no gravity, 1 = full gravity).")]
    public float wallRunGravityReduction = 0.5f;

    [Tooltip("Minimum vertical input to initiate a wall run.")]
    public float minWallRunInput = 0.1f;

    [Tooltip("Layers that count as walls.")]
    public LayerMask wallJumpMask = ~0;

    private SurfCharacter _surfCharacter;
    private float _lastWallJumpTime = -Mathf.Infinity;

    // Wall run state
    private bool isWallRunning = false;
    private float wallRunTimer = 0f;
    private Vector3 wallRunDirection = Vector3.zero;
    private Vector3 currentWallNormal = Vector3.zero;

    private void Start()
    {
        _surfCharacter = GetComponent<SurfCharacter>();
        if (_surfCharacter == null)
        {
            Debug.LogError("WallJumpAndRun requires a SurfCharacter component on the same GameObject.");
        }
    }

    private void Update()
    {
        // --- WALL JUMP LOGIC ---
        // When Jump is pressed (and off cooldown) while airborne and near a wall,
        // perform a wall jump that applies a fixed upward force plus a fixed side force.
        if (Input.GetButtonDown("Jump") && Time.time >= _lastWallJumpTime + wallJumpCooldown)
        {
            if (!IsGrounded())
            {
                Vector3 wallNormal;
                if (CheckForWall(out wallNormal))
                {
                    // Determine lateral force: if the wall's normal dot player's right is positive,
                    // the wall is on the left so jump to the right; otherwise, jump to the left.
                    float dot = Vector3.Dot(wallNormal, transform.right);
                    Vector3 lateralImpulse = (dot > 0f ? transform.right : -transform.right) * wallJumpHorizontalForce;
                    Vector3 upwardImpulse = Vector3.up * wallJumpUpForce;
                    Vector3 jumpVelocity = upwardImpulse + lateralImpulse;

                    _surfCharacter.moveData.velocity = jumpVelocity;
                    _surfCharacter.moveData.wishJump = false;
                    StartCoroutine(LockAirStrafeRoutine());
                    _lastWallJumpTime = Time.time;

                    // Cancel any active wall run.
                    isWallRunning = false;
                    return;
                }
            }
        }

        // --- WALL RUN LOGIC ---
        // When airborne and pressing forward, if a wall is detected the player will wall run.
        if (!IsGrounded() && Input.GetAxis("Vertical") > minWallRunInput)
        {
            Vector3 detectedWallNormal;
            if (CheckForWall(out detectedWallNormal))
            {
                if (!isWallRunning)
                {
                    // Start wall run.
                    isWallRunning = true;
                    wallRunTimer = wallRunDuration;
                    currentWallNormal = detectedWallNormal;
                    // Calculate wall run direction as the tangent to the wall.
                    // (We use the cross product of the wall's normal and up.
                    // Then choose the candidate that best aligns with the player's forward.)
                    Vector3 candidate = Vector3.Cross(currentWallNormal, Vector3.up);
                    if (Vector3.Dot(candidate, transform.forward) < 0)
                    {
                        candidate = -candidate;
                    }
                    wallRunDirection = candidate.normalized;
                }
            }
            else
            {
                // If no wall is detected, end wall run.
                isWallRunning = false;
            }
        }
        else
        {
            // If not moving forward or grounded, cancel wall run.
            isWallRunning = false;
        }

        // If currently wall running, update the timer and override movement.
        if (isWallRunning)
        {
            wallRunTimer -= Time.deltaTime;
            if (wallRunTimer <= 0)
            {
                isWallRunning = false;
            }
            else
            {
                // While wall running, force a constant horizontal speed along the wall-run direction.
                // Also, reduce the effect of gravity by preserving the vertical velocity with a reduction factor.
                Vector3 currentVel = _surfCharacter.moveData.velocity;
                float preservedUpward = currentVel.y;
                _surfCharacter.moveData.velocity = wallRunDirection * wallRunSpeed + Vector3.up * preservedUpward * wallRunGravityReduction;
            }
        }
    }

    private IEnumerator LockAirStrafeRoutine()
    {
        _surfCharacter.disableAirStrafe = true;
        yield return new WaitForSeconds(airStrafeLockDuration);
        _surfCharacter.disableAirStrafe = false;
    }

    /// <summary>
    /// Checks for a wall by casting rays in several horizontal directions.
    /// Returns true if a valid wall is detected and outputs its normal.
    /// </summary>
    private bool CheckForWall(out Vector3 wallNormal)
    {
        wallNormal = Vector3.zero;
        // Check in cardinal and diagonal directions.
        Vector3[] directions = new Vector3[]
        {
            transform.right,
            -transform.right,
            transform.forward,
            -transform.forward,
            (transform.forward + transform.right).normalized,
            (transform.forward - transform.right).normalized,
            (-transform.forward + transform.right).normalized,
            (-transform.forward - transform.right).normalized
        };

        foreach (Vector3 dir in directions)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, dir, out hit, wallCheckDistance, wallJumpMask))
            {
                if (hit.normal.y < wallNormalThreshold)
                {
                    wallNormal = hit.normal;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// A simple ground check that casts a ray downward from the player's position.
    /// Adjust the ray length if needed.
    /// </summary>
    private bool IsGrounded()
    {
        float rayLength = _surfCharacter.colliderSize.y / 2f + 0.1f;
        return Physics.Raycast(transform.position, Vector3.down, rayLength);
    }
}
