using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SurfaceMaster : MonoBehaviour
{
    [SerializeField] private bool Stickable = false;

    [SerializeField] private GraphObject graphObject;

    [SerializeField] private bool chainable = false;

    [SerializeField] private Collider2D hitbox;

    public bool infiniteFriction = false;

    public float staticFrictConst = 0.2f;

    public float kineticFrictConst = 0.1f;

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

        if (chainable)
        {
            CableHitbox trigger = collision.GetComponent<CableHitbox>();

            if (trigger != null)
            {
                if (hitbox == null)
                {
                    hitbox = this.GetComponent<Collider2D>();
                }
                //trigger.Transmit(hitbox);
            }
        }
    }
}
