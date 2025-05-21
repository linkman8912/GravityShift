using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Grappling : MonoBehaviour {
  [Header("References")]
  private PlayerMovementGrappling pm;
  public Transform cam;
  public Transform gunTip;
  public LayerMask whatIsGrappleable;

  [Header("Grappling")]
  public float maxGrappleDistance;
  public float grappleDelayTime;

  private Vector3 grapplePoint;


  [Header("Cooldown")]
  public float grapplingCd;
  private float grapplingCdTimer;

  [Header("Input")]
  public KeyCode grappleKey = KeyCode.Mouse1;
  private bool grappling;


  void Start() {
    pm = GetComponent<PlayerMovementGrappling>();
  }

  void Update() {
    if (Input.GetKeyDown(grappleKey)) StartGrapple();
    if (grapplingCdTimer > 0) grapplingCdTimer -= Time.deltaTime;
  }

  public void StartGrapple() {
    if (grapplingCdTimer > 0) return;
    grappling = true;
    RaycastHit hit;
    if (Physics.Raycast(cam.position, cam.forward, out hit, maxGrappleDistance, whatIsGrappleable)) {
      grapplePoint = hit.point;

      Invoke(nameof(ExecuteGrapple), grappleDelayTime);
    }
    else {
      grapplePoint = cam.position + cam.forward * maxGrappleDistance;
      Invoke(nameof(StopGrapple), grappleDelayTime);
    }
  }

  public void ExecuteGrapple() {

  }

  public void StopGrapple() {
    grappling = false;
    grapplingCdTimer = grapplingCd;
  }
  public Vector3 GetGrapplePoint() {
    return grapplePoint;
  }
  public bool IsGrappling() {
    return grappling;
  }
}
