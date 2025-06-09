// GrappleRight.cs
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
    private bool prevOrbHeld = false;
    private float orbReleaseTime = -Mathf.Infinity;
    private const float grappleBuffer = 0.1f;

    void Start()
    {
        _orbShooter = FindObjectOfType<GravityOrbShooter>();
        if (playerCamera == null)
            playerCamera = Camera.main;
        // removed error log
    }

    void Update()
    {
        if (_orbShooter != null)
        {
            bool currentlyHeld = _orbShooter.IsOrbHeld;
            if (prevOrbHeld && !currentlyHeld)
            {
                orbReleaseTime = Time.time;
            }
            prevOrbHeld = currentlyHeld;
        }
        HandleGrappleInput();
        HandleGrappleRelease();
        UpdateLineRenderers();
    }

    void HandleGrappleInput()
    {
        // 1) If we “consumed” a left-click for orb shooting, skip this frame
        if (GravityOrbShooter.leftClickConsumed)
        {
            GravityOrbShooter.leftClickConsumed = false;
            return;
        }

        if (_orbShooter != null && _orbShooter.IsOrbHeld)
        {
            return;
        }

        // 2.1) Enforce post-shot buffer
        if (Time.time - orbReleaseTime < grappleBuffer)
        {
            return;
        }

        // 3) Only proceed on right-mouse down
        if (!Input.GetMouseButtonDown(1))
            return;

        // 4) Raycast for valid grapple targets
        int detectionMask = grappleLayer.value | anchorLayer.value;
        if (!Physics.Raycast(playerCamera.transform.position,
                             playerCamera.transform.forward,
                             out RaycastHit hit,
                             maxGrappleDistance,
                             detectionMask))
        {
            if (enableDebug)
            {
                Debug.DrawRay(playerCamera.transform.position,
                              playerCamera.transform.forward * maxGrappleDistance,
                              invalidTargetColor,
                              0.1f);
                if (isFirstTargetSelected)
                    ; // cleared first target
                isFirstTargetSelected = false;
            }
            return;
        }

        // 5) Require a Rigidbody on the hit
        if (hit.rigidbody == null)
        {
            return;
        }

        // 6) Check dynamic vs. anchor layers
        bool isDynamic = !hit.rigidbody.isKinematic;
        bool isAnchor = (anchorLayer.value & (1 << hit.collider.gameObject.layer)) != 0;
        if (!isDynamic && !isAnchor)
        {
            return;
        }

        // 7) First click selects host, second click creates the connection
        if (!isFirstTargetSelected)
        {
            firstHit = hit;
            isFirstTargetSelected = true;
        }
        else
        {
            CreateGrappleConnection(firstHit, hit);
            isFirstTargetSelected = false;
        }
    }

    void CreateGrappleConnection(RaycastHit hitA, RaycastHit hitB)
    {
        bool aIsAnchor = (anchorLayer.value & (1 << hitA.collider.gameObject.layer)) != 0;
        bool bIsAnchor = (anchorLayer.value & (1 << hitB.collider.gameObject.layer)) != 0;

        if (aIsAnchor && bIsAnchor)
        {
            CreateVisualConnection(hitA, hitB);
            return;
        }

        RaycastHit hostHit, anchorHit;
        if (aIsAnchor ^ bIsAnchor)
        {
            anchorHit = aIsAnchor ? hitA : hitB;
            hostHit = aIsAnchor ? hitB : hitA;
        }
        else
        {
            hostHit = hitA;
            anchorHit = hitB;
        }

        var hostRb = hostHit.rigidbody;
        var anchorRb = anchorHit.rigidbody;

        var spring = hostRb.gameObject.AddComponent<SpringJoint>();
        spring.spring = springForce;
        spring.damper = damperForce;
        spring.autoConfigureConnectedAnchor = false;
        spring.enableCollision = true;

        spring.anchor = ((centerGrappleLayer.value & (1 << hostHit.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : hostHit.transform.InverseTransformPoint(hostHit.point);

        spring.connectedBody = anchorRb;
        spring.connectedAnchor = ((centerGrappleLayer.value & (1 << anchorHit.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : anchorHit.transform.InverseTransformPoint(anchorHit.point);

        var lr = new GameObject("GrappleLine").AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = lineMaterial;
        lr.startWidth = lr.endWidth = 0.1f;

        activeGrapples.Add(new GrapplePair(hostRb, anchorRb, spring, lr));

        var hostEnemy = hostRb.GetComponent<EnemyShooter>();
        var anchorEnemy = anchorRb != null ? anchorRb.GetComponent<EnemyShooter>() : null;
        if (hostEnemy != null) hostEnemy.IsGrappled = true;
        if (anchorEnemy != null) anchorEnemy.IsGrappled = true;

        if (enableDebug)
        {
            Debug.DrawLine(hostRb.transform.TransformPoint(spring.anchor),
                           spring.connectedBody.transform.TransformPoint(spring.connectedAnchor),
                           Color.cyan);
        }
    }

    void CreateVisualConnection(RaycastHit hitA, RaycastHit hitB)
    {
        Vector3 localA = ((centerGrappleLayer.value & (1 << hitA.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : hitA.transform.InverseTransformPoint(hitA.point);
        Vector3 localB = ((centerGrappleLayer.value & (1 << hitB.collider.gameObject.layer)) != 0)
            ? Vector3.zero
            : hitB.transform.InverseTransformPoint(hitB.point);

        var lr = new GameObject("GrappleLine_Visual").AddComponent<LineRenderer>();
        lr.positionCount = 2;
        lr.material = lineMaterial;
        lr.startWidth = lr.endWidth = 0.1f;

        activeGrapples.Add(new GrapplePair(hitA.rigidbody, hitB.rigidbody, null, lr, localA, localB));
    }

    void HandleGrappleRelease()
    {
        if (!Input.GetKeyDown(releaseKey))
            return;

        foreach (var pair in activeGrapples)
        {
            if (pair.springJoint != null) Destroy(pair.springJoint);
            if (pair.lineRenderer != null) Destroy(pair.lineRenderer.gameObject);

            var enemyA = pair.objectA?.GetComponent<EnemyShooter>();
            var enemyB = pair.objectB?.GetComponent<EnemyShooter>();
            if (enemyA != null) enemyA.IsGrappled = false;
            if (enemyB != null) enemyB.IsGrappled = false;
        }

        activeGrapples.Clear();
        isFirstTargetSelected = false;
    }

    void UpdateLineRenderers()
    {
        foreach (var pair in activeGrapples)
        {
            if (pair.lineRenderer == null) continue;

            Vector3 pA, pB;
            if (pair.springJoint != null)
            {
                pA = pair.objectA.transform.TransformPoint(pair.springJoint.anchor);
                pB = pair.springJoint.connectedBody.transform.TransformPoint(pair.springJoint.connectedAnchor);
            }
            else
            {
                pA = pair.objectA.transform.TransformPoint(pair.anchorA);
                pB = pair.objectB.transform.TransformPoint(pair.anchorB);
            }

            pair.lineRenderer.SetPosition(0, pA);
            pair.lineRenderer.SetPosition(1, pB);

            if (enableDebug)
                Debug.DrawLine(pA, pB, Color.cyan);
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
        public Vector3 anchorA, anchorB;

        public GrapplePair(Rigidbody a, Rigidbody b, SpringJoint sj, LineRenderer lr)
        {
            objectA = a;
            objectB = b;
            springJoint = sj;
            lineRenderer = lr;
            if (sj != null)
            {
                anchorA = sj.anchor;
                anchorB = sj.connectedAnchor;
            }
        }

        public GrapplePair(Rigidbody a, Rigidbody b, SpringJoint sj, LineRenderer lr, Vector3 aA, Vector3 aB)
        {
            objectA = a;
            objectB = b;
            springJoint = sj;
            lineRenderer = lr;
            anchorA = aA;
            anchorB = aB;
        }
    }
}
