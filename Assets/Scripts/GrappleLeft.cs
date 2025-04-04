using UnityEngine;
using Fragsurf.Movement

public class GrappleLeft : MonoBehaviour {
  [Header("Input Settings)]
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

  // Components
  private LineRenderer _lineRenderer;
  private SurfCharacter _surfCharacter;
  private GravityOrbShooter _orbShooter; // Reference to GravityOrbShooter

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
    if (_isgrappling) {
      Vector3 grapple = transform.position - _grapplePoint;
      float distance = toPlayer.magnitude;
      if (distance > _currentRopeLength) {
        transform.position = grapple.Normalized() * _currentRopeLength;
      }
    }
  }
}
