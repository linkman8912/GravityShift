using UnityEngine;

public class GrapplingHook : MonoBehaviour
{
    [Header("Input Settings")]
    [Tooltip("Key to fire the grappling hook.")]
    public KeyCode grappleKey = KeyCode.Mouse0;
    [Tooltip("Key to pull the player toward the grapple point.")]
    public KeyCode pullKey = KeyCode.LeftControl;
    [Tooltip("Key to cancel/release the grapple.")]
    public KeyCode cancelKey = KeyCode.E;

    [Header("Grapple Settings")]
    [Tooltip("Maximum distance the grappling hook can reach.")]
    public float maxGrappleDistance = 50f;
    [Tooltip("Speed at which the rope shortens when pulling.")]
    public float pullSpeed = 10f;
    [Tooltip("Minimum rope length allowed when pulling.")]
    public float minRopeLength = 2f;
    [Tooltip("LayerMask to specify which layers are grappleable.")]
    public LayerMask grappleLayer;

    [Header("References")]
    [Tooltip("Assign the camera used for grappling. If left empty, it will default to Camera.main.")]
    public Camera hookCamera;

    // Private state
    private bool _isGrappling = false;
    private Vector3 _grapplePoint;
    private float _currentRopeLength;

    // Components
    private LineRenderer _lineRenderer;

    void Start()
    {
        // If no camera is assigned, try to find the main camera.
        if (hookCamera == null)
        {
            hookCamera = Camera.main;
        }

        if (hookCamera == null)
        {
            Debug.LogError("GrapplingHook: No camera found. Please assign a camera to the GrapplingHook script.");
        }

        // Set up a simple LineRenderer to draw the rope.
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.05f;
        _lineRenderer.endWidth = 0.05f;
        _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        _lineRenderer.enabled = false;
    }

    void Update()
    {
        // Fire the grappling hook on left click.
        if (Input.GetKeyDown(grappleKey))
        {
            TryStartGrapple();
        }

        // Cancel grapple (for example, press "E" to release).
        if (_isGrappling && Input.GetKeyDown(cancelKey))
        {
            StopGrapple();
        }

        // If currently grappling and the pull key is held down, shorten the rope.
        if (_isGrappling && Input.GetKey(pullKey))
        {
            _currentRopeLength -= pullSpeed * Time.deltaTime;
            if (_currentRopeLength < minRopeLength)
            {
                _currentRopeLength = minRopeLength;
            }
        }

        // Update rope visualization.
        if (_isGrappling)
        {
            _lineRenderer.SetPosition(0, transform.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
        }
    }

    // LateUpdate runs after your SurfCharacter controller’s Update.
    void LateUpdate()
    {
        if (_isGrappling)
        {
            Vector3 toPlayer = transform.position - _grapplePoint;
            float distance = toPlayer.magnitude;

            // Clamp player position if they're farther than the rope's length.
            if (distance > _currentRopeLength)
            {
                Vector3 newPos = _grapplePoint + toPlayer.normalized * _currentRopeLength;
                transform.position = newPos;
            }
        }
    }

    // Attempt to start the grapple by raycasting from the camera.
    void TryStartGrapple()
    {
        Debug.Log("trying to grapple");
        if (hookCamera == null)
        {
            Debug.LogWarning("GrapplingHook: No camera found. Cannot start grapple.");
            return;
        }

        Ray ray = hookCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer))
        {
            _isGrappling = true;
            _grapplePoint = hit.point;
            _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);

            // Enable rope visualization.
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
