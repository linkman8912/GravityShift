using UnityEngine;

public class ExplodableWall : MonoBehaviour
{

    private Rigidbody[] pieces;

    void Awake()
    {
        // Gather all child Rigidbodies up front
        pieces = GetComponentsInChildren<Rigidbody>();
        transform.DetachChildren();

    }

}
