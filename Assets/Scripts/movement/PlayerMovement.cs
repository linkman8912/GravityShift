// Some stupid rigidbody based movement by Dani

using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour {

    [Header("Assignables")]
    public Transform playerCam;
    public Transform orientation;
    [SerializeField] Grappling gp;

    //Other
    private Rigidbody rb;
    private Wallrunning wr;

    [Header("Rotation and look")]
    private float xRotation;
    public float sensitivity = 300f;

    [Header("Movement")]
    public float moveSpeed = 950f;
    public float maxSpeed = 400f;
    [HideInInspector] public bool grounded;
    [HideInInspector] public bool wallrunning = false;
    public LayerMask whatIsGround;
    public float wallrunSpeed = 200f;

    public float counterMovement = 0.175f;
    [SerializeField] private float counterMovementTime = 1f;
    private float counterMovementTimer = 0f;
    private bool airLastFrame = false;
    private float threshold = 0.01f;
    public float maxSlopeAngle = 35f;

    [Header("Crouch & Slide")]
    private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
    private Vector3 playerScale;
    public float slideForce = 9f;
    public float slideCounterMovement = 0;
    public bool sliding = false;
    private Vector3 slideStartSpeed;
    [SerializeField] private float slideLeniencyHeight = 1f;
    private bool slideBuffered;
    [SerializeField] private float slideDecay = 0.99f;
    [SerializeField] private float slideLeanDegrees = 20f;
    [SerializeField] private float slideCameraLean = 15f;

    [Header("Jumping")]
    private bool readyToJump = true;
    [HideInInspector] public bool secondJump = true;
    [SerializeField] private float jumpCooldown = 0.25f;
    public float jumpForce = 200;

    [Header("Ground Slam")]
    [SerializeField] private float slamVelocity = 100f;
    private bool slamming = false;
    float slamStartingHeight;
    [SerializeField] float slamJumpTime = 0.5f;
    float slamJumpTimer = 0;

    [Header("Ground Slam Effect")]
    [SerializeField] private ParticleSystem slamLandEffect;

    [HideInInspector] public bool grappling = false;


    //Input
    float x, y;
    bool jumping, doubleJumping, sprinting, crouching;

    //Sliding
    private Vector3 normalVector = Vector3.up;
    private Vector3 wallNormalVector;

    private Footsteps footsteps;

    void Awake() {
        wr = GetComponent<Wallrunning>();
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        footsteps = GetComponent<Footsteps>();
    }

    void Start() {
        playerScale = transform.localScale;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void FixedUpdate() {
        if (grounded && airLastFrame) {
          airLastFrame = false;
          counterMovementTimer = counterMovementTime;
        }
        Movement();
    }

    void Update() {
        if (counterMovementTimer > 0)
          counterMovementTimer -= Time.deltaTime;
        if (counterMovementTimer < 0) 
          counterMovementTimer = 0;
        if (grounded && airLastFrame) {
          airLastFrame = false;
          counterMovementTimer = counterMovementTime;
        }
        MyInput();
        //Look();
        if (slamJumpTimer > 0) {
            slamJumpTimer -= Time.deltaTime;
            if (slamJumpTimer < 0)
                slamJumpTimer = 0;
        }
        if (!grounded) 
          airLastFrame = true;
    }

    void LateUpdate() {
        Look();
    }

    /// <summary>
    /// Find user input. Should put this in its own class but im lazy
    /// </summary>
    void MyInput() {
        x = Input.GetAxisRaw("Horizontal");
        y = Input.GetAxisRaw("Vertical");
        jumping = Input.GetButton("Jump");
        doubleJumping = Input.GetButtonDown("Jump");
        crouching = (Input.GetKey(KeyCode.LeftControl) && grounded);
        //Crouching
        if ((Input.GetKeyDown(KeyCode.LeftControl) || slideBuffered) && grounded && !slamming) {
            StartCrouch();
            StopSlideBuffer();
        }
        if (Input.GetKeyDown(KeyCode.LeftControl) && !grounded && !slamming)
            if (slideBuffered) {
              if (!CheckSlideHeight()) {
                StopSlideBuffer();
              }
            }
            else if (CheckSlideHeight()) {
              BufferSlide(); 
            }
            else {
              StartSlam();
            }
        if (Input.GetKeyUp(KeyCode.LeftControl))
            StopCrouch();
    }

    void StartCrouch() {
        transform.localScale = crouchScale;
        transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
        if (rb.velocity.magnitude > 0.5f && grounded) {
          //rb.AddForce(orientation.transform.forward * slideForce);
          StartSlide();
        }
    }

    void StartSlide() {
      sliding = true;
      //slideStartSpeed = rb.velocity.magnitude;
      slideStartSpeed = rb.velocity;
    }

    void StopCrouch() {
        transform.localScale = playerScale;
        transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
        if (sliding) StopSlide();
    }
    
    void StopSlide() {
      sliding = false;
    }

    void Movement() {
        //Extra gravity
        //rb.AddForce(Vector3.down * Time.deltaTime * 60);
        //Find actual velocity relative to where player is looking
        Vector2 mag = FindVelRelativeToLook();
        float xMag = mag.x, yMag = mag.y;
        rb.useGravity = false;
        //Counteract sliding and sloppy movement
        if (counterMovementTimer == 0 && !sliding)
          CounterMovement(x, y, mag);
        if (!wallrunning) {
            //rb.AddForce(Physics.gravity);
            rb.useGravity = true;
            //rb.AddForce(Vector3.down * Time.deltaTime * 60);
            //CounterMovement(x, y, mag);
        }

        //If holding jump && ready to jump, then jump
        //if (grappling && (jumping || doubleJumping)) gp.StopGrapple(); 
        if (readyToJump && jumping) Jump();
        if (readyToJump && doubleJumping && !grounded) Jump(true);

        //Set max speed
        float maxSpeed = this.maxSpeed;

        //If sliding down a ramp, add force down so player stays grounded and also builds speed
        if (crouching && grounded && readyToJump) {
            rb.AddForce(Vector3.down * Time.deltaTime * 3000);
            //return;
        }
        // Movement while sliding
        if (sliding) {
          //rb.velocity = slideStartSpeed * orientation.forward;
          rb.velocity = (Quaternion.AngleAxis(slideLeanDegrees * x, Vector3.up) * slideStartSpeed.normalized) * (slideStartSpeed.magnitude * slideDecay);
          wr.LeanCamera(slideCameraLean * -x);
        }

        //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
        if (x > 0 && xMag > maxSpeed) x = 0;
        if (x < 0 && xMag < -maxSpeed) x = 0;
        if (y > 0 && yMag > maxSpeed) y = 0;
        if (y < 0 && yMag < -maxSpeed) y = 0;

        //Some multipliers
        float multiplier = 1f, multiplierV = 1f;

        // Movement in air
        /*if (!grounded) {
          multiplier = 0.5f;
          multiplierV = 0.5f;
          }*/

        if (grounded && FindVelRelativeToLook() != new Vector2(0, 0))
            footsteps.PlayFootstep();

        if (slamming && grounded) StopSlam();
        else if (slamming) {
            rb.velocity = new Vector3(0f, -slamVelocity, 0f);
        }

        if (slamming && grappling) StopSlam();

        if (!wallrunning && !slamming && !sliding) {
            //Apply forces to move player
            rb.AddForce(orientation.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
            rb.AddForce(orientation.right * x * moveSpeed * Time.deltaTime * multiplier);
        }
    }

    void Jump(bool dj = false) {
        bool canJump = false;
        if ((dj && secondJump /*&& readyToJump*/) || (!dj && grounded && readyToJump)) canJump = true;

        if (canJump) {
            if (dj) {
                secondJump = false;
                if (FindVelRelativeToLook().y < 0)
                    rb.velocity = new Vector3(0, 0, 0);
            }
            readyToJump = false;

            if (slamJumpTimer > 0) {
                // Add jump forces
                float velocity = Mathf.Sqrt(slamStartingHeight * -2 * Physics.gravity.y) / 1.8f - (Time.fixedDeltaTime * Physics.gravity.y / 2);
                //if (velocity > jumpForce * 1.5f) {
                rb.AddForce(Vector2.up * velocity, ForceMode.VelocityChange);
                rb.AddForce(normalVector * velocity, ForceMode.VelocityChange);
                /*}
                else {
                  rb.AddForce(Vector2.up * jumpForce * 1.5f);
                  rb.AddForce(normalVector * jumpForce * 0.5f);
                }*/
            }
            else
            {
                // Add jump forces
                rb.AddForce(Vector2.up * jumpForce * 1.5f);
                rb.AddForce(normalVector * jumpForce * 0.5f);
            }

            //If jumping while falling, reset y velocity.
            Vector3 vel = rb.velocity;
            if (rb.velocity.y < 0.5f) rb.velocity = new Vector3(vel.x, 0, vel.z);
            else if (rb.velocity.y > 0) rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

            Invoke(nameof(ResetJump), jumpCooldown);
            StopCrouch();
        }
    }

    void ResetJump() {
        readyToJump = true;
    }

    private float desiredX;
    private float yaw;
    private float newDesiredX;

    void CounterMovement(float x, float y, Vector2 mag) {
        if (!grounded || jumping || sliding) return;

        //Slow down sliding
        if (crouching) {
            //rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
            return;
        }

        //Counter movement
        if (Math.Abs(mag.x) > threshold && Math.Abs(x) < 0.05f || (mag.x < -threshold && x > 0) || (mag.x > threshold && x < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.right * Time.deltaTime * -mag.x * counterMovement);
        }
        if (Math.Abs(mag.y) > threshold && Math.Abs(y) < 0.05f || (mag.y < -threshold && y > 0) || (mag.y > threshold && y < 0)) {
            rb.AddForce(moveSpeed * orientation.transform.forward * Time.deltaTime * -mag.y * counterMovement);
        }

        //Limit diagonal running. This will also cause a full stop if sliding fast and un-crouching, so not optimal.
        if (Mathf.Sqrt((Mathf.Pow(rb.velocity.x, 2) + Mathf.Pow(rb.velocity.z, 2))) > maxSpeed) {
            float fallspeed = rb.velocity.y;
            Vector3 n = rb.velocity.normalized * maxSpeed;
            rb.velocity = new Vector3(n.x, fallspeed, n.z);
        }
    }

    /// <summary>
    /// Find the velocity relative to where the player is looking
    /// Useful for vectors calculations regarding movement and limiting movement
    /// </summary>
    /// <returns></returns>
    public Vector2 FindVelRelativeToLook() {
        Vector3 localVel = orientation.InverseTransformDirection(rb.velocity);
        return new Vector2(localVel.x, localVel.z);
    }

    private bool IsFloor(Vector3 v) {
        float angle = Vector3.Angle(Vector3.up, v);
        return angle < maxSlopeAngle;
    }

    private bool cancellingGrounded;

    /// <summary>
    /// Handle ground detection
    /// </summary>
    void OnCollisionStay(Collision other) {
        //Make sure we are only checking for walkable layers
        int layer = other.gameObject.layer;
        if (whatIsGround != (whatIsGround | (1 << layer))) return;

        //Iterate through every collision in a physics update
        for (int i = 0; i < other.contactCount; i++) {
            Vector3 normal = other.contacts[i].normal;
            //FLOOR
            if (IsFloor(normal)) {
                grounded = true;
                secondJump = true;
                cancellingGrounded = false;
                normalVector = normal;
                CancelInvoke(nameof(StopGrounded));
            }
        }

        //Invoke ground/wall cancel, since we can't check normals with CollisionExit
        float delay = 3f;
        if (!cancellingGrounded) {
            cancellingGrounded = true;
            Invoke(nameof(StopGrounded), Time.deltaTime * delay);
        }
    }

    void StopGrounded() {
        grounded = false;
    }

    void StartSlam() {
        slamStartingHeight = transform.position.y;
        slamming = true;
        StopCrouch();
        gp.StopGrapple();
    }

    void StopSlam() {
        if (slamLandEffect != null)
            Instantiate(slamLandEffect, transform.position, Quaternion.identity);
        slamming = false;
        slamJumpTimer = slamJumpTime;
    }


    void Look() {
        float mouseX = Input.GetAxis("Mouse X") * sensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * sensitivity * Time.deltaTime;

        //// pitch
        //xRotation = Mathf.Clamp(xRotation - mouseY, -90f, 90f);
        //playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // yaw
        ////transform.Rotate(Vector3.up, mouseX);
        ////orientation.rotation = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        ////attempt: (works)
        //orientation.Rotate(Vector3.up, mouseX);
        //orientation.rotation = Quaternion.Euler(0f, orientation.eulerAngles.y, 0f);

        //new 
        xRotation = Mathf.Clamp(xRotation - mouseY, -90f, 90f);
        playerCam.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        yaw += mouseX;
        orientation.rotation = Quaternion.Euler(0f, yaw, 0f);
    }

    bool CheckSlideHeight() {
      return Physics.Raycast(transform.position, Vector3.down, slideLeniencyHeight, whatIsGround);
    }

    void BufferSlide() {
      slideBuffered = true;
    }

    void StopSlideBuffer() {
      slideBuffered = false;
    }
}
