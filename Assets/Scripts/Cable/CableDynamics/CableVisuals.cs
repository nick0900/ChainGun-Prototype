using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class CableVisuals : MonoBehaviour
{
    private LineRenderer lineRenderer;
    private CableRoot cableRoot = null;
    private void OnEnable()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        if (cableRoot == null) cableRoot = gameObject.GetComponent<CableRoot>();
        if (cableRoot == null) this.enabled = false;
    }

    private void OnDisable()
    {
        Destroy(lineRenderer);
    }

    private void FixedUpdate()
    {
        lineRenderer.startWidth = cableRoot.CableHalfWidth * 2;
        lineRenderer.endWidth = cableRoot.CableHalfWidth * 2;
        lineRenderer.positionCount = 0;
        foreach (CableRoot.Joint joint in cableRoot.Joints)
        {
            lineRenderer.positionCount += 2;

            lineRenderer.SetPosition(lineRenderer.positionCount - 2, joint.tangentPointTail);
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, joint.tangentPointHead);
        }
    }
}
