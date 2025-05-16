using UnityEngine;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class GrappleLevelManager : MonoBehaviour
{
    [System.Serializable]
    public class GrapplePair
    {
        public GameObject objectA;
        public GameObject objectB;
        public float springForce = 100f;
        public float damper = 10f;
    }

    [Header("Level-Wide Settings")]
    [Tooltip("Which layers should treat hits as 'center' attachments.")]
    public LayerMask centerGrappleLayer;
    [Tooltip("Material to use for all grapple lines.")]
    public Material lineMaterial;

    [Header("Preconfigured Pairs")]
    public List<GrapplePair> pairs = new List<GrapplePair>();

    // internal list to keep track of runtime joints & lines
    private class ActiveGrapple
    {
        public SpringJoint spring;
        public LineRenderer lr;
        public Rigidbody hostRb;
        public Rigidbody anchorRb;
    }
    private readonly List<ActiveGrapple> _active = new List<ActiveGrapple>();

    void Start()
    {
        // For each designer-configured pair, create exactly the same SpringJoint + LineRenderer
        foreach (var p in pairs)
        {
            if (p.objectA == null || p.objectB == null) continue;
            var rbA = p.objectA.GetComponent<Rigidbody>();
            var rbB = p.objectB.GetComponent<Rigidbody>();
            if (rbA == null || rbB == null) continue;

            // Decide "host" vs "anchor": if one is kinematic, it's the anchor
            bool aIsKinematic = rbA.isKinematic;
            bool bIsKinematic = rbB.isKinematic;

            // Don’t connect two kinematic objects
            if (aIsKinematic && bIsKinematic) continue;

            Rigidbody hostRb = aIsKinematic ? rbB : rbA;
            Rigidbody anchorRb = hostRb == rbA ? rbB : rbA;

            // spring joint
            var spring = hostRb.gameObject.AddComponent<SpringJoint>();
            spring.spring = p.springForce;
            spring.damper = p.damper;
            spring.autoConfigureConnectedAnchor = false;
            spring.connectedBody = anchorRb;
            spring.enableCollision = true;

            // anchor points: center vs hit-point
            Vector3 posA = hostRb.transform.position;
            Vector3 posB = anchorRb.transform.position;

            spring.anchor =
                (centerGrappleLayer == (centerGrappleLayer | (1 << hostRb.gameObject.layer)))
                    ? Vector3.zero
                    : hostRb.transform.InverseTransformPoint(posA);

            spring.connectedAnchor =
                (centerGrappleLayer == (centerGrappleLayer | (1 << anchorRb.gameObject.layer)))
                    ? Vector3.zero
                    : anchorRb.transform.InverseTransformPoint(posB);

            // visual line
            var go = new GameObject($"GrappleLine_{hostRb.name}_{anchorRb.name}");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.material = lineMaterial;
            lr.startWidth = lr.endWidth = 0.1f;

            _active.Add(new ActiveGrapple
            {
                spring = spring,
                lr = lr,
                hostRb = hostRb,
                anchorRb = anchorRb
            });
        }
    }

    void Update()
    {
        // Update line positions each frame
        foreach (var ag in _active)
        {
            if (ag.lr == null || ag.spring == null) continue;
            var start = ag.hostRb.transform.TransformPoint(ag.spring.anchor);
            var end = ag.anchorRb.transform.TransformPoint(ag.spring.connectedAnchor);
            ag.lr.SetPosition(0, start);
            ag.lr.SetPosition(1, end);
        }
    }
}
