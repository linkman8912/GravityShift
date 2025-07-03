using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using CameraShake;

public class ScreenShakeTrigger : MonoBehaviour
{


    // Parameters of the shake to tweak in the inspector.
    public PerlinShake.Params shakeParams;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {

    }

    // This is called by animator.
    public void Shake()
    {
        Vector3 sourcePosition = transform.position;

        CameraShaker.Shake(new PerlinShake(shakeParams));
    }
}
