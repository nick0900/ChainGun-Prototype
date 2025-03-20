using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static CableMeshInterface;

public class GJKTest : MonoBehaviour
{
    public List<CableMeshInterface> colliders;

    public List<CablePinchManifold> manifolds;

    private void Start()
    {
        manifolds = new List<CablePinchManifold>();
    }

    // Update is called once per frame
    void Update()
    {
        manifolds.Clear();
        /*
        for (int i = 0; i < colliders.Count; i++)
            for(int j = 0; j < colliders.Count; j++)
            {
                if (i == j) continue;
                CablePinchManifold result = CableMeshInterface.GJKIntersection(colliders[i], colliders[j], 0.1f, 0.001f);
                if (result.hasContact)
                {
                    manifolds.Add(result);
                }
            }
        */
        CablePinchManifold result = CableMeshInterface.GJKIntersection(colliders[0], colliders[1], 0.1f, 0.001f);
        if (result.hasContact)
        {
            manifolds.Add(result);
        }
    }

    private void OnDrawGizmos()
    {
        if (Application.isPlaying)
            foreach (CablePinchManifold manifold in manifolds)
            {
                if (manifold.contactCount > 0)
                {
                    Gizmos.color = Color.red;
                    Gizmos.DrawSphere(manifold.contact1.A, 0.05f);
                    Gizmos.color = Color.yellow;
                    Gizmos.DrawSphere(manifold.contact1.B, 0.05f);
                }

                if (manifold.contactCount == 2)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawSphere(manifold.contact2.A, 0.05f);
                    Gizmos.color = Color.cyan;
                    Gizmos.DrawSphere(manifold.contact2.B, 0.05f);
                }

            }
    }
}
