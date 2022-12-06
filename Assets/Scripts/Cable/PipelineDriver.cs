using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PipelineDriver : MonoBehaviour
{
    [SerializeField] CableAnchorSet cableSet = null;
    [SerializeField] CableMeshSet pulleySet = null;

    private void OnEnable()
    {
        recordPositions();
    }

    private void FixedUpdate()
    {
        foreach (CableAnchor cableStart in cableSet)
        {
            if (cableStart != null)
            {
                cableStart.CableUpdate();
                cableStart.CableSolve();
            }
        }
        recordPositions();
    }

    private void recordPositions()
    {
        foreach (CableAnchor cableStart in cableSet)
        {
            if (cableStart != null)
            {
                cableStart.ChainRecordPositions(cableStart);
            }
        }
        foreach (CableMeshInterface pulley in pulleySet)
        {
            if (pulley != null)
            {
                pulley.RecordPosition();
            }
        }
    }
}
