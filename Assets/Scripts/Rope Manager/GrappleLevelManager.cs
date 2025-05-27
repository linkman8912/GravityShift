using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[DisallowMultipleComponent]
public class GrappleLevelManager : MonoBehaviour
{
    [System.Serializable]
    public class GrapplePair
    {
        public GameObject objectA;
        public GameObject objectB;

        [Header("Spring Settings")]
        public float springForce = 100f;
        public float damper = 10f;

        [Header("Mass Scaling (for lifting)")]
        [Tooltip("Lower values make objectA feel lighter to the spring.")]
        [Range(0.01f, 10f)]
        public float hostMassScale = 1f;
        [Tooltip("Lower values make objectB feel lighter to the spring.")]
        [Range(0.01f, 10f)]
        public float anchorMassScale = 1f;

        [Header("Snap & Weaken Settings")]
        [Tooltip("Force (in N) at which the rope will begin to weaken.")]
        public float breakForce = Mathf.Infinity;
        [Tooltip("How many seconds the rope weakens before fully breaking.")]
        public float weakenDuration = 3f;
    }

    [Header("Level-Wide Settings")]
    [Tooltip("Which layers should treat attachments as 'center' instead of surface.")]
    public LayerMask centerGrappleLayer;
    [Tooltip("Material to use for all grapple lines.")]
    public Material lineMaterial;

    [Header("Preconfigured Pairs")]
    public List<GrapplePair> pairs = new List<GrapplePair>();

    // internal state for each active rope
    private class ActiveGrapple
    {
        public SpringJoint spring;
        public LineRenderer lr;
        public Rigidbody hostRb;
        public Rigidbody anchorRb;

        public float originalSpringForce;
        public float restDistance;
        public float thresholdDistance;
        public float weakenDuration;
        public bool isWeakening;
    }
    private readonly List<ActiveGrapple> _active = new List<ActiveGrapple>();

    void Start()
    {
        foreach (var p in pairs)
        {
            if (p.objectA == null || p.objectB == null)
                continue;

            var rbA = p.objectA.GetComponent<Rigidbody>();
            var rbB = p.objectB.GetComponent<Rigidbody>();
            var colA = p.objectA.GetComponent<Collider>();
            var colB = p.objectB.GetComponent<Collider>();
            if (rbA == null || rbB == null || colA == null || colB == null)
                continue;

            bool aIsK = rbA.isKinematic;
            bool bIsK = rbB.isKinematic;
            if (aIsK && bIsK)
                continue;

            Rigidbody hostRb = aIsK ? rbB : rbA;
            Rigidbody anchorRb = hostRb == rbA ? rbB : rbA;
            Collider hostCol = hostRb == rbA ? colA : colB;
            Collider anchorCol = hostRb == rbA ? colB : colA;

            // create and configure the SpringJoint
            var spring = hostRb.gameObject.AddComponent<SpringJoint>();
            spring.spring = p.springForce;
            spring.damper = p.damper;
            spring.autoConfigureConnectedAnchor = false;
            spring.enableCollision = true;

            // apply mass-scaling
            spring.massScale = p.hostMassScale;
            spring.connectedMassScale = p.anchorMassScale;

            // compute world attachment points
            bool hostCenter = (centerGrappleLayer.value & (1 << hostRb.gameObject.layer)) != 0;
            bool anchorCenter = (centerGrappleLayer.value & (1 << anchorRb.gameObject.layer)) != 0;

            Vector3 worldHostPoint = hostCenter
                ? hostRb.transform.position
                : hostCol.ClosestPoint(anchorRb.transform.position);

            Vector3 worldAnchorPoint = anchorCenter
                ? anchorRb.transform.position
                : anchorCol.ClosestPoint(hostRb.transform.position);

            spring.anchor = hostRb.transform.InverseTransformPoint(worldHostPoint);
            spring.connectedBody = anchorRb;
            spring.connectedAnchor = anchorRb.transform.InverseTransformPoint(worldAnchorPoint);

            // prepare ActiveGrapple
            var ag = new ActiveGrapple
            {
                spring = spring,
                lr = null, // set below
                hostRb = hostRb,
                anchorRb = anchorRb,
                originalSpringForce = p.springForce,
                restDistance = Vector3.Distance(worldHostPoint, worldAnchorPoint),
                thresholdDistance = Vector3.Distance(worldHostPoint, worldAnchorPoint)
                                       + (p.breakForce / p.springForce),
                weakenDuration = p.weakenDuration,
                isWeakening = false
            };

            // create the visual line
            var go = new GameObject($"GrappleLine_{hostRb.name}_{anchorRb.name}");
            var lr = go.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.material = lineMaterial;
            lr.startWidth = lr.endWidth = 0.1f;
            ag.lr = lr;

            _active.Add(ag);
        }
    }

    void Update()
    {
        for (int i = _active.Count - 1; i >= 0; i--)
        {
            var ag = _active[i];

            // if the spring has already been destroyed, clean up the line
            if (ag.spring == null)
            {
                if (ag.lr != null)
                    Destroy(ag.lr.gameObject);
                _active.RemoveAt(i);
                continue;
            }

            // check if it's time to start weakening
            if (!ag.isWeakening)
            {
                Vector3 start = ag.hostRb.transform.TransformPoint(ag.spring.anchor);
                Vector3 end = ag.anchorRb.transform.TransformPoint(ag.spring.connectedAnchor);
                float currentDist = Vector3.Distance(start, end);

                if (currentDist > ag.thresholdDistance)
                {
                    ag.isWeakening = true;
                    StartCoroutine(WeakenRope(ag));
                }
            }

            // update the visual line
            if (ag.lr != null)
            {
                Vector3 start = ag.hostRb.transform.TransformPoint(ag.spring.anchor);
                Vector3 end = ag.anchorRb.transform.TransformPoint(ag.spring.connectedAnchor);
                ag.lr.SetPosition(0, start);
                ag.lr.SetPosition(1, end);
            }
        }
    }

    private IEnumerator WeakenRope(ActiveGrapple ag)
    {
        int steps = Mathf.CeilToInt(ag.weakenDuration);
        float delta = ag.originalSpringForce / steps;

        for (int i = 1; i <= steps; i++)
        {
            yield return new WaitForSeconds(1f);
            if (ag.spring != null)
                ag.spring.spring = Mathf.Max(ag.originalSpringForce - delta * i, 0f);
        }

        // finally, destroy the joint to fully break
        if (ag.spring != null)
        {
            Destroy(ag.spring);
            ag.spring = null;
        }
    }
}
