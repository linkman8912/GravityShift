﻿// Grappling.cs
using UnityEngine;
using System.Collections;

public class Grappling : MonoBehaviour
{
    // ——— Existing fields ———
    private LineRenderer lr;
    private Vector3 grapplePoint;
    public LayerMask whatIsGrappleable;
    public KeyCode grappleKey = KeyCode.Mouse0;    // ← left-click
    public KeyCode pullKey = KeyCode.Tab;
    public float pullSpeed = 30f;
    public float pullBudget = 2f; // The amount of time you have to use the pull key, this gets refilled over time after a short delay.
    public float pullBudgetTime; // The modifiable version - made public so UIHolder can access it
    [SerializeField] const float pullRefillRate = 2f; // The rate that the pullBudget gets refilled when it's being recharged, in seconds of budget per second of recharge.
    [SerializeField] const float pullRefillDelay = 0.5f; // The delay before pullbudget gets refilled.
    float pullRefillTimer = 0; // The modifiable version
    public Transform gunTip, camera, player;
    public float maxDistance = 50f;
    private SpringJoint joint;
    private PlayerMovement pm;
    public Transform gunTipBase;
    [SerializeField] private UIHolder uiHolder;

    // ——— Orb shooter reference ———
    [Tooltip("Drag your GravityOrbShooter here, or it'll auto-find at Start")]
    public GravityOrbShooter orb;

    // ——— Buffer logic fields ———
    private bool prevOrbHeld = false;
    private float orbReleaseTime = -Mathf.Infinity;
    private const float grappleBuffer = 0.1f;  // seconds after release

    // ——— Visual Settings ———
    [Header("Visual Settings")]
    [SerializeField] private bool use3DVisualizer = true; // Toggle between 2D LineRenderer and 3D visualizer

    void Start()
    {
        pullBudgetTime = pullBudget;
        lr = GetComponent<LineRenderer>();
        pm = player.GetComponent<PlayerMovement>();

        // Disable LineRenderer if using 3D visualizer
        if (use3DVisualizer && lr != null)
        {
            lr.enabled = false;
            lr.positionCount = 0; // Also clear any positions
        }

        // Make sure orb reference is set
        if (orb == null)
        {
            orb = FindObjectOfType<GravityOrbShooter>();
        }

        // Auto-find UIHolder if not assigned
        if (uiHolder == null)
        {
            uiHolder = FindObjectOfType<UIHolder>();
        }
    }

    void Update()
    {
        //Debug.Log($"pull budget = {pullBudgetTime}s");
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
        if (isGrappling() && Input.GetKey(pullKey) && pullBudgetTime > 0)
        {
            joint.maxDistance -= pullSpeed * Time.deltaTime;
            //joint.minDistance -= pullSpeed * Time.deltaTime;
            pullBudgetTime -= Time.deltaTime;
            pullRefillTimer = pullRefillDelay;
            if (pullBudgetTime < 0)
                pullBudgetTime = 0;
        }
        // 3) timer refill logic
        if (pullRefillTimer > 0)
            pullRefillTimer -= Time.deltaTime;
        // pull refill logic
        if (pullBudgetTime < pullBudget && pullRefillTimer <= 0 && (pm.grounded || pm.wallrunning))
        {
            pullRefillTimer = 0;
            RefillPull();
        }

        // Update UI with current pull budget
        if (uiHolder != null)
        {
            uiHolder.SetPull(pullBudgetTime);
        }
    }

    void LateUpdate()
    {
        DrawRope();
    }

    public void StartGrapple()
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
            joint.minDistance = 0;
            joint.spring = 4.5f;
            joint.damper = 7f;
            joint.massScale = 4.5f;

            // Only set LineRenderer position count if not using 3D visualizer
            if (!use3DVisualizer && lr != null)
            {
                lr.positionCount = 2;
                lr.enabled = true; // Make sure it's enabled when we want to use it
            }

            pm.secondJump = true;
            pm.grappling = true;
        }
        else
        {
            // no hit
        }
    }

    public void StopGrapple()
    {
        // Only set LineRenderer position count if not using 3D visualizer
        if (!use3DVisualizer && lr != null)
        {
            lr.positionCount = 0;
        }

        Destroy(joint);
        pm.grappling = false;
    }

    void DrawRope()
    {
        // Skip LineRenderer drawing if using 3D visualizer OR if LineRenderer is disabled
        if (use3DVisualizer || lr == null || !lr.enabled) return;

        if (!joint || gunTipBase == null) return;

        lr.SetPosition(0, gunTipBase.position);
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

    void RefillPull()
    {
        if (pullBudgetTime < pullBudget)
            pullBudgetTime += Time.deltaTime * pullRefillRate;
        if (pullBudgetTime > pullBudget)
            pullBudgetTime = pullBudget;
    }
}