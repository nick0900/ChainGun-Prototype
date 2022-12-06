using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CableLengthTally : MonoBehaviour
{
    [SerializeField] private CableAnchor cableAnchor = null;
    private void OnEnable()
    {
        if (cableAnchor == null) cableAnchor = gameObject.GetComponent<CableAnchor>();
        if (cableAnchor == null) this.enabled = false;
    }

    private void OnDisable()
    {
        cableAnchor = null;
    }

    private void FixedUpdate()
    {
        CableBase start = cableAnchor;

        print(cableAnchor.name + " tallied length: " + (LengthTally(start.head) + start.storedLength));
    }

    float LengthTally(CableBase current)
    {
        if (current == null) return 0;
        if (current.node == null)
        {
            return LengthTally(current.head);
        }
        return current.node.restLength + current.storedLength + LengthTally(current.head);
    }
}
