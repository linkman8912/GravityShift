// Some stupid rigidbody based movement by Dani

using System;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{

  [Header("Assignables")]
  public Transform playerCam;
  public Transform orientation;

  //Other
  private Rigidbody rb;

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
  private float threshold = 0.01f;
  public float maxSlopeAngle = 35f;

  [Header("Crouch & Slide")]
  private Vector3 crouchScale = new Vector3(1, 0.5f, 1);
  private Vector3 playerScale;
  public float slideForce = 9f;
  public float slideCounterMovement = 0.2f;

  [Header("Jumping")]
  private bool readyToJump = true;
  private bool secondJump = true;
  private float jumpCooldown = 0.25f;
  public float jumpForce = 200;

  [Header("Ground Slam")]
  [SerializeField] private float slamForce = 500f;
  private bool slamming = false;

  //Input
  float x, y;
  bool jumping, doubleJumping, sprinting, crouching;

  //Sliding
  private Vector3 normalVector = Vector3.up;
  private Vector3 wallNormalVector;

  void Awake() {
    rb = GetComponent<Rigidbody>();
    rb.freezeRotation = true;  
    rb.interpolation = RigidbodyInterpolation.Interpolate;
  }

  void Start() {
    playerScale = transform.localScale;
    Cursor.lockState = CursorLockMode.Locked;
    Cursor.visible = false;
  }


  private void FixedUpdate() {
    Movement();
  }

  private void Update() {
    MyInput();
    //Look();
  }

  private void LateUpdate() {
    Look();
  }

  /// <summary>
  /// Find user input. Should put this in its own class but im lazy
  /// </summary>
  private void MyInput() {
    x = Input.GetAxisRaw("Horizontal");
    y = Input.GetAxisRaw("Vertical");
    jumping = Input.GetButton("Jump");
    doubleJumping = Input.GetButtonDown("Jump");
    crouching = Input.GetKey(KeyCode.LeftControl);

    //Crouching
    if (Input.GetKeyDown(KeyCode.LeftControl) && grounded && !slamming)
      StartCrouch();
    if (Input.GetKeyDown(KeyCode.LeftControl) && !grounded && !slamming)
      StartSlam();
    if (Input.GetKeyUp(KeyCode.LeftControl))
      StopCrouch();
  }

  private void StartCrouch() {
    transform.localScale = crouchScale;
    transform.position = new Vector3(transform.position.x, transform.position.y - 0.5f, transform.position.z);
    if (rb.velocity.magnitude > 0.5f) {
      if (grounded) {
        rb.AddForce(orientation.transform.forward * slideForce);
      }
    }
  }

  private void StopCrouch() {
    transform.localScale = playerScale;
    transform.position = new Vector3(transform.position.x, transform.position.y + 0.5f, transform.position.z);
  }

  private void Movement() {
    //Extra gravity
    //rb.AddForce(Vector3.down * Time.deltaTime * 60);

    //Find actual velocity relative to where player is looking
    Vector2 mag = FindVelRelativeToLook();
    float xMag = mag.x, yMag = mag.y;

    //Counteract sliding and sloppy movement
    //CounterMovement(x, y, mag);
    if (!wallrunning) {
      rb.useGravity = true;
      rb.AddForce(Vector3.down * Time.deltaTime * 60);
      CounterMovement(x, y, mag);
    }

    //If holding jump && ready to jump, then jump
    if (readyToJump && jumping) Jump();
    if (readyToJump && doubleJumping && !grounded) Jump(true);

    //Set max speed
    float maxSpeed = this.maxSpeed;

    //If sliding down a ramp, add force down so player stays grounded and also builds speed
    if (crouching && grounded && readyToJump) {
      rb.AddForce(Vector3.down * Time.deltaTime * 3000);
      return;
    }

    //If speed is larger than maxspeed, cancel out the input so you don't go over max speed
    if (x > 0 && xMag > maxSpeed) x = 0;
    if (x < 0 && xMag < -maxSpeed) x = 0;
    if (y > 0 && yMag > maxSpeed) y = 0;
    if (y < 0 && yMag < -maxSpeed) y = 0;

    //Some multipliers
    float multiplier = 1f, multiplierV = 1f;

    // Movement in air
    if (!grounded) {
      multiplier = 0.5f;
      multiplierV = 0.5f;
    }

    // Movement while sliding
    if (grounded && crouching) multiplierV = 0f;

    if (slamming && grounded) StopSlam();
    else if (slamming) {
      rb.velocity = new Vector3(0f, -100, 0f);
    }

    if (!wallrunning && !slamming) {
      //Apply forces to move player
      rb.AddForce(orientation.forward * y * moveSpeed * Time.deltaTime * multiplier * multiplierV);
      rb.AddForce(orientation.right * x * moveSpeed * Time.deltaTime * multiplier);
    }
  }

  private void Jump(bool dj = false) {
    bool canJump = false;
    if (dj && secondJump && readyToJump) canJump = true;
    else if (!dj && grounded && readyToJump) canJump = true;

    if (canJump) {
      if (dj) secondJump = false;
      readyToJump = false;

      //Add jump forces
      rb.AddForce(Vector2.up * jumpForce * 1.5f);
      rb.AddForce(normalVector * jumpForce * 0.5f);

      //If jumping while falling, reset y velocity.
      Vector3 vel = rb.velocity;
      if (rb.velocity.y < 0.5f) rb.velocity = new Vector3(vel.x, 0, vel.z);
      else if (rb.velocity.y > 0) rb.velocity = new Vector3(vel.x, vel.y / 2, vel.z);

      Invoke(nameof(ResetJump), jumpCooldown);
    }
  }

  private void ResetJump() {
    readyToJump = true;
  }

  private float desiredX;
  private float yaw;
  private float newDesiredX;

  private void CounterMovement(float x, float y, Vector2 mag) {
    if (!grounded || jumping) return;

    //Slow down sliding
    if (crouching) {
      rb.AddForce(moveSpeed * Time.deltaTime * -rb.velocity.normalized * slideCounterMovement);
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
  private void OnCollisionStay(Collision other) {
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

  private void StopGrounded() {
    grounded = false;
  }

  private void StartSlam() { 
    slamming = true;
    StopCrouch();
  }

  private void StopSlam() { 
    slamming = false;
  }

  private void Look() {
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
}
