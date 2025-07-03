using UnityEngine;

// Don't forget to add this.
using CameraShake;

public class ShakeTrigger : MonoBehaviour
{
    // Parameters of the shake to tweak in the inspector.
    public PerlinShake.Params shakeParams;

    // This is called by animator.
    public void Stomp()
    {
        Vector3 sourcePosition = transform.position;

        CameraShaker.Shake(new PerlinShake(shakeParams));
    }
}
