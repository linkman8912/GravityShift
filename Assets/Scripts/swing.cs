using UnityEngine;
using System.Collections;

public class Swinging : MonoBehaviour
{
  [Header("Input")]
  [Tooltip("The button to use to start a swing")]
  public KeyCode swingKey = KeyCode.Mouse0;
  public KeyCode pullKey = KeyCode.Tab;

  [Header("References")]
  public LineRenderer lr;
  public Transform gunTip, cam, player;
  public LayerMask swingLayer;

  [Header("Swinging")]
  private float maxSwingDistance = 25f;
  private Vector3 swingPoint;
  private SpringJoint joint;
  private Vector3 currentGrapplePosition;

  void Update() {
    if (Input.GetKeyDown(swingKey)) startSwing();
    if (Input.GetKeyUp(swingKey)) stopSwing();
  }
  
  void LateUpdate() {
    drawRope();
  }
  
  void startSwing() {
    RaycastHit hit;
    if (Physics.Raycast(cam.position, cam.forward, out hit, maxSwingDistance, swingLayer)) {
      swingPoint = hit.point;
      joint = player.gameObject.AddComponent<SpringJoint>();
      joint.autoConfigureConnectedAnchor = false;
      joint.connectedAnchor = swingPoint;

      float distanceFromPoint = Vector3.Distance(player.position, swingPoint);

      // the distance grapple will try to keep from grapple point.
      joint.maxDistance = distanceFromPoint * 0.8f;
      joint.minDistance = distanceFromPoint * 0.25f;

      // customize values as you like
      joint.spring = 4.5f;
      joint.damper = 7f;
      joint.massScale = 4.5f;
      lr.positionCount = 2;
    }
  }
  
  void stopSwing() {
    lr.positionCount = 0;
    Destroy(joint);
  }

  void drawRope() {
    // if not grappling, don't draw rope
    if (!joint) return;
    if (Input.GetKey(pullKey)) currentGrapplePosition = Vector3.Lerp(currentGrapplePosition, swingPoint, Time.deltaTime * 8f);
    lr.SetPosition(0, gunTip.position);
    lr.SetPosition(1, swingPoint);
  }
}
