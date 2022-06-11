using System.Collections;
using System.Collections.Generic;
using UnityEngine;

abstract public class StickPoint : MonoBehaviour
{
    [SerializeField] private GraphObject graphObject;

    //triggerCollider måste inte vara addresserad för att triggers ska fungera, dock blir de ej avstängda då.
    [SerializeField] private Collider2D triggerCollider;

    private bool isSticky = false;

    private bool isSticked = false;

    private GraphObject sticked = null;

    private void Awake()
    {
        if (triggerCollider != null)
        {
            triggerCollider.isTrigger = true;
            triggerCollider.enabled = false;
        }
    }

    public bool IsSticky
    {
        get { return isSticky; }

        set { if (value != isSticky) ToggleSticky(); }
    }

    public bool IsSticked
    {
        get { return isSticked; }
    }

    public void ToggleSticky()
    {
        bool nullCheck = triggerCollider != null;
        if (isSticky)
        {
            isSticky = false;
            Unstick();
            if (nullCheck)
            {
                triggerCollider.enabled = false;
            }
        }
        else
        {
            isSticky = true;
            if (nullCheck)
            {
                triggerCollider.enabled = true;
            }
        }
        StickToggleEvent();
        graphObject.StickToggleEvent(isSticky);
    }

    virtual public void Stick(GraphObject target)
    {
        if (graphObject != null && !isSticked && isSticky)
        {
            GraphManager.GM.Join(graphObject, target);

            sticked = target;

            isSticked = true;

            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
            StickEvent();
            graphObject.StickEvent();
        }
    }

    virtual public void Unstick()
    {
        if (isSticked)
        {
            GraphManager.GM.split(graphObject, sticked);
            sticked = null;

            isSticked = false;

            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }

            UnstickEvent();
            graphObject.UnstickEvent();
        }
    }

    abstract protected void StickEvent();

    abstract protected void UnstickEvent();

    abstract protected void StickToggleEvent();
}
