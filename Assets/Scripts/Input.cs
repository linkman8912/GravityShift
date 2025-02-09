using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// a lot of this is stolen from https://discussions.unity.com/t/how-to-correctly-setup-3d-character-movement-in-unity/811250/2

public class PlayerInput : MonoBehaviour {

  private CharacterController charController;
  private Vector3 moveDirection;
  private float groundedTimer;
  private float deltaY;

  public float moveSpeed = 6f; // Adjust to match TF2's feel
  public float jumpForce = 5f; // Source-like jump force
  public float gravity = 20f;

  void Start() {
    charController = GetComponent<CharacterController>();
  }

  void Update() {

    bool groundedPlayer = charController.isGrounded;
    if (groundedPlayer) {
      // cooldown interval to allow reliable jumping even whem coming down ramps
      groundedTimer = 0.2f;
    }

    // handle crouch
    //if (Input.GetButtonDown("

    if (groundedTimer > 0) {
      groundedTimer -= Time.deltaTime;
    }

    // slam into the ground
    if (groundedPlayer && deltaY < 0) {
      // hit ground
      deltaY = 0f;
    }

    // apply gravity always, to let us track down ramps properly
    deltaY -= gravity * Time.deltaTime;

    Vector3 movement = new Vector3(Input.GetAxis("Horizontal") * moveSpeed, 0, Input.GetAxis("Vertical") * moveSpeed);
    // only align to motion if we are providing enough input
    //if (movement.magnitude > 0.05f) { 
    //  gameObject.transform.forward = movement;
    //}

    // allow jump as long as the player is on the ground
    if (Input.GetButtonDown("Jump")) {
      // no more until we recontact ground
      if (groundedTimer > 0) {
        groundedTimer = 0;
        // Physics dynamics formula for calculating jump up velocity based on height and gravity
        deltaY += Mathf.Sqrt(jumpForce * 2 * gravity);
      }
    }

    // Vector3 movement = new Vector3(Input.GetAxis("Horizontal") * moveSpeed, 0, Input.GetAxis("Vertical") * moveSpeed);
    movement = Vector3.ClampMagnitude(movement, moveSpeed);
    movement = transform.TransformDirection(movement);
    movement.y = deltaY;
    charController.Move(movement * Time.deltaTime);

    // Vertical movement
    //float verticalInput = Input.GetAxis("Vertical");
    //moveDirection.y = verticalInput * moveSpeed;

    // Air control
    //if (! grounded && Input.GetAxis("Horizontal") != 0)
    //    charController.AddForce(moveDirection.normalized * -charController.mass, ForceMode.VelocityChange);

    //charController.MoveRotation(charController.rotation * Quaternion.Euler(AxisX: Input.GetAxis("Mouse X"), AxisY: Input.GetAxis("Mouse Y")));
    
  /*
    // gather lateral input control
    Vector3 movement = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
    movement *= moveSpeed;

    // only align to motion if we are providing enough input
    if (movement.magnitude > 0.05f) { 
      gameObject.transform.forward = movement;
    }

    // allow jump as long as the player is on the ground
    if (Input.GetButtonDown("Jump")) {
      // no more until we recontact ground
      if (groundedTimer > 0) {
        groundedTimer = 0;
        // Physicss dynamics formula for calculating jump up velocity based on height and gravity
        verticalVelocity += Mathf.Sqrt(jumpForce * 2 * gravity);
      }
    }

    movement.y = verticalVelocity;
    charController.Move(movement * Time.deltaTime);
    */

    /*
    float deltaY = gravity;
    if (Input.GetButton("Jump") && charController.isGrounded) {
      // StandardJump();
      Debug.Log("jump");
      deltaY = jumpForce;
    }
  */
  }
}
