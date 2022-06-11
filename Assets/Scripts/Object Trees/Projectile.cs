using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Projectile : GraphObject
{
    [SerializeField] StickPoint projStickPoint;

    [HideInInspector] public LinkedListNode<Projectile> LLN;

    protected override void AdditionalSetup()
    {
        LLN = new LinkedListNode<Projectile>(this);
    }

    public void Detonate()
    {
        Remove();
    }

    public void ToggleSticky()
    {
        if (projStickPoint != null)
        {
            projStickPoint.ToggleSticky();
        }
    }

    public override void Unstick()
    {
        projStickPoint.Unstick();
    }

    public override void StickEvent()
    {

    }

    public override void UnstickEvent()
    {

    }

    public override void StickToggleEvent(bool isSticky)
    {

    }

    public void Remove()
    {
        projStickPoint.IsSticky = false;

        if (LLN.List != null)
        {
            LLN.List.Remove(LLN);
        }
        GraphManager.GM.UnstickConnected(this);
        ProjectilePool.ProjPool.Store(this);
    }
}