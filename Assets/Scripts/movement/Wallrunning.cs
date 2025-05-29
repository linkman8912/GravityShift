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
  [SerializeField] private float wallrunDelay = 0.3f;
  //[SerializeField] private float walljumpDelayTime = 0.75f;
  private float wallrunTimer;
  //private float walljumpDelayTimer;
  private bool readyToWallrun = true;
  [Header("Walljumping")]
  [SerializeField] private float walljumpUpForce = 100f;
  [SerializeField] private float walljumpSideForce = 50f;
  private GameObject mostRecentWalljump;

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
  private PlayerMovement pm;
  private Rigidbody rb;


  // Start is called before the first frame update
  void Start() {
    rb = GetComponent<Rigidbody>();
    pm = GetComponent<PlayerMovement>();
    whatIsGround = pm.whatIsGround;
  }

  // Update is called once per frame
  void Update() {
    StateMachine();
    if (pm.grounded) mostRecentWalljump = null;
  }

  void FixedUpdate() {
    CheckForWall();
  }

  private void CheckForWall() {
    wallRight = Physics.Raycast(transform.position, orientation.right, out rightWallHit, wallCheckDistance, whatIsWall);
    wallLeft = Physics.Raycast(transform.position, -orientation.right, out leftWallHit, wallCheckDistance, whatIsWall);
  }


  private bool AboveGround() {
    return !Physics.Raycast(transform.position, Vector3.down, minJumpHeight, whatIsGround);
  }

  void StateMachine() {
    // handle inputs
    horizontalInput = Input.GetAxisRaw("Horizontal");
    verticalInput = Input.GetAxisRaw("Vertical");

    // state 1: wallrunning
    if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround() && readyToWallrun) {
      if(!pm.wallrunning) {
        StartWallrun();
      }
      WallrunningMovement();
      if (wallrunTimer < maxWallrunTime) {
        wallrunTimer += Time.deltaTime;
      }
      else {
        StopWallrun();
      }
      if (Input.GetButtonDown("Jump")) {
        StopWallrun();
        Walljump();
      }
    }
    else if (pm.wallrunning) {
      StopWallrun();
    }
  }

  void StartWallrun() {
    pm.wallrunning = true;
    //Debug.Log("Starting wallrun");

  }
  void WallrunningMovement() {
    Debug.Log("wallrunning movement");
    rb.useGravity = false;
    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
    Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

    if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
      wallForward = -wallForward;
    if (Vector3.Angle(rb.velocity.normalized, wallForward) <= wallMomentumAngle) {
      rb.velocity = rb.velocity.magnitude * wallForward;
    }

    // forward force
    rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
    rb.AddForce(-wallNormal * wallRunForce/2, ForceMode.Force);
  }
  void StopWallrun() {
    pm.wallrunning = false;
    //Debug.Log("Stopping wallrun");
  }

  void Walljump() {
    //Debug.Log("Walljump");
    Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
    GameObject currentTarget = wallRight ? rightWallHit.collider.gameObject : leftWallHit.collider.gameObject;
    if (currentTarget == mostRecentWalljump) return;

    Vector3 forceToApply = transform.up * walljumpUpForce / 10 + wallNormal * walljumpSideForce / 10;
    // reset force
    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    // add force
    rb.AddForce(forceToApply, ForceMode.Impulse);
    mostRecentWalljump = currentTarget;
    readyToWallrun = false;
    Invoke("ResetWallrunDelay", wallrunDelay);
  }

  void ResetWallrunDelay() {
    Debug.Log("ready to wallrun");
    readyToWallrun = true;
  }
}
