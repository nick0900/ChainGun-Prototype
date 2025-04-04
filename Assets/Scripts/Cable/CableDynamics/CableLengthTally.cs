using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableLengthTally : MonoBehaviour
{
    [SerializeField] private CableRoot cableRoot = null;
    private void OnEnable()
    {
        if (cableRoot == null) cableRoot = gameObject.GetComponent<CableRoot>();
        if (cableRoot == null) this.enabled = false;
    }

    private void OnDisable()
    {
        cableRoot = null;
    }

    private void FixedUpdate()
    {
        print(cableRoot.name + " tallied length: " + LengthTally(cableRoot));
    }

    float LengthTally(CableRoot cable)
    {
        float tally = 0.0f;
        foreach (CableRoot.Joint joint in cable.Joints)
        {
            tally += joint.restLength + joint.storedLength;
        }
        return tally;
    }
}
