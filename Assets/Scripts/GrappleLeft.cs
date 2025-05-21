using UnityEngine;
using Fragsurf.Movement;

public class GrappleLeft : MonoBehaviour
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

    private bool _isGrappling = false;
    private Vector3 _grapplePoint;
    private float _currentRopeLength;
    private float _prevY;

    private float _angleAtExtend;
    private float _momentumAtExtend;

    private LineRenderer _lineRenderer;
    private SurfCharacter _surfCharacter;
    private GravityOrbShooter _orbShooter;

    private Vector3 _swingVelocity = Vector3.zero;
    private float _swingForce = 5f;
    private float _maxSwingSpeed = 20f;

    void Start()
    {
        if (hookCamera == null)
            hookCamera = Camera.main;
        if (hookCamera == null)
            Debug.LogError("GrapplingHookLeft: No camera assigned.");

        if (ropeOrigin == null)
            ropeOrigin = hookCamera.transform;

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

        _surfCharacter = GetComponent<SurfCharacter>();
        if (_surfCharacter == null)
            Debug.LogWarning("GrapplingHookLeft: SurfCharacter component not found on this GameObject.");

        _orbShooter = FindObjectOfType<GravityOrbShooter>();
    }

    void Update()
    {
        if (Input.GetKeyDown(grappleKey))
        {
            if ((_orbShooter != null && _orbShooter.IsOrbHeld) || GravityOrbShooter.leftClickConsumed)
                return;
            TryStartGrapple();
        }

        if (_isGrappling && Input.GetKeyDown(cancelKey))
        {
            StopGrapple();
        }

        if (_isGrappling && Input.GetKey(pullKey))
        {
            _currentRopeLength -= pullSpeed * Time.deltaTime;
            if (_currentRopeLength < minRopeLength)
                _currentRopeLength = minRopeLength;
        }
    }

    void LateUpdate()
    {
        if (_isGrappling)
        {
            Vector3 grapple = transform.position - _grapplePoint;
            float distance = grapple.magnitude;

            if (distance > _currentRopeLength)
            {
                transform.position = _grapplePoint + grapple.normalized * _currentRopeLength;
            }

            _prevY = transform.position.y;

            Vector3 swingDirection = (transform.position - _grapplePoint).normalized;
            float swingSpeed = Mathf.Clamp(grapple.magnitude - _currentRopeLength, 0f, _maxSwingSpeed);
            _swingVelocity = swingDirection * swingSpeed * 10;
            transform.position += _swingVelocity * Time.deltaTime;

            // Update rope line
            _lineRenderer.SetPosition(0, ropeOrigin.position);
            _lineRenderer.SetPosition(1, _grapplePoint);
        }
    }

    void TryStartGrapple()
    {
        if (hookCamera == null)
        {
            Debug.LogWarning("GrappleLeft: No camera assigned.");
            return;
        }

        Ray ray = hookCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        Debug.DrawRay(ray.origin, ray.direction * maxGrappleDistance, Color.red, 2f);

        if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer))
        {
            _isGrappling = true;
            _grapplePoint = hit.point;
            _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);
            _lineRenderer.enabled = true;
            Debug.Log("Grapple hit: " + hit.collider.name);

            // You can calculate these later once you finalize how you want to use them
            // _angleAtExtend = Vector3.Angle(Vector3.up, (_grapplePoint - transform.position).normalized);
            // _momentumAtExtend = _surfCharacter.moveData.velocity.magnitude;
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
            Vector3 vel = _surfCharacter.moveData.velocity;
            vel.y = 0;
            _surfCharacter.moveData.velocity = vel;
            _surfCharacter.isGrappling = false;
        }
    }
}
