using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Wallrunning : MonoBehaviour
{
  [Header("Wallrunning")]
  [SerializeField] private LayerMask whatIsWall;
  private LayerMask whatIsGround;
  [SerializeField] private float wallRunForce = 500;
  [SerializeField] private float maxWallRunTime = 1.5f;
  private float wallRunTimer;
  [Header("Walljumping")]
  [SerializeField] private float walljumpUpForce = 100f;
  [SerializeField] private float walljumpSideForce = 50f;

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
    if ((wallLeft || wallRight) && verticalInput > 0 && AboveGround()) {
      if(!pm.wallrunning) {
        StartWallrun();
      }
    }
    if (pm.wallrunning) {
      if (wallRunTimer < maxWallRunTime) {
        wallRunTimer += Time.deltaTime;
      }
      else {
        StopWallrun();
      }
      if (Input.GetButtonDown("Jump")) {
        StopWallrun();
        Walljump();
      }
    }
  }

  void StartWallrun() {
    pm.wallrunning = true;
    Debug.Log("Starting wallrun");
  }
  void WallrunningMovement() {
    rb.useGravity = false;
    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;
    Vector3 wallForward = Vector3.Cross(wallNormal, transform.up);

    if((orientation.forward - wallForward).magnitude > (orientation.forward - -wallForward).magnitude)
      wallForward = -wallForward;

    // forward force
    rb.AddForce(wallForward * wallRunForce, ForceMode.Force);
    rb.AddForce(-wallNormal * wallRunForce/2, ForceMode.Force);
  }
  void StopWallrun() {
    pm.wallrunning = false;
    Debug.Log("Stopping wallrun");
  }
  
  void Walljump() {
    Debug.Log("Walljump");
    Vector3 wallNormal = wallRight ? rightWallHit.normal : leftWallHit.normal;

    Vector3 forceToApply = transform.up * walljumpUpForce / 10 + wallNormal * walljumpSideForce / 10;
    // reset force
    rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
    // add force
    rb.AddForce(forceToApply, ForceMode.Impulse);
  }
}
