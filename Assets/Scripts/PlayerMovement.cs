using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float jumpForce = 5f;
    public float gravity = 9.81f;

    [Header("Look Settings")]
    public float mouseSensitivity = 100f;
    public Transform playerCamera;
    public float maxLookAngle = 90f;

    private CharacterController characterController;
    private float verticalRotation = 0f;
    private Vector3 moveDirection = Vector3.zero;

    void Start()
    {
        characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
    }

    void HandleMouseLook()
    {
        // Horizontal rotation (turning left/right)
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        transform.Rotate(Vector3.up * mouseX);

        // Vertical rotation (looking up/down)
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        verticalRotation -= mouseY;
        verticalRotation = Mathf.Clamp(verticalRotation, -maxLookAngle, maxLookAngle);
        playerCamera.localRotation = Quaternion.Euler(verticalRotation, 0f, 0f);
    }

    void HandleMovement()
    {
        // Grounded check
        bool isGrounded = characterController.isGrounded;

        // Horizontal movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        characterController.Move(move * walkSpeed * Time.deltaTime);

        // Jumping
        if (isGrounded)
        {
            moveDirection.y = -0.5f; // Small force to keep player grounded

            if (Input.GetButtonDown("Jump"))
            {
                moveDirection.y = jumpForce;
            }
        }
        else
        {
            // Apply gravity
            moveDirection.y -= gravity * Time.deltaTime;
        }

        // Apply vertical movement
        characterController.Move(moveDirection * Time.deltaTime);
    }
}