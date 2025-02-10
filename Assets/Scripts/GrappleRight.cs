using UnityEngine;
using System.Collections.Generic;
// Include the SurfCharacter namespace
using Fragsurf.Movement;

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
    public LayerMask centerGrappleLayer;

    [Header("References")]
    // Instead of a Camera reference, we use SurfCharacter
    public SurfCharacter surfCharacter;

    [Header("Debug Settings")]
    public bool enableDebug = true;
    public Color validTargetColor = Color.green;
    public Color invalidTargetColor = Color.red;

    private List<GrapplePair> activeGrapples = new List<GrapplePair>();
    private RaycastHit firstHit;
    private bool isFirstTargetSelected = false;

    private void Awake()
    {
        // Auto-assign the SurfCharacter if not set.
        if (surfCharacter == null)
        {
            surfCharacter = GetComponent<SurfCharacter>();
            if (surfCharacter == null)
            {
                Debug.LogError("SurfCharacter not found on the GameObject. Please assign a SurfCharacter reference.");
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
        if (Input.GetMouseButtonDown(1))
        {
            if (surfCharacter == null || surfCharacter.viewTransform == null)
            {
                Debug.LogError("SurfCharacter or its viewTransform is not assigned!");
                return;
            }

            // Use the SurfCharacter's viewTransform for the ray origin and direction.
            Vector3 rayOrigin = surfCharacter.viewTransform.position;
            Vector3 rayDirection = surfCharacter.viewTransform.forward;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, maxGrappleDistance, grappleLayer))
            {
                if (enableDebug)
                {
                    Debug.Log("Raycast hit: " + hit.collider.name);
                    Debug.DrawRay(rayOrigin, rayDirection * hit.distance, validTargetColor, 0.1f);
                }

                // Only allow grappling to dynamic (non-kinematic) rigidbodies.
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
                    Debug.DrawRay(rayOrigin, rayDirection * maxGrappleDistance, invalidTargetColor, 0.1f);

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
        // Add a SpringJoint to the first hit object.
        SpringJoint spring = startHit.rigidbody.gameObject.AddComponent<SpringJoint>();
        spring.connectedBody = endHit.rigidbody;
        spring.spring = springForce;
        spring.damper = damperForce;
        spring.autoConfigureConnectedAnchor = false;

        // Enable collisions between the connected bodies.
        spring.enableCollision = true;

        // Determine the local anchor point on the first object.
        Vector3 localHitPointA;
        if ((centerGrappleLayer.value & (1 << startHit.collider.gameObject.layer)) != 0)
        {
            localHitPointA = Vector3.zero;
            if (enableDebug)
                Debug.Log($"Using center for {startHit.collider.name}");
        }
        else
        {
            localHitPointA = startHit.transform.InverseTransformPoint(startHit.point);
        }
        spring.anchor = localHitPointA;

        // Determine the local anchor point on the second object.
        Vector3 localHitPointB;
        if ((centerGrappleLayer.value & (1 << endHit.collider.gameObject.layer)) != 0)
        {
            localHitPointB = Vector3.zero;
            if (enableDebug)
                Debug.Log($"Using center for {endHit.collider.name}");
        }
        else
        {
            localHitPointB = endHit.transform.InverseTransformPoint(endHit.point);
        }
        spring.connectedAnchor = localHitPointB;

        // Create and configure a LineRenderer to visualize the grapple.
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
        if (Input.GetKeyDown(KeyCode.R))
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

    // Helper class to store grapple connections.
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
