using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjStickPoint : StickPoint
{
    [SerializeField] SkinnedMeshRenderer projRenderer;

    [SerializeField] Material stickyMaterial;

    [SerializeField] Material unstickyMaterial;

    [SerializeField] Material stickedMaterial;

    Material[] materials;

    private void Start()
    {
        materials = projRenderer.materials;
        materials[3] = unstickyMaterial;
        projRenderer.materials = materials;
    }

    protected override void StickEvent()
    {
        materials[3] = stickedMaterial;
        projRenderer.materials = materials;
    }

    protected override void UnstickEvent()
    {
        if (IsSticky)
        {
            materials[3] = stickyMaterial;
            projRenderer.materials = materials;
        }
        else
        {
            materials[3] = unstickyMaterial;
            projRenderer.materials = materials;
        }
    }

    protected override void StickToggleEvent()
    {
        if (IsSticky)
        {
            materials[3] = stickyMaterial;
            projRenderer.materials = materials;
        }
        else
        {
            materials[3] = unstickyMaterial;
            projRenderer.materials = materials;
        }
    }
}
