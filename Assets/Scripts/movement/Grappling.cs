using System.Collections;
using UnityEngine;

public class Grappling : MonoBehaviour {
  private LineRenderer lr;
  private Vector3 grapplePoint;
  public LayerMask whatIsGrappleable;
  public KeyCode grappleKey = KeyCode.Mouse1;
  public KeyCode pullKey = KeyCode.Tab;
  public float pullSpeed = 50f;
  public Transform gunTip, camera, player;
  public float maxDistance = 100000f;
  private SpringJoint joint;
  private GravityOrbShooter orb;

  void Start() {
    lr = GetComponent<LineRenderer>();
    orb = GetComponent<GravityOrbShooter>();
  }
  void Update() {
    if(Input.GetKeyDown(grappleKey) /*&& !orb.IsOrbHeld*/) {
      StartGrapple();
    }
    else if(Input.GetKeyUp(grappleKey)) {
      StopGrapple();
    } 
    if (isGrappling() && Input.GetKey(pullKey)) {
      joint.maxDistance -= pullSpeed * Time.deltaTime;
      joint.minDistance -= pullSpeed * Time.deltaTime;
    }
  }

  void LateUpdate() {
    DrawRope();
  }

  void StartGrapple()
  {
    RaycastHit hit;
    // 1) pass your LayerMask into the Raycast call:
    if (Physics.Raycast(
          origin: camera.position,
          direction: camera.forward,
          out hit,
          maxDistance,
          whatIsGrappleable  // <-- mask excludes your Player layer!
          ))
    {
      // 2) sanity-check that you didn't still hit your own player transform:
      if (hit.collider.transform.root == player.transform)
      {
        Debug.Log("Grapple hit player – ignoring.");
        return;
      }

      // ––––– existing grapple hookup code –––––
      grapplePoint = hit.point;
      joint = player.gameObject.AddComponent<SpringJoint>();
      joint.autoConfigureConnectedAnchor = false;
      joint.connectedAnchor = grapplePoint;
      float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);
      joint.maxDistance = distanceFromPoint * 0.8f;
      joint.minDistance = distanceFromPoint * 0.25f;
      joint.spring = 4.5f;
      joint.damper = 7f;
      joint.massScale = 4.5f;
      lr.positionCount = 2;
    }
  }


  void StopGrapple() {
    lr.positionCount = 0;
    Destroy(joint);
  }

  void DrawRope() {
    // don't draw if not grappling
    if (!joint) return;
    lr.SetPosition(0, gunTip.position);
    lr.SetPosition(1, grapplePoint);

  }
  public bool isGrappling() {
    return joint != null;
  }
  public Vector3 getGrapplePoint() {
    return grapplePoint;
  }
}
