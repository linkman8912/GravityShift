using System.Collections;
using UnityEngine;

public class Grappling : MonoBehaviour {
  private LineRenderer lr;
  private Vector3 grapplePoint;
  public LayerMask whatIsGrappleable;
  public KeyCode grappleKey = KeyCode.Mouse1;
  public Transform gunTip, camera, player;
  public float maxDistance = 100000f;
  private SpringJoint joint;

  void Start() {
    lr = GetComponent<LineRenderer>();
  }
  void Update() {
    if(Input.GetKeyDown(grappleKey)) {
      StartGrapple();
    }
    else if(Input.GetKeyUp(grappleKey)) {
      StopGrapple();
    } 
  }

  void LateUpdate() {
    DrawRope();
  }

  void StartGrapple() {
    Debug.Log("start grapple");
    RaycastHit hit;
    if (Physics.Raycast(origin: camera.position, direction: camera.forward, out hit, maxDistance)) {
      grapplePoint = hit.point;
      joint = player.gameObject.AddComponent<SpringJoint>();
      joint.autoConfigureConnectedAnchor = false;
      joint.connectedAnchor = grapplePoint;
      float distanceFromPoint = Vector3.Distance(player.position, grapplePoint);
      // modifiable based on game feel
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
