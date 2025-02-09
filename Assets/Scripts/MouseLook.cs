using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MouseLook : MonoBehaviour
{
  public enum RotationAxes {
	MouseXAndY = 0,
	MouseX = 1,
	MouseY = 2
  }
	
  public RotationAxes axes = RotationAxes.MouseXAndY;
  public float sensitivityHorizontal = 3.0f;
  public float sensitivityVertical = 3.0f;
  public float minimumVertical = -90.0f;
  public float maximumVertical = 90.0f;
  private float verticalRotation = 0;

  // Start is called before the first frame update
  void Start() {
	Rigidbody body = GetComponent<Rigidbody>();
	if(body !=null) {
	  body.freezeRotation = true;
	}
  }

  // Update is called once per frame
  void Update() {
	if(axes == RotationAxes.MouseX) {
	  transform.Rotate(0, Input.GetAxis("Mouse X") * sensitivityHorizontal, 0);
	}
		
	else if(axes == RotationAxes.MouseY) {
	  verticalRotation -= Input.GetAxis("Mouse Y") * sensitivityVertical;
	  verticalRotation = Mathf.Clamp(verticalRotation, minimumVertical, maximumVertical);
	  float horizontalRot = transform.localEulerAngles.y;
	  transform.localEulerAngles = new Vector3(verticalRotation, horizontalRot, 0);
	}

	else {
	  verticalRotation -= Input.GetAxis("Mouse Y") * sensitivityVertical;
	  verticalRotation = Mathf.Clamp(verticalRotation, minimumVertical, maximumVertical);
	  float delta=Input.GetAxis("Mouse X") * sensitivityHorizontal;
	  float horizontalRot = transform.localEulerAngles.y + delta;
	  transform.localEulerAngles=new Vector3(verticalRotation, horizontalRot, 0);
	}
  }
}
