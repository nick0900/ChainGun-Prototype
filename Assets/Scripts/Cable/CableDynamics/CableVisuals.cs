using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using static CableRoot;

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
        if (cableRoot.Looping)
        {
            foreach (CableRoot.Joint joint in cableRoot.Joints)
            {
                lineRenderer.positionCount += 2;

                lineRenderer.SetPosition(lineRenderer.positionCount - 2, joint.tangentPointTail);
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, joint.tangentPointHead);
            }
        }
        else
        {
            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, cableRoot.Joints[0].tangentPointHead);
            for (int i = 1; i < cableRoot.Joints.Count - 1; i++)
            {
                lineRenderer.positionCount += 2;

                lineRenderer.SetPosition(lineRenderer.positionCount - 2, cableRoot.Joints[i].tangentPointTail);
                lineRenderer.SetPosition(lineRenderer.positionCount - 1, cableRoot.Joints[i].tangentPointHead);
            }
            lineRenderer.positionCount += 1;
            lineRenderer.SetPosition(lineRenderer.positionCount - 1, cableRoot.Joints[cableRoot.Joints.Count - 1].tangentPointTail);
        }
    }
}
