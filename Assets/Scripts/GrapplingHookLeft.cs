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
    [Tooltip("The camera used for grappling. Defaults to Camera.main if left empty.")]
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
        _lineRenderer.startWidth = 0.2f;
        _lineRenderer.endWidth = 0.2f;

        // Use an unlit shader for constant brightness.
        Material ropeMaterial = new Material(Shader.Find("Unlit/Color"));
        ropeMaterial.color = Color.yellow;
        _lineRenderer.material = ropeMaterial;

        // Optional: Set sorting so it renders on top.
        _lineRenderer.sortingLayerName = "Overlay";
        _lineRenderer.sortingOrder = 1000;

        _lineRenderer.enabled = false;
    }

    void Update()
    {
        // Start grappling when the grapple key is pressed.
        if (Input.GetKeyDown(grappleKey))
            TryStartGrapple();

        // Cancel the grapple when the cancel key is pressed.
        if (_isGrappling && Input.GetKeyDown(cancelKey))
            StopGrapple();

        // While grappling, shorten the rope if the pull key is held.
        if (_isGrappling && Input.GetKey(pullKey))
        {
            _currentRopeLength -= pullSpeed * Time.deltaTime;
            if (_currentRopeLength < minRopeLength)
                _currentRopeLength = minRopeLength;
        }

        // Update the rope's positions if grappling.
        if (_isGrappling)
        {
            _lineRenderer.SetPosition(0, ropeOrigin.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
        }
    }

    void LateUpdate()
    {
        if (_isGrappling)
        {
            // Enforce the rope's length constraint by clamping the player's position.
            Vector3 toPlayer = transform.position - _grapplePoint;
            float distance = toPlayer.magnitude;
            if (distance > _currentRopeLength)
            {
                transform.position = _grapplePoint + toPlayer.normalized * _currentRopeLength;
            }
        }
    }

    void TryStartGrapple()
    {
        if (hookCamera == null)
        {
            Debug.LogWarning("GrapplingHook: No camera assigned.");
            return;
        }

        // Use the center of the screen as the starting point without any offset.
        Ray ray = hookCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Draw a debug ray in the scene view (red) for 2 seconds.
        Debug.DrawRay(ray.origin, ray.direction * maxGrappleDistance, Color.red, 2f);

        // Raycast using the center ray.
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer))
        {
            _isGrappling = true;
            _grapplePoint = hit.point;
            _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);
            _lineRenderer.enabled = true;
            Debug.Log("Grapple hit: " + hit.collider.name);
        }
        else
        {
            Debug.Log("Grapple ray did not hit any target.");
        }
    }

    void StopGrapple()
    {
        _isGrappling = false;
        _lineRenderer.enabled = false;
    }
}
