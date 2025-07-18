﻿// Grappling.cs
using UnityEngine;
using System.Collections;

public class OldGrappling : MonoBehaviour
{
    // ——— Existing fields ———
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable;
    public KeyCode grappleKey = KeyCode.Mouse0;    // ← left-click
    public KeyCode pullKey = KeyCode.Tab;
    public float pullSpeed = 50f;
    [SerializeField] const float pullBudget = 3f; // The amount of time you have to use the pull key, this gets refilled over time after a short delay.
    float pullBudgetTime = pullBudget; // The modifiable version
    [SerializeField] const float pullRefillRate = 0.5f; // The rate that the pullBudget gets refilled when it's being recharged, in seconds of budget per second of recharge.
    [SerializeField] const float pullRefillDelay = 0.5f; // The delay before pullbudget gets refilled.
    float pullRefillTimer = pullRefillDelay; // The modifiable version
    public Transform gunTip, camera, player;
    public float maxDistance = 100000f;
    private SpringJoint joint;

    // ——— Orb shooter reference ———
    [Tooltip("Drag your GravityOrbShooter here, or it'll auto-find at Start")]
    public GravityOrbShooter orb;

    // ——— Buffer logic fields ———
    private bool prevOrbHeld = false;
    private float orbReleaseTime = -Mathf.Infinity;
    private const float grappleBuffer = 0.1f;  // seconds after release

    void Start()
    {
        lr = GetComponent<LineRenderer>();

        // Make sure orb reference is set
        if (orb == null)
        {
            orb = FindObjectOfType<GravityOrbShooter>();
        }
    }

    void Update()
    {
        // 1) Track orb hold→release
        if (orb != null)
        {
            bool currentlyHeld = orb.IsOrbHeld;
            if (prevOrbHeld && !currentlyHeld)
            {
                orbReleaseTime = Time.time;
            }
            prevOrbHeld = currentlyHeld;
        }

        // 2) Grapple on left-click
        if (Input.GetKeyDown(grappleKey))
        {
            // 2a) blocked while orb held
            if (orb != null && orb.IsOrbHeld)
            {
                // blocked
            }
            // 2b) blocked during buffer
            else if (Time.time - orbReleaseTime < grappleBuffer)
            {
                // blocked
            }
            // 2c) ok to grapple
            else
            {
                StartGrapple();
            }
        }
        else if (Input.GetKeyUp(grappleKey))
        {
            StopGrapple();
        }

        // 3) Pull logic
        if (isGrappling() && Input.GetKey(pullKey) && pullBudget > 0)
        {
            joint.maxDistance -= pullSpeed * Time.deltaTime;
            //joint.minDistance -= pullSpeed * Time.deltaTime;
            //pullBudgetLeft -= Time.deltaTime;
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    void StartGrapple()
    {
        RaycastHit hit;
        if (Physics.Raycast(camera.position, camera.forward, out hit, maxDistance, whatIsGrappleable))
        {
            if (hit.collider.transform.root == player.transform)
            {
                return;
            }

            grapplePoint = hit.point;
            joint = player.gameObject.AddComponent<SpringJoint>();
            joint.autoConfigureConnectedAnchor = false;
            joint.connectedAnchor = grapplePoint;
            float dist = Vector3.Distance(player.position, grapplePoint);
            joint.maxDistance = dist * 0.8f;
            //joint.minDistance = dist * 0.25f;
            joint.minDistance = 0;
            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;
            //joint.tolerance = 10;
            lr.positionCount = 2;
        }
        else
        {
            // no hit
        }
    }

    void StopGrapple()
    {
        lr.positionCount = 0;
        Destroy(joint);
    }

    void DrawRope()
    {
        if (!joint) return;
        lr.SetPosition(0, gunTip.position);
        lr.SetPosition(1, grapplePoint);
    }

    public bool isGrappling()
    {
        return joint != null;
    }

    public Vector3 getGrapplePoint()
    {
        return grapplePoint;
    }
}
