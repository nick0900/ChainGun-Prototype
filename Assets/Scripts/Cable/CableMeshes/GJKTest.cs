using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GJKTest : MonoBehaviour
{
    public List<CableMeshInterface> colliders;

    // Update is called once per frame
    void Update()
    {
        for (int i = 0; i < colliders.Count; i++)
            for(int j = 0; j < colliders.Count; j++)
            {
                if (i == j) continue;
                CableMeshInterface.cableIntersection result = CableMeshInterface.GJKIntersection(colliders[i], colliders[j]);
                if (result.intersecting)
                {
                    print(colliders[i].name + " colliding with " + colliders[j].name);
                }
            }
    }
}
