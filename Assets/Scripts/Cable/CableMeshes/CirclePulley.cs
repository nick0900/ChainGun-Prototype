using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class CirclePulley : CableMeshInterface
{

    [SerializeField] CircleCollider2D data = null;

    public override CMPrimitives ChainMeshPrimitiveType { get { return CMPrimitives.polygon; } }

    public override bool MeshGenerated { get { return data != null; } }

    public override bool Errornous
    {
        get
        {
            return !MeshGenerated;
        }
    }

    protected override void SetupMesh()
    {
        data = GetComponent<CircleCollider2D>();
    }
    public override void RemoveChainMesh()
    {
        data = null;
    }

    public override bool PrintErrors()
    {
        bool error = false;

        if (!MeshGenerated)
        {
            error = true;
            print(this + "/Error: mesh not generated");
        }
        return error;
    }

    public override bool CorrectErrors()
    {
        bool errorsFixed = true;

        if (!MeshGenerated)
        {
            errorsFixed = false;
            print(this + "/Error: mesh not generated");
            print(this + "/FixFailed: you need to manually generate this or all cablemeshes");
        }
        return errorsFixed;
    }

    public override bool Orientation(in Vector2 tailPrevious, in Vector2 headPrevious)
    {
        Vector2 tailDirection = tailPrevious - CenterWorldPosition(data);
        Vector2 headDirection = headPrevious - CenterWorldPosition(data);

        return Vector2.SignedAngle(tailDirection, headDirection) > 0;
    }

    public override Vector2 PointToShapeTangent(in Vector2 point, bool orientation, float chainWidth, ref ChainJointCache cache)
    {
        Vector2 d = ((Vector2)data.transform.position) - point;

        if (d.magnitude <= data.radius - chainWidth / 2)
        {
            throw new System.Exception();
        }

        float alpha = d.x >= 0 ? Mathf.Asin(d.y / d.magnitude) : Mathf.PI - Mathf.Asin(d.y / d.magnitude);

        float phi = Mathf.Asin((data.radius - chainWidth / 2) / d.magnitude);

        alpha = orientation ? alpha - Mathf.PI / 2 - phi : alpha + Mathf.PI / 2 + phi;

        return data.radius * new Vector2(Mathf.Cos(alpha), Mathf.Sin(alpha));
    }

    public override void CreateChainCollider(float chainWidth)
    {
        throw new System.NotImplementedException();
    }
}
