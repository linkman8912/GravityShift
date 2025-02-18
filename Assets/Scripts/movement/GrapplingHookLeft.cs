using UnityEngine;
using Fragsurf.Movement;  // Ensure this matches your namespace

public class GrapplingHook : MonoBehaviour
{
    [Header("Input Settings")]
    public KeyCode grappleKey = KeyCode.Mouse0;
    public KeyCode pullKey = KeyCode.LeftControl;
    public KeyCode cancelKey = KeyCode.E;

    [Header("Grapple Settings")]
    public float maxGrappleDistance = 50f;
    public float pullSpeed = 30f;
    public float minRopeLength = 2f;
    public LayerMask grappleLayer;

    [Header("Rope Extension Settings")]
    [Tooltip("Speed at which the rope automatically extends when falling.")]
    public float extendSpeed = 5f;
    [Tooltip("Maximum rope length when extended.")]
    public float maxExtendedRopeLength = 70f;

    [Header("Swing Settings")]
    [Tooltip("Extra force applied to swing the player toward the other side of the rope.")]
    public float swingForce = 5f;

    [Header("Fall Settings")]
    [Tooltip("Vertical speed to reset to on grapple release.")]
    public float normalFallSpeed = -9.81f;

    [Header("References")]
    [Tooltip("The camera used for grappling. Defaults to Camera.main if left empty.")]
    public Camera hookCamera;
    [Tooltip("The transform from which the rope visually originates (e.g., a weapon tip or camera).")]
    public Transform ropeOrigin;

    // Private state
    private bool _isGrappling = false;
    private Vector3 _grapplePoint;
    private float _currentRopeLength;
    private float _prevY;

    // Components
    private LineRenderer _lineRenderer;
    private SurfCharacter _surfCharacter;

    void Start()
    {
        // Ensure a camera is assigned.
        if (hookCamera == null)
            hookCamera = Camera.main;
        if (hookCamera == null)
            Debug.LogError("GrapplingHook: No camera assigned.");

        // Default rope origin to the camera's transform if not set.
        if (ropeOrigin == null)
            ropeOrigin = hookCamera.transform;

        // Set up the LineRenderer.
        _lineRenderer = gameObject.AddComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;
        _lineRenderer.startWidth = 0.2f;
        _lineRenderer.endWidth = 0.2f;
        Material ropeMaterial = new Material(Shader.Find("Unlit/Color"));
        ropeMaterial.color = Color.yellow;
        _lineRenderer.material = ropeMaterial;
        _lineRenderer.sortingLayerName = "Overlay";
        _lineRenderer.sortingOrder = 1000;
        _lineRenderer.enabled = false;

        _prevY = transform.position.y;

        // Get a reference to the SurfCharacter component.
        _surfCharacter = GetComponent<SurfCharacter>();
        if (_surfCharacter == null)
        {
            Debug.LogWarning("GrapplingHook: SurfCharacter component not found on this GameObject.");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(grappleKey))
            TryStartGrapple();

        if (_isGrappling && Input.GetKeyDown(cancelKey))
            StopGrapple();

        if (_isGrappling && Input.GetKey(pullKey))
        {
            _currentRopeLength -= pullSpeed * Time.deltaTime;
            if (_currentRopeLength < minRopeLength)
                _currentRopeLength = minRopeLength;
        }

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
            // Enforce rope length constraint.
            Vector3 toPlayer = transform.position - _grapplePoint;
            float distance = toPlayer.magnitude;
            if (distance > _currentRopeLength)
            {
                transform.position = _grapplePoint + toPlayer.normalized * _currentRopeLength;
            }

            // Compute vertical velocity.
            float verticalVelocity = (transform.position.y - _prevY) / Time.deltaTime;
            _prevY = transform.position.y;

            // Automatically extend the rope when falling.
            if (verticalVelocity < 0)
            {
                _currentRopeLength += extendSpeed * Time.deltaTime;
                _currentRopeLength = Mathf.Min(_currentRopeLength, maxExtendedRopeLength);
            }

            // Apply extra lateral swing force if at rope's end and falling.
            if (distance >= _currentRopeLength - 0.1f && verticalVelocity < 0)
            {
                Vector3 ropeDir = (transform.position - _grapplePoint).normalized;
                Vector3 tangential = Vector3.down - Vector3.Dot(Vector3.down, ropeDir) * ropeDir;
                if (tangential.sqrMagnitude > 0.001f)
                {
                    tangential.Normalize();
                    transform.position += tangential * swingForce * Time.deltaTime;
                }
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

        // Use the center of the screen.
        Ray ray = hookCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Debug.DrawRay(ray.origin, ray.direction * maxGrappleDistance, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer))
        {
            _isGrappling = true;
            _grapplePoint = hit.point;
            _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);
            _lineRenderer.enabled = true;
            if (_surfCharacter != null)
            {
                _surfCharacter.isGrappling = true;
            }
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
        if (_surfCharacter != null)
        {
            _surfCharacter.isGrappling = false;
            // Reset the vertical velocity in moveData so that normal falling resumes.
            Vector3 vel = _surfCharacter.moveData.velocity;
            vel.y = normalFallSpeed;
            _surfCharacter.moveData.velocity = vel;
        }
    }
}
