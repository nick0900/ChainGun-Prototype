using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugLineCable : MonoBehaviour
{
    private LineRenderer lineRenderer;
    [SerializeField] private CableAnchor cableAnchor = null;
    private void OnEnable()
    {
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        if (cableAnchor == null) cableAnchor = gameObject.GetComponent<CableAnchor>();
        if (cableAnchor == null) this.enabled = false;
    }

    private void OnDisable()
    {
        Destroy(lineRenderer);
    }

    private void FixedUpdate()
    {
        CableBase start = cableAnchor;

        lineRenderer.startWidth = start.CableWidth;
        lineRenderer.endWidth = start.CableWidth;

        lineRenderer.positionCount = 1;

        lineRenderer.SetPosition(0, start.CableEndPosition);

        ChainDraw(start.head);
    }

    void ChainDraw(CableBase current)
    {
        if (current== null) return;

        lineRenderer.positionCount += 2;

        lineRenderer.SetPosition(lineRenderer.positionCount - 2, current.CableStartPosition);
        lineRenderer.SetPosition(lineRenderer.positionCount - 1, current.CableEndPosition);

        ChainDraw(current.head);
    }
}
