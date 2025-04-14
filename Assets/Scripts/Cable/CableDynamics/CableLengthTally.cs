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
        print(cableRoot.name + " tallied length: " + CableRoot.LengthTally(cableRoot));
    }
}
