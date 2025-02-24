using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMaster : MonoBehaviour
{
    [SerializeField] private bool Stickable = false;

    [SerializeField] private GraphObject graphObject;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (Stickable)
        {
            StickPoint point = collision.GetComponent<StickPoint>();

            if (point != null)
            {
                point.Stick(graphObject);
            }
        }
    }
}
