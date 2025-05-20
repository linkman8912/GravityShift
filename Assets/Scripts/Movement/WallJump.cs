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
    
    [Tooltip("Fixed lateral force applied during a wall jump (when not wall running).")]
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
    
    [Tooltip("Factor to reduce gravity during a wall run (0 = no gravity, 1 = full gravity).")]
    public float wallRunGravityReduction = 0.5f;
    
    [Tooltip("Minimum horizontal speed during a wall run.")]
    public float minWallRunSpeed = 8f;
    
    [Tooltip("Multiplier for any extra speed above the minimum (1 = add extra speed as-is).")]
    public float wallRunSpeedBonusMultiplier = 1f;
    
    [Tooltip("Delay (in seconds) before starting a wall run once conditions are met.")]
    public float wallRunDelay = 0.2f;
    
    [Header("Camera Lean Settings")]
    [Tooltip("Maximum angle (in degrees) that the camera will lean.")]
    public float cameraLeanAngle = 15f;
    
    [Tooltip("Speed at which the camera lean interpolates.")]
    public float cameraLeanSpeed = 5f;
    
    [Header("Layer Mask")]
    [Tooltip("Layers that count as walls.")]
    public LayerMask wallJumpMask = ~0; // All layers by default

    private SurfCharacter _surfCharacter;
    private float _lastWallJumpTime = -Mathf.Infinity;
    
    // Wall run state variables
    private bool _isWallRunning = false;
    private float _wallRunTimer = 0f;
    private Vector3 _wallRunDirection = Vector3.zero;
    private Vector3 _currentWallNormal = Vector3.zero;
    
    // Capture the player's horizontal speed when entering the wall run.
    private float _initialWallRunSpeed = 0f;
    
    // Delay timer before wall run starts.
    private float _wallRunDelayTimer = 0f;
    
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
        if (Input.GetButtonDown("Jump") && Time.time >= _lastWallJumpTime + wallJumpCooldown)
        {
            if (!IsGrounded())
            {
                Vector3 wallNormal;
                if (CheckForWall(out wallNormal))
                {
                    Vector3 jumpVelocity;
                    if (_isWallRunning)
                    {
                        // Preserve current horizontal momentum from wall run.
                        Vector3 currentHorizontal = _surfCharacter.moveData.velocity;
                        currentHorizontal.y = 0f;
                        jumpVelocity = currentHorizontal + Vector3.up * wallJumpUpForce;
                    }
                    else
                    {
                        // Use fixed lateral force based on wall relative to the player.
                        float dot = Vector3.Dot(wallNormal, transform.right);
                        Vector3 lateralImpulse = (dot > 0f ? transform.right : -transform.right) * wallJumpHorizontalForce;
                        jumpVelocity = Vector3.up * wallJumpUpForce + lateralImpulse;
                    }
                    
                    _surfCharacter.moveData.velocity = jumpVelocity;
                    _surfCharacter.moveData.wishJump = false;
                    StartCoroutine(LockAirStrafeRoutine());
                    _lastWallJumpTime = Time.time;
                    
                    // Cancel any active wall run.
                    _isWallRunning = false;
                    _wallRunDelayTimer = 0f;
                    return;
                }
            }
        }
        
        // --- WALL RUN LOGIC ---
        // Prevent wall runs if the player is on the ground.
        if (IsGrounded())
        {
            _isWallRunning = false;
            _wallRunDelayTimer = 0f;
        }
        else
        {
            Vector3 detectedWallNormal;
            if (CheckForWall(out detectedWallNormal))
            {
                // Increment the delay timer unconditionally.
                _wallRunDelayTimer += Time.deltaTime;
                
                if (_wallRunDelayTimer >= wallRunDelay)
                {
                    if (!_isWallRunning)
                    {
                        // Start the wall run.
                        _isWallRunning = true;
                        _wallRunTimer = wallRunDuration;
                        _currentWallNormal = detectedWallNormal;
                        // Calculate the wall run direction as the tangent to the wall.
                        Vector3 candidate = Vector3.Cross(_currentWallNormal, Vector3.up);
                        if (Vector3.Dot(candidate, transform.forward) < 0)
                        {
                            candidate = -candidate;
                        }
                        _wallRunDirection = candidate.normalized;
                        // Capture the player's current horizontal speed.
                        Vector3 horizontalVel = _surfCharacter.moveData.velocity;
                        horizontalVel.y = 0f;
                        _initialWallRunSpeed = horizontalVel.magnitude;
                    }
                }
                // If no wall is detected, cancel wall run.
                else
                {
                    _isWallRunning = false;
                }
            }
            else
            {
                _isWallRunning = false;
                _wallRunDelayTimer = 0f;
            }
        }
        
        // If wall running, update the timer and override movement.
        if (_isWallRunning)
        {
            _wallRunTimer -= Time.deltaTime;
            if (_wallRunTimer <= 0f)
            {
                _isWallRunning = false;
            }
            else
            {
                // Calculate effective speed:
                // Ensure it's at least minWallRunSpeed, and add extra momentum above minimum.
                float effectiveSpeed = minWallRunSpeed + 
                    wallRunSpeedBonusMultiplier * Mathf.Max(0f, _initialWallRunSpeed - minWallRunSpeed);
                // Preserve vertical velocity (with gravity reduction) and apply effectiveSpeed horizontally.
                Vector3 currentVel = _surfCharacter.moveData.velocity;
                float preservedUpward = currentVel.y;
                _surfCharacter.moveData.velocity = _wallRunDirection * effectiveSpeed + 
                    Vector3.up * (preservedUpward * wallRunGravityReduction);
            }
        }
        
        // --- CAMERA LEAN LOGIC ---
        if (_surfCharacter.viewTransform != null)
        {
            float targetLean = 0f;
            if (_isWallRunning)
            {
                // Determine which side the wall is relative to the player.
                float dot = Vector3.Dot(_currentWallNormal, transform.right);
                targetLean = (dot > 0f) ? -cameraLeanAngle : cameraLeanAngle;
            }
            Vector3 camEuler = _surfCharacter.viewTransform.localEulerAngles;
            float currentLean = camEuler.z;
            if (currentLean > 180f)
                currentLean -= 360f; // Convert to -180...180 range.
            float newLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * cameraLeanSpeed);
            camEuler.z = newLean;
            _surfCharacter.viewTransform.localEulerAngles = camEuler;
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
    /// Simple ground check: casts a ray downward from the player's position.
    /// Adjust the ray length as necessary for your collider.
    /// </summary>
    private bool IsGrounded()
    {
        float rayLength = _surfCharacter.colliderSize.y / 2f + 0.1f;
        return Physics.Raycast(transform.position, Vector3.down, rayLength);
    }
}
