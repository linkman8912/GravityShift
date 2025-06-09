using UnityEngine;
using Cinemachine;

public class TestImpulse : MonoBehaviour
{
    public CinemachineImpulseSource source;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (source == null)
            {
                Debug.LogWarning("No ImpulseSource assigned!", this);
                return;
            }
            Debug.Log("🔔 Generating Test Impulse", this);
            source.GenerateImpulse();
        }
    }
}
