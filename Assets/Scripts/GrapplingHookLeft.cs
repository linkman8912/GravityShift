using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    [Header("Input Settings")]
    public KeyCode grappleKey = KeyCode.Mouse0;
    public KeyCode pullKey = KeyCode.LeftControl;
    public KeyCode cancelKey = KeyCode.E;

    [Header("Grapple Settings")]
    public float maxGrappleDistance = 50f;
    public float pullSpeed = 10f;
    public float minRopeLength = 2f;
    public LayerMask grappleLayer;

    [Header("References")]
    [Tooltip("Assign the camera used for grappling. Defaults to Camera.main if left empty.")]
    public Camera hookCamera;
    [Tooltip("The transform from which the rope visually originates (e.g., a weapon tip or camera).")]
    public Transform ropeOrigin;

    // Private state
    private bool _isGrappling = false;
    private Vector3 _grapplePoint;
    private float _currentRopeLength;

    // Components
    private LineRenderer _lineRenderer;

    void Start()
    {
        // Ensure a camera is assigned.
        if (hookCamera == null)
            hookCamera = Camera.main;
        if (hookCamera == null)
            Debug.LogError("GrapplingHook: No camera found. Please assign a camera.");

        // If no rope origin is specified, default to the camera's transform.
        if (ropeOrigin == null)
            ropeOrigin = hookCamera.transform;

        // Create and configure the LineRenderer.
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.1f;  // Increased width for better visibility.
        _lineRenderer.endWidth = 0.1f;
        // Use an unlit shader so the rope isn’t affected by lighting.
        _lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
        _lineRenderer.material.color = Color.white;
        // Ensure the rope is drawn on top.
        _lineRenderer.sortingLayerName = "Default";
        _lineRenderer.sortingOrder = 1000;
        _lineRenderer.enabled = false;
    }

    void Update()
    {
        // Fire the grappling hook on left click.
        if (Input.GetKeyDown(grappleKey))
            TryStartGrapple();

        // Cancel the grapple (for example, press "E" to release).
        if (_isGrappling && Input.GetKeyDown(cancelKey))
            StopGrapple();

        // If currently grappling and the pull key is held down, shorten the rope.
        if (_isGrappling && Input.GetKey(pullKey))
        {
            _currentRopeLength -= pullSpeed * Time.deltaTime;
            if (_currentRopeLength < minRopeLength)
                _currentRopeLength = minRopeLength;
        }

        // Update rope visualization.
        if (_isGrappling)
        {
            _lineRenderer.SetPosition(0, ropeOrigin.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
        }
    }

    // LateUpdate ensures the rope constraint is applied after other position updates.
    void LateUpdate()
    {
        if (_isGrappling)
        {
            Vector3 toPlayer = transform.position - _grapplePoint;
            float distance = toPlayer.magnitude;

            // Clamp player position if farther than the current rope length.
            if (distance > _currentRopeLength)
            {
                Vector3 newPos = _grapplePoint + toPlayer.normalized * _currentRopeLength;
                transform.position = newPos;
            }
        }
    }

    // Attempt to start the grapple by raycasting from a rotated ray.
    void TryStartGrapple()
    {
        if (hookCamera == null)
        {
            Debug.LogWarning("GrapplingHook: No camera assigned.");
            return;
        }

        // Get the original ray from the mouse position.
        Ray originalRay = hookCamera.ScreenPointToRay(Input.mousePosition);

        // First, rotate 50° to the left (negative means left rotation).
        Quaternion leftRotation = Quaternion.AngleAxis(-50f, hookCamera.transform.up);
        // Then, tilt down 15° by rotating around the camera's right axis.
        Quaternion downRotation = Quaternion.AngleAxis(-30f, hookCamera.transform.right);
        // Combine the rotations (order matters; this applies the down rotation first, then the left rotation).
        Quaternion combinedRotation = leftRotation * downRotation;
        // Apply the combined rotation to the original ray's direction.
        Vector3 rotatedDirection = combinedRotation * originalRay.direction;

        // Create a new ray using the original origin and the rotated direction.
        Ray ray = new Ray(originalRay.origin, rotatedDirection);

        // Perform the raycast.
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer))
        {
            _isGrappling = true;
            _grapplePoint = hit.point;
            _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);
            _lineRenderer.enabled = true;
        }
    }

    // Cancel the grapple.
    void StopGrapple()
    {
        _isGrappling = false;
        _lineRenderer.enabled = false;
    }
}
