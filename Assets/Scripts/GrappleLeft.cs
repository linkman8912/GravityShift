using UnityEngine;
using Fragsurf.Movement;

public class GrappleLeft : MonoBehaviour {
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
  private float _prevY;

  private float _angleAtExtend; // for use when the player extends the grappling hook to its full length each time, it should be reset when slack is added to the rope and it will hold the angle from vertical to the vector from the player to the grapple point. This should be used to set a maximum angle for a swing, which, when it is reached, the player's momentum will be gradually slowed until they stop and start swinging the other way. (uncertain how I will do this, problem for later)
  private float _momentumAtExtend; // same as angleatextend but for a momentum number, this should be kept at the same (possibly with a multiplier added) but redirected based on the player's angle to the rope. this should be stopped at some high point, maybe when the player is level with the grapple point, likely earlier, when the rope should just be broken

  // Components
  private LineRenderer _lineRenderer;
  private SurfCharacter _surfCharacter;
  private GravityOrbShooter _orbShooter; // Reference to GravityOrbShooter

  private Vector3 _swingVelocity = Vector3.zero;
  private float _swingForce = 5f; // A force to control the swing speed
  private float _maxSwingSpeed = 20f; // Maximum swing speed to cap it

  void Start() {
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

    // Get a reference to the SurfCharacter component.
    _surfCharacter = GetComponent<SurfCharacter>();
    if (_surfCharacter == null)
      Debug.LogWarning("GrapplingHookLeft: SurfCharacter component not found on this GameObject.");

    // Get reference to GravityOrbShooter.
    _orbShooter = FindObjectOfType<GravityOrbShooter>();
  }

  void Update() {
    // Only try to grapple if the left click is pressed and no orb is held,
    // and also if the left click wasn't consumed by the orb shooter.
    if (Input.GetKeyDown(grappleKey)) {
      if ((_orbShooter != null && _orbShooter.IsOrbHeld) || GravityOrbShooter.leftClickConsumed)
        return;
      TryStartGrapple();
    }

    // Release the grapple when the cancel key is pressed.
    if (_isGrappling && Input.GetKeyDown(cancelKey)) {
      StopGrapple();
    }
    // Allow rope shortening if the pull key is held.
    if (_isGrappling && Input.GetKey(pullKey)) {
      _currentRopeLength -= pullSpeed * Time.deltaTime;
      if (_currentRopeLength < minRopeLength)
        _currentRopeLength = minRopeLength;
    }
  }
  void LateUpdate() {
    if (_isGrappling) {
      Vector3 grapple = transform.position - _grapplePoint;
      float distance = grapple.magnitude;
      if (distance > _currentRopeLength) {
        transform.position = grapple.normalized * _currentRopeLength;
      }

      // float verticalVelocity = (transform.position.y - _prevY / Time.deltaTime);
      _prevY = transform.position.y;
      
      /*
      // new lines:
      
      // Calculate the direction to swing
      Vector3 swingDirection = (transform.position - _grapplePoint).normalized;

      // Calculate the velocity based on distance from the grapple point (swing speed)
      float swingSpeed = Mathf.Clamp(grapple.magnitude - _currentRopeLength, 0f, _maxSwingSpeed); // Swing speed based on rope slack

      // Calculate the swing force (speed) applied based on distance
      _swingVelocity = swingDirection * swingSpeed * 10;

      // Apply the swing velocity to the player, updating their position
      transform.position += _swingVelocity * Time.deltaTime;

      // Optionally, you can apply gravity manually if needed, to make the swing feel more natural
      /*if (!Physics.Raycast(transform.position, Vector3.down, 1f)) {
      // Simulate gravity by applying a small downward velocity (this can be adjusted)
      transform.position += Vector3.down * 9.81f * Time.deltaTime;
      }*/
    }

  }
  void TryStartGrapple() {
    if (hookCamera == null) {
      Debug.LogWarning("GrappleLeft: No camera assigned.");
      return;
    }
    
    Ray ray = hookCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
    Debug.DrawRay(ray.origin, ray.direction * maxGrappleDistance, Color.red, 2f);

    if (Physics.Raycast(ray, out RaycastHit hit, maxGrappleDistance, grappleLayer)) {
      _isGrappling = true;
      _grapplePoint = hit.point;
      _currentRopeLength = Vector3.Distance(transform.position, _grapplePoint);
      _lineRenderer.enabled = true;
      Debug.Log("Grapple hit: " + hit.collider.name);
    }
    else {
      Debug.Log("Grapple ray did not hit any target.");
    }
    _angleAtExtend = Vector3.Angle(
    _momentumAtExtend = 
  }
  void StopGrapple() {
    _isGrappling = false;
    _lineRenderer.enabled = false;
    if (_surfCharacter != null) {
      Vector3 vel = _surfCharacter.moveData.velocity;
      vel.y = 0;
      _surfCharacter.moveData.velocity = vel;
      _surfCharacter.isGrappling = false;
    }
  }
}
