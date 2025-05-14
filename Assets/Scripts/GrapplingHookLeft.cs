using UnityEngine;
using Fragsurf.Movement;  // Ensure this matches your project's namespace

public class GrapplingHookLeft : MonoBehaviour
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

  [Header("Rope Extension Settings")]
  [Tooltip("Speed at which the rope automatically extends when falling.")]
  public float extendSpeed = 5f;
  [Tooltip("Maximum rope length when extended.")]
  public float maxExtendedRopeLength = 70f;

  [Header("Swing Settings")]
  [Tooltip("Base extra force applied to swing the player toward the other side of the rope.")]
  public float swingForce = 5f;

  [Header("Fall Settings")]
  [Tooltip("Vertical velocity to set upon releasing the grapple (e.g. 0 to cancel falling momentum).")]
  public float resetVerticalVelocity = 0f;

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
  private GravityOrbShooter _orbShooter; // Reference to GravityOrbShooter

  private float _angleAtExtend; // for use when the player extends the grappling hook to its full length each time, it should be reset when slack is added to the rope and it will hold the angle between vertical and the vector from the player to the grapple point. This should be used to set a maximum angle for a swing which, when it is reached, the player's momentum will be gradually slowed until they stop and start swinging the other way.

  void Start()
  {
    // Ensure a camera is assigned.
    if (hookCamera == null)
      hookCamera = Camera.main;
    if (hookCamera == null)
      Debug.LogError("GrapplingHookLeft: No camera assigned.");

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
      Debug.LogWarning("GrapplingHookLeft: SurfCharacter component not found on this GameObject.");

    // Get reference to GravityOrbShooter.
    _orbShooter = FindObjectOfType<GravityOrbShooter>();
  }

  void Update()
  {
    // Only try to grapple if the left click is pressed and no orb is held,
    // and also if the left click wasn't consumed by the orb shooter.
    if (Input.GetKeyDown(grappleKey))
    {
      if ((_orbShooter != null && _orbShooter.IsOrbHeld) || GravityOrbShooter.leftClickConsumed)
        return;
      TryStartGrapple();
    }

    // Release the grapple when the cancel key is pressed.
    if (_isGrappling && Input.GetKeyDown(cancelKey))
      StopGrapple();

    // Allow rope shortening if the pull key is held.
    if (_isGrappling && Input.GetKey(pullKey))
    {
      _currentRopeLength -= pullSpeed * Time.deltaTime;
      if (_currentRopeLength < minRopeLength)
        _currentRopeLength = minRopeLength;
    }

    // Update the rope visualization.
    if (_isGrappling)
    {
      _lineRenderer.SetPosition(0, ropeOrigin.position);
      _lineRenderer.SetPosition(1, _grapplePoint);
    }

    // Option 2: Reset the consumed flag at the end of Update.
    GravityOrbShooter.leftClickConsumed = false;
  }

  void LateUpdate()
  {
    if (_isGrappling)
    {
      // Enforce the rope's length: clamp the player's distance to the grapple point.
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

      // When at the rope's end and falling, apply extra lateral swing force.
      if (distance >= _currentRopeLength - 0.1f && verticalVelocity < 0)
      {
        Vector3 ropeDir = (transform.position - _grapplePoint).normalized;
        Vector3 tangential = Vector3.down - Vector3.Dot(Vector3.down, ropeDir) * ropeDir;
        if (tangential.sqrMagnitude > 0.001f)
        {
          tangential.Normalize();
          float multiplier = 1f + Mathf.Clamp(-verticalVelocity, 0f, 20f) / 10f;
          transform.position += tangential * swingForce * multiplier * Time.deltaTime;
        }
      }
    }
  }

  void TryStartGrapple()
  {
    if (hookCamera == null)
    {
      Debug.LogWarning("GrapplingHookLeft: No camera assigned.");
      return;
    }

    // Cast a ray from the center of the screen.
    Ray ray = hookCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    Debug.DrawRay(ray.origin, ray.direction * maxGrappleDistance, Color.red, 2f);

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
    if (_surfCharacter != null)
    {
      Vector3 vel = _surfCharacter.moveData.velocity;
      vel.y = resetVerticalVelocity;
      _surfCharacter.moveData.velocity = vel;
      _surfCharacter.isGrappling = false;
    }
  }
}
