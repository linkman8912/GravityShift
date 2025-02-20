using UnityEngine;
using System.Collections.Generic;

public class GrappleRight : MonoBehaviour
{
    [Header("Grapple Settings")]
    public float maxGrappleDistance = 50f;
    public float springForce = 100f;
    public float damperForce = 10f;
    public float connectionSpeed = 5f;
    public LayerMask grappleLayer;
    public Material lineMaterial;

    [Header("Grapple Behavior Settings")]
    [Tooltip("If an object’s layer is in this mask, the grapple will attach to its center instead of the hit point.")]
    public LayerMask centerGrappleLayer; // New layer mask for center grappling

    [Header("References")]
    public Camera playerCamera;
    public KeyCode releaseKey = KeyCode.R;

    [Header("Debug Settings")]
    public bool enableDebug = true;
    public Color validTargetColor = Color.green;
    public Color invalidTargetColor = Color.red;

    // Private state
    private List<GrapplePair> activeGrapples = new List<GrapplePair>();
    private RaycastHit firstHit;
    private bool isFirstTargetSelected = false;

    // Reference to GravityOrbShooter to check orb state.
    private GravityOrbShooter _orbShooter;

    void Start()
    {
        // Get reference to GravityOrbShooter in the scene.
        _orbShooter = FindObjectOfType<GravityOrbShooter>();

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                Debug.LogError("GrappleRight: No camera assigned and no main camera found.");
            }
        }
    }

    void Update()
    {
        HandleGrappleInput();
        HandleGrappleRelease();
        UpdateLineRenderers();
    }

    void HandleGrappleInput()
    {
        // Prevent grapple input if an orb is held or if the orb shooter consumed the right-click.
        if ((_orbShooter != null && _orbShooter.IsOrbHeld) || GravityOrbShooter.leftClickConsumed)
            return;

        if (Input.GetMouseButtonDown(1))
        {
            if (Physics.Raycast(playerCamera.transform.position,
                                  playerCamera.transform.forward,
                                  out RaycastHit hit,
                                  maxGrappleDistance,
                                  grappleLayer))
            {
                if (enableDebug)
                {
                    Debug.Log("Raycast hit: " + hit.collider.name);
                    Debug.DrawRay(playerCamera.transform.position,
                                  playerCamera.transform.forward * hit.distance,
                                  validTargetColor, 0.1f);
                }

                if (hit.rigidbody != null && !hit.rigidbody.isKinematic)
                {
                    if (!isFirstTargetSelected)
                    {
                        firstHit = hit;
                        isFirstTargetSelected = true;
                        if (enableDebug)
                            Debug.Log($"FIRST TARGET SET: {hit.collider.name} at {hit.point}");
                    }
                    else
                    {
                        if (enableDebug)
                            Debug.Log($"CREATING CONNECTION BETWEEN: {firstHit.collider.name} and {hit.collider.name}");
                        CreateGrappleConnection(firstHit, hit);
                        isFirstTargetSelected = false;
                    }
                }
                else if (enableDebug)
                {
                    Debug.LogWarning(hit.rigidbody == null ?
                        $"Invalid target: {hit.collider.name} (No Rigidbody)" :
                        $"Invalid target: {hit.collider.name} (Kinematic)");
                }
            }
            else
            {
                if (enableDebug)
                {
                    Debug.DrawRay(playerCamera.transform.position,
                                  playerCamera.transform.forward * maxGrappleDistance,
                                  invalidTargetColor, 0.1f);

                    if (isFirstTargetSelected)
                    {
                        Debug.Log("Clearing first target - no valid second target found");
                        isFirstTargetSelected = false;
                    }
                    else
                    {
                        Debug.Log("No valid grapple target detected");
                    }
                }
            }
        }
    }

    void CreateGrappleConnection(RaycastHit startHit, RaycastHit endHit)
    {
        SpringJoint spring = startHit.rigidbody.gameObject.AddComponent<SpringJoint>();
        spring.connectedBody = endHit.rigidbody;
        spring.spring = springForce;
        spring.damper = damperForce;
        spring.autoConfigureConnectedAnchor = false;
        spring.enableCollision = true; // Enable collisions between connected bodies

        // Determine the local anchor point for the first object.
        Vector3 localHitPointA;
        if ((centerGrappleLayer.value & (1 << startHit.collider.gameObject.layer)) != 0)
        {
            localHitPointA = Vector3.zero;
            if (enableDebug) Debug.Log($"Using center for {startHit.collider.name}");
        }
        else
        {
            localHitPointA = startHit.transform.InverseTransformPoint(startHit.point);
        }
        spring.anchor = localHitPointA;

        // Determine the local anchor point for the second object.
        Vector3 localHitPointB;
        if ((centerGrappleLayer.value & (1 << endHit.collider.gameObject.layer)) != 0)
        {
            localHitPointB = Vector3.zero;
            if (enableDebug) Debug.Log($"Using center for {endHit.collider.name}");
        }
        else
        {
            localHitPointB = endHit.transform.InverseTransformPoint(endHit.point);
        }
        spring.connectedAnchor = localHitPointB;

        // Create a line renderer to visualize the grapple.
        LineRenderer lr = new GameObject("GrappleLine").AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = lineMaterial;
        lr.startWidth = 0.1f;
        lr.endWidth = 0.1f;

        activeGrapples.Add(new GrapplePair(
            startHit.rigidbody,
            endHit.rigidbody,
            spring,
            lr
        ));

        if (enableDebug)
        {
            Debug.Log($"New Grapple Created!\n" +
                      $"Object A: {startHit.collider.name} (Mass: {startHit.rigidbody.mass})\n" +
                      $"Object B: {endHit.collider.name} (Mass: {endHit.rigidbody.mass})\n" +
                      $"Anchor Points:\nA: {spring.anchor}\nB: {spring.connectedAnchor}");
        }
    }

    void HandleGrappleRelease()
    {
        if (Input.GetKeyDown(releaseKey))
        {
            if (enableDebug)
                Debug.Log($"Releasing all ({activeGrapples.Count}) grapples");

            foreach (GrapplePair pair in activeGrapples)
            {
                if (enableDebug)
                    Debug.Log($"Removing connection: {pair.objectA.name} <-> {pair.objectB.name}");

                if (pair.springJoint != null)
                    Destroy(pair.springJoint);

                if (pair.lineRenderer != null)
                    Destroy(pair.lineRenderer.gameObject);
            }
            activeGrapples.Clear();
            isFirstTargetSelected = false;
        }
    }

    void UpdateLineRenderers()
    {
        foreach (GrapplePair pair in activeGrapples)
        {
            try
            {
                if (pair.lineRenderer != null && pair.objectA != null && pair.objectB != null)
                {
                    Vector3 startPoint = pair.objectA.transform.TransformPoint(pair.springJoint.anchor);
                    Vector3 endPoint = pair.springJoint.connectedBody.transform.TransformPoint(pair.springJoint.connectedAnchor);

                    pair.lineRenderer.SetPosition(0, startPoint);
                    pair.lineRenderer.SetPosition(1, endPoint);

                    if (enableDebug)
                    {
                        Debug.DrawLine(startPoint, endPoint, Color.cyan);
                        Debug.DrawRay(startPoint, Vector3.up * 0.2f, Color.green, 0.1f);
                        Debug.DrawRay(endPoint, Vector3.up * 0.2f, Color.red, 0.1f);
                    }
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Line Renderer Error: {e.Message}");
            }
        }
    }

    void OnDrawGizmos()
    {
        if (enableDebug && Application.isPlaying)
        {
            Gizmos.color = Color.yellow;
            foreach (GrapplePair pair in activeGrapples)
            {
                if (pair.objectA != null && pair.objectB != null)
                {
                    Gizmos.DrawSphere(pair.objectA.position, 0.2f);
                    Gizmos.DrawSphere(pair.objectB.position, 0.2f);
                    Gizmos.DrawLine(pair.objectA.position, pair.objectB.position);
                }
            }
        }
    }

    private class GrapplePair
    {
        public Rigidbody objectA;
        public Rigidbody objectB;
        public SpringJoint springJoint;
        public LineRenderer lineRenderer;

        public GrapplePair(Rigidbody a, Rigidbody b, SpringJoint sj, LineRenderer lr)
        {
            objectA = a;
            objectB = b;
            springJoint = sj;
            lineRenderer = lr;
        }
    }
}
