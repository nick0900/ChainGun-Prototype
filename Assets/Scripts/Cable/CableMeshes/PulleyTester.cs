using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PulleyTester : MonoBehaviour
{
    [SerializeField] CableMeshInterface pulley;

    [SerializeField] Vector2 p1;
    [SerializeField] Vector2 p2;
    [SerializeField] Vector2 p3;
    [SerializeField] Vector2 p4;

    [ContextMenu("orientation test")]
    public void orientation()
    {
        print(pulley.Orientation(p1, p2).ToString());
    }
}
