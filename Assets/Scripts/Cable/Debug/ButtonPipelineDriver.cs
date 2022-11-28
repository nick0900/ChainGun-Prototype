using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ButtonPipelineDriver : MonoBehaviour
{
    [SerializeField] CableAnchorSet cableSet = null;
    [SerializeField] CableMeshSet pulleySet = null;

    float frameAxis;
    bool buttonPressed = false;

    void Update()
    {
        frameAxis = Input.GetAxis("debug");
    }

    private void OnEnable()
    {
        recordPositions();
    }

    private void FixedUpdate()
    {
        if (frameAxis > 0)
        {
            if (!buttonPressed)
            {
                foreach (CableAnchor cableStart in cableSet)
                {
                    if (cableStart != null)
                    {
                        cableStart.CableUpdate();
                        //cableStart.CableSolve();
                    }
                }

                recordPositions();

                buttonPressed = true;
            }
        }
        else if (buttonPressed)
        {
            buttonPressed = false;
        }
        
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
