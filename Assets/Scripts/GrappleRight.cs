using UnityEngine;
using System.Collections.Generic;

public class GrappleRight : MonoBehaviour
{
    [Header("Grapple Settings")]
    public float maxGrappleDistance = 50f;
    public float springForce = 100f;
    public float damperForce = 10f;
    public LayerMask grappleLayer;
    public LayerMask anchorLayer;
    public LayerMask centerGrappleLayer;
    public Material lineMaterial;

    [Header("References")]
    public Camera playerCamera;
    public KeyCode releaseKey = KeyCode.R;

    [Header("Debug")]
    public bool enableDebug = true;
    public Color validTargetColor = Color.green;
    public Color invalidTargetColor = Color.red;

    private List<GrapplePair> activeGrapples = new List<GrapplePair>();
    private RaycastHit firstHit;
    private bool isFirstTargetSelected = false;
    private GravityOrbShooter _orbShooter;

    void Start()
    {
        _orbShooter = FindObjectOfType<GravityOrbShooter>();
        if (playerCamera == null)
            playerCamera = Camera.main;
        if (playerCamera == null)
            Debug.LogError("GrappleRight: No Camera assigned or found.");
    }

    void Update()
    {
        HandleGrappleInput();
        HandleGrappleRelease();
        UpdateLineRenderers();
    }

    void HandleGrappleInput()
    {
        // block input if orb is held
        if ((_orbShooter != null && _orbShooter.IsOrbHeld) ||
            GravityOrbShooter.leftClickConsumed)
            return;

        if (!Input.GetMouseButtonDown(1))
            return;

        // COMBINE both masks so anchors become hittable
        int detectionMask = grappleLayer.value | anchorLayer.value;
        if (!Physics.Raycast(
                playerCamera.transform.position,
                playerCamera.transform.forward,
                out RaycastHit hit,
                maxGrappleDistance,
                detectionMask))
        {
            if (enableDebug)
            {
                Debug.DrawRay(
                    playerCamera.transform.position,
                    playerCamera.transform.forward * maxGrappleDistance,
                    invalidTargetColor, 0.1f);

                if (isFirstTargetSelected)
                    Debug.Log("Clearing first target — no valid second target");
                else
                    Debug.Log("No valid grapple target detected");

                isFirstTargetSelected = false;
            }
            return;
        }

        if (hit.rigidbody == null)
        {
            if (enableDebug)
                Debug.LogWarning($"{hit.collider.name}: No Rigidbody");
            return;
        }

        bool isDynamic = !hit.rigidbody.isKinematic;
        bool isAnchor = (anchorLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        if (!isDynamic && !isAnchor)
        {
            if (enableDebug)
                Debug.LogWarning($"{hit.collider.name}: Kinematic and not in Anchor layer");
            return;
        }

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

    void CreateGrappleConnection(RaycastHit hitA, RaycastHit hitB)
    {
        bool aIsAnchor = (anchorLayer.value & (1 << hitA.collider.gameObject.layer)) != 0;
        bool bIsAnchor = (anchorLayer.value & (1 << hitB.collider.gameObject.layer)) != 0;

        // DON’T hook two anchors together
        if (aIsAnchor && bIsAnchor)
        {
            Debug.LogWarning("Both targets are anchors! At least one must be dynamic.");
            return;
        }

        // determine which is host (dynamic) vs anchor (if any)
        RaycastHit hostHit, anchorHit;
        if (aIsAnchor ^ bIsAnchor)
        {
            if (aIsAnchor)
            {
                anchorHit = hitA; hostHit = hitB;
            }
            else
            {
                anchorHit = hitB; hostHit = hitA;
            }
        }
        else
        {
            // both dynamic or both non-anchor: treat hitA as host
            hostHit = hitA;
            anchorHit = hitB;
        }

        Rigidbody hostRb = hostHit.rigidbody;
        Rigidbody anchorRb = anchorHit.rigidbody;

        // add SpringJoint on host only
        SpringJoint spring = hostRb.gameObject.AddComponent<SpringJoint>();
        spring.spring = springForce;
        spring.damper = damperForce;
        spring.autoConfigureConnectedAnchor = false;
        spring.enableCollision = true;

        // set host anchor point
        Vector3 hostAnchor = ((centerGrappleLayer.value & (1 << hostHit.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : hostHit.transform.InverseTransformPoint(hostHit.point);
        spring.anchor = hostAnchor;

        // connect to anchorRb
        spring.connectedBody = anchorRb;
        Vector3 connAnchor = ((centerGrappleLayer.value & (1 << anchorHit.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : anchorHit.transform.InverseTransformPoint(anchorHit.point);
        spring.connectedAnchor = connAnchor;

        // draw line
        LineRenderer lr = new GameObject("GrappleLine").AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = lineMaterial;
        lr.startWidth = lr.endWidth = 0.1f;

        activeGrapples.Add(new GrapplePair(hostRb, anchorRb, spring, lr));

        if (enableDebug)
        {
            string hDesc = hostHit.collider.name + (hostRb.isKinematic ? " (AnchorLayer)" : "");
            string aDesc = anchorHit.collider.name + (anchorRb.isKinematic ? " (AnchorLayer)" : "");
            Debug.Log($"New Grapple! {hDesc} ↔ {aDesc}\n" +
                      $"Anchors → Host: {spring.anchor}, Connected: {spring.connectedAnchor}");
        }
    }

    void HandleGrappleRelease()
    {
        if (!Input.GetKeyDown(releaseKey)) return;

        if (enableDebug)
            Debug.Log($"Releasing all ({activeGrapples.Count}) grapples");

        foreach (var pair in activeGrapples)
        {
            if (pair.springJoint != null) Destroy(pair.springJoint);
            if (pair.lineRenderer != null) Destroy(pair.lineRenderer.gameObject);
        }

        activeGrapples.Clear();
        isFirstTargetSelected = false;
    }

    void UpdateLineRenderers()
    {
        foreach (var pair in activeGrapples)
        {
            if (pair.lineRenderer == null) continue;

            Vector3 startPoint = pair.objectA.transform.TransformPoint(pair.springJoint.anchor);
            Vector3 endPoint = pair.springJoint.connectedBody.transform.TransformPoint(pair.springJoint.connectedAnchor);

            pair.lineRenderer.SetPosition(0, startPoint);
            pair.lineRenderer.SetPosition(1, endPoint);

            if (enableDebug)
                Debug.DrawLine(startPoint, endPoint, Color.cyan);
        }
    }

    void OnDrawGizmos()
    {
        if (!enableDebug || !Application.isPlaying) return;
        Gizmos.color = Color.yellow;
        foreach (var pair in activeGrapples)
        {
            if (pair.objectA == null || pair.objectB == null) continue;
            Gizmos.DrawSphere(pair.objectA.position, 0.2f);
            Gizmos.DrawSphere(pair.objectB.position, 0.2f);
            Gizmos.DrawLine(pair.objectA.position, pair.objectB.position);
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
