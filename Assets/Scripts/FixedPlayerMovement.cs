using UnityEngine;

public class FixedPlayerMovement : MonoBehaviour {
  [Header("References")]
  public Transform playerCam;
  public Transform orientation;
  public Rigidbody rb;

  [Header("Movement")]
  public float moveSpeed = 5f;

  [Header("Mouse Look")]
  public float sensitivity = 50f;
  public float sensMultiplier = 1f;

  float yaw = 0f;
  float pitch = 0f;

  void Update() {
    Look();
  }

  void FixedUpdate() {
    Move();
  }

  void Look() {
    float mouseX = Input.GetAxisRaw("Mouse X") * sensitivity * Time.deltaTime * sensMultiplier;
    float mouseY = Input.GetAxisRaw("Mouse Y") * sensitivity * Time.deltaTime * sensMultiplier;

    yaw   += mouseX;
    pitch -= mouseY;
    pitch = Mathf.Clamp(pitch, -90f, 90f);

    // Apply rotations
    orientation.localRotation = Quaternion.Euler(0f, yaw, 0f);
    playerCam.localRotation   = Quaternion.Euler(pitch, 0f, 0f);
  }

  void Move() {
    float horizontalInput = Input.GetAxisRaw("Horizontal");
    float verticalInput = Input.GetAxisRaw("Vertical");

    Vector3 moveDirection = orientation.forward * verticalInput + orientation.right * horizontalInput;
    rb.AddForce(moveDirection.normalized * moveSpeed * 10f, ForceMode.Force);
  }
}
