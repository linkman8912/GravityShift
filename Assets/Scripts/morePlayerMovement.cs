using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/* A script to handle wallrunning, walljumping, ground slam and other forms of additional movement
 * basic pseudocode for wallrun
 * if player is near wall:
 *  slow down falling
 *  move camera or something
 */

public class extraMovement : MonoBehaviour {
  private Rigidbody rb;
  private bool onWall;

  public float walljumpForceX;
  public float walljumpForceY;
  public float wallrunTime;
  
  void Start() {
    rb = GetComponent<Rigidbody>();
    Debug.Log("Start");
  }

  void OnCollisionStay(Collision collision) {
    Debug.Log("Collided");
    if (collision.gameObject.tag == "Wall") {
      onWall = true;
    }
  }

  void Update() {
 /*   if (onWall) {
      Debug.Log("On a wall");
    }
    else {
      Debug.Log("Not on a wall");
    }
    Debug.Log("update"); */
  }
}
